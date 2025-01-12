
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Helpers;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Enums;
using System.Text.Json;
using LanguageExt.Common;
using LanguageExt;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.Services
{
    /// <summary>
    /// Invoked when the application is launched.
    /// Needs to call InitializeAsync to get the portfolios and connect to the database.
    /// </summary>
    public partial class PortfolioService : ObservableObject
    {
        private static ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(PortfolioService).Name.PadRight(22));
        private readonly IPreferencesService _preferenceService;
        private readonly IPortfolioContextFactory _contextFactory;
        public PortfolioContext Context { get; private set; }
        public string DatabasePath { get; private set; }
        [ObservableProperty] private Portfolio currentPortfolio;
        [ObservableProperty] private List<Portfolio> portfolios = new();

        public bool IsInitialPortfolioLoaded { get; private set; } = false;

        public PortfolioService(IPortfolioContextFactory contextFactory, IPreferencesService preferencesService)
        {
            _contextFactory = contextFactory;
            _preferenceService = preferencesService;
        }

        /// <summary>
        /// Gets the available Portfolios and connects to the Database
        /// </summary>
        public async Task InitializeAsync()
        {
            await GetPortfolios();
            IsInitialPortfolioLoaded = await LoadInitialPortfolio();
        }

        public async Task<Result<bool>> SwitchPortfolio(Portfolio portfolio)
        {
            var result = await ConnectPortfolioDatabase(portfolio);
            return result.Match(
                Succ: succ =>
                {
                    Logger?.Information($"Switched to Database {DatabasePath}");
                    return new Result<bool>(true);
                },
                Fail: err =>
                {
                    Logger?.Error(err, $"Failed to switch to Database {DatabasePath}");
                    return new Result<bool>(err);
                });
        }

        private async Task GetPortfolios()
        {
            string portfoliosFile = Path.Combine(App.appDataPath, App.PortfoliosPath, App.PortfoliosFileName);

            if (File.Exists(portfoliosFile))
            {
                await LoadPortfoliosAsync(portfoliosFile, async stream =>
                {
                    Portfolios = await JsonSerializer.DeserializeAsync<List<Portfolio>>(stream);
                    Logger.Information("Portfolios data de-serialized successfully. {0} portfolios)", Portfolios?.Count);
                });
            }
            else
            {
                if (await MigrateFolderStructureIfNeeded())
                {
                    Portfolios = GetPortfoliosFromFolders();
                    await SavePortfoliosAsync(portfoliosFile, async stream =>
                    {
                        await JsonSerializer.SerializeAsync(stream, Portfolios);
                        Logger.Information("Portfolios data serialized successfully. {0} portfolios)", Portfolios?.Count);
                    });
                }
            }
        }

        private async Task<bool> LoadInitialPortfolio()
        {
            try
            {
                var portfolio = _preferenceService.GetLastPortfolio() ?? Portfolios.FirstOrDefault();
                if (portfolio == null)
                {
                    Logger?.Error("No portfolio found to load.");
                    return false;
                }

                var result = await ConnectPortfolioDatabase(portfolio);
                if (result.IsSuccess)
                {
                    Logger?.Information($"Connected to Database {portfolio.Path}");
                    return true;
                }
                else
                {
                    Logger?.Error($"Failed to connect to Database {portfolio.Path}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger?.Error(ex, "Failed to connect to Database");
                return false;
            }
        }

        private async Task<Result<bool>> ConnectPortfolioDatabase(Portfolio portfolio)
        {
            var previousContext = Context;
            try
            {
                var fullPath = Path.Combine(portfolio.Path, App.DbName);
                Context = _contextFactory.Create($"Data Source=|DataDirectory|{fullPath}");
                await CheckDatabase(portfolio.Path);
                DatabasePath = portfolio.Path;
                CurrentPortfolio = portfolio;

                var pf = Portfolios.FirstOrDefault(p => p.Name == portfolio.Name);
                if (pf != null)
                {
                    pf.LastAccess = DateTime.Now;
                    _preferenceService.SetLastPortfolio(pf);
                }

                string portfoliosFile = Path.Combine(App.appDataPath, App.PortfoliosPath, App.PortfoliosFileName);
                await SavePortfoliosAsync(portfoliosFile, async stream =>
                {
                    await JsonSerializer.SerializeAsync(stream, Portfolios);
                    Logger.Information("Portfolios data serialized successfully. {0} portfolios)", Portfolios?.Count);
                });
            }
            catch (Exception ex)
            {
                Context = previousContext;
                return new Result<bool>(ex);
            }
            return true;
        }

        private async Task CheckDatabase(string databasePath)
        {
            Logger?.Information($"Checking Database for portfolio {databasePath}");

            if (Context == null)
            {
                Logger?.Error("Failed to retrieve PortfolioContext from the service container.");
                return;
            }

            var dbFilename = Path.Combine(App.appDataPath, databasePath, App.DbName);

            if (File.Exists(dbFilename))
            {
                BackupCptFiles(databasePath, false);
            }

            var pendingMigrations = await Context.Database.GetPendingMigrationsAsync();
            var initPriceLevelsEntity = pendingMigrations.Any(m => m.Contains("AddPriceLevelsEntity"));
            var initNarrativesEntity = pendingMigrations.Any(m => m.Contains("AddNarrativesEntity"));

            if (pendingMigrations.Any())
            {
                foreach (var migration in pendingMigrations)
                {
                    Logger?.Information("Pending Migrations {0}", migration);
                }

                if (File.Exists(dbFilename)) BackupCptFiles(databasePath, true);
            }

            App.needFixFaultyMigration = (await Context.Database.GetAppliedMigrationsAsync()).Contains("20241228225250_AddNarrativesEntity");

            await Context.Database.MigrateAsync();

            if (initPriceLevelsEntity)
            {
                await SeedPriceLevelsTable(Context);
            }
            if (initNarrativesEntity)
            {
                await SeedNarrativesTable(Context);
            }

            var appliedMigrations = await Context.Database.GetAppliedMigrationsAsync();
            foreach (var migration in appliedMigrations)
            {
                Logger?.Information("Applied Migrations {0}", migration);
            }
        }

        private void BackupCptFiles(string databasePath, bool isMigration)
        {
            var dbFile = Path.Combine(App.appDataPath, databasePath, App.DbName);
            var preferencesFile = Path.Combine(App.appDataPath, App.PrefFileName);
            var graphFile = Path.Combine(App.appDataPath, databasePath, "graph.json");
            var chartsFolder = Path.Combine(App.appDataPath, App.ChartsFolder);
            var tempFolder = Path.Combine(App.appDataPath, App.PortfoliosPath, "Temp");
            var backupFolder = Path.Combine(App.appDataPath, databasePath, App.BackupFolder);
            string backUpName;

            try
            {
                Directory.CreateDirectory(backupFolder);

                if (isMigration)
                {
                    if (Directory.GetFiles(backupFolder, "*" + App.ProductVersion.Replace(".", "-") + "*").Any())
                    {
                        return;
                    }
                    backUpName = $"{App.PrefixBackupName}_m{App.ProductVersion.Replace(".", "-")}_{DateTime.Now:yyyyMMdd-HHmmss}.{App.ExtentionBackup}";
                }
                else
                {
                    App.initDone = true;
                    var backupFiles = Directory.GetFiles(backupFolder, "*_s_*");
                    if (backupFiles.Length > 5)
                    {
                        File.Delete(backupFiles[0]);
                    }
                    backUpName = $"{App.PrefixBackupName}_s_{DateTime.Now:yyyyMMdd-HHmmss}.{App.ExtentionBackup}";
                }

                MkOsft.CreateDirectory(tempFolder, true);
                File.Copy(dbFile, Path.Combine(tempFolder, App.DbName));
                File.Copy(preferencesFile, Path.Combine(tempFolder, App.PrefFileName));
                File.Copy(graphFile, Path.Combine(tempFolder, "graph.json"));

                Directory.CreateDirectory(chartsFolder);
                MkOsft.DirectoryCopy(chartsFolder, Path.Combine(tempFolder, App.ChartsFolder), true);
                ZipFile.CreateFromDirectory(tempFolder, Path.Combine(backupFolder, backUpName));
                Directory.Delete(tempFolder, true);
            }
            catch (Exception ex)
            {
                Logger?.Error(ex, "BackUp CPT files failed!");
            }
        }

        private static async Task SeedPriceLevelsTable(PortfolioContext context)
        {
            if (!context.Coins.Any()) return;

            var priceLevels = context.Coins.SelectMany(coin => new List<PriceLevel>
                {
                    new() { Coin = coin, Type = PriceLevelType.TakeProfit, Value = 0, Status = PriceLevelStatus.NotWithinRange, Note = string.Empty },
                    new() { Coin = coin, Type = PriceLevelType.Buy, Value = 0, Status = PriceLevelStatus.NotWithinRange, Note = string.Empty },
                    new() { Coin = coin, Type = PriceLevelType.Stop, Value = 0, Status = PriceLevelStatus.NotWithinRange, Note = string.Empty }
                }).ToList();

            context.PriceLevels.AddRange(priceLevels);
            await context.SaveChangesAsync();
        }

        private static async Task SeedNarrativesTable(PortfolioContext context)
        {
            if (context.Narratives.Count() > 1) return;

            var narratives = new List<Narrative>
                {
                    new() { Name = "AI", About = "AI in crypto refers to the use of artificial intelligence to optimize trading, provide market insights, and enhance security." },
                    new() { Name = "Appchain", About = "Appchains are application-specific blockchains designed to optimize performance for particular decentralized applications (DApps)." },
                    new() { Name = "DeFI", About = "DeFi (Decentralized Finance) aims to recreate traditional financial systems using decentralized technologies like blockchain." },
                    new() { Name = "DEX", About = "DEX (Decentralized Exchange) allows users to trade cryptocurrencies directly without an intermediary, leveraging smart contracts." },
                    new() { Name = "DePin", About = "DePin (Decentralized Physical Infrastructure Networks) combines blockchain with physical infrastructures like IoT to create decentralized networks." },
                    new() { Name = "Domains", About = "Blockchain domains offer decentralized, censorship-resistant alternatives to traditional domain names, enhancing ownership and control." },
                    new() { Name = "Gamble-Fi", About = "Gamble-Fi integrates decentralized finance principles with online gambling, providing transparent and secure gaming experiences." },
                    new() { Name = "Game-Fi", About = "Game-Fi combines gaming and decentralized finance, allowing players to earn cryptocurrency and trade in-game assets." },
                    new() { Name = "Social-Fi", About = "Social-Fi integrates social media with decentralized finance, enabling monetization and decentralized governance of social platforms." },
                    new() { Name = "Interoperability", About = "Interoperability focuses on enabling different blockchain networks to communicate and interact, facilitating seamless asset transfers and data exchange." },
                    new() { Name = "Layer 1s", About = "Layer 1s are the base layer blockchains like Bitcoin and Ethereum that provide the foundational security and consensus mechanisms." },
                    new() { Name = "Layer 2s", About = "Layer 2s are scaling solutions built on top of Layer 1 blockchains to improve transaction speed and reduce fees." },
                    new() { Name = "LSD", About = "LSD (Liquid Staking Derivatives) allow users to stake assets and receive liquid tokens that can be used in DeFi activities." },
                    new() { Name = "Meme", About = "Meme coins are cryptocurrencies inspired by internet memes, often characterized by high volatility and community-driven value." },
                    new() { Name = "NFT", About = "NFTs (Non-Fungible Tokens) are unique digital assets representing ownership of items like art, music, and virtual real estate." },
                    new() { Name = "Privacy", About = "Privacy coins and technologies aim to enhance transaction anonymity and data protection on the blockchain." },
                    new() { Name = "Real Yield", About = "Real Yield focuses on generating sustainable returns through staking, lending, and other DeFi activities with real-world asset backing." },
                    new() { Name = "RWA", About = "RWA (Real World Assets) are physical assets like real estate or commodities tokenized on the blockchain for easier trading and investment." },
                    new() { Name = "CEX", About = "CEX (Centralized Exchange) refers to traditional cryptocurrency exchanges where trades are managed by a central entity." },
                    new() { Name = "Stablecoins", About = "Stablecoins are cryptocurrencies pegged to stable assets like fiat currencies to minimize price volatility." },
                    new() { Name = "Others", About = "Narrative for coins that you don't want to assign a specific Narrative" }
                };

            context.Narratives.AddRange(narratives);
            await context.SaveChangesAsync();
        }

        private static async Task LoadPortfoliosAsync(string fileName, Func<FileStream, Task> processStream)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    using FileStream openStream = File.OpenRead(fileName);
                    await processStream(openStream);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to de-serialize data from {0}", fileName);
                }
            }
        }

        private static async Task SavePortfoliosAsync(string fileName, Func<FileStream, Task> processStream)
        {
            try
            {
                using FileStream createStream = File.Create(fileName);
                await processStream(createStream);
                Logger.Information("Portfolios data serialized successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to serialize Portfolio data to {0}", fileName);
            }
        }

        private List<Portfolio> GetPortfoliosFromFolders()
        {
            var fullPortfoliosPath = Path.Combine(App.appDataPath, App.PortfoliosPath);
            var dirs = Directory.GetDirectories(fullPortfoliosPath);
            var portfolios = new List<Portfolio>();

            foreach (var dir in dirs)
            {
                var pidFilePath = Path.Combine(dir, "pid.json");
                string portfolioName = Path.GetRelativePath(fullPortfoliosPath, dir);

                if (File.Exists(pidFilePath))
                {
                    try
                    {
                        var jsonString = File.ReadAllText(pidFilePath);
                        var jsonDoc = JsonDocument.Parse(jsonString);
                        if (jsonDoc.RootElement.TryGetProperty("PortfolioName", out var nameElement))
                        {
                            portfolioName = nameElement.GetString() ?? portfolioName;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger?.Error(ex, "Failed to read or parse pid.json in directory {0}", dir);
                    }
                }

                portfolios.Add(new Portfolio
                {
                    Name = portfolioName,
                    Path = Path.GetRelativePath(App.appDataPath, dir)
                });
            }
            return portfolios;
        }

        private async Task<bool> MigrateFolderStructureIfNeeded()
        {
            bool alreadyMigrated = true;
            string portfoliosPath = Path.Combine(App.appDataPath, App.PortfoliosPath);

            if (!Directory.Exists(portfoliosPath))
            {
                string initialPortfolioFolder = Guid.NewGuid().ToString();
                string fullPortfolioPath = Path.Combine(portfoliosPath, initialPortfolioFolder);
                string fullChartsPath = Path.Combine(App.appDataPath, App.ChartsFolder);
                string fullBackupPath = Path.Combine(App.appDataPath, App.BackupFolder);
                string newBackupPath = Path.Combine(fullPortfolioPath, "Backup");

                alreadyMigrated = false;

                Directory.CreateDirectory(fullPortfolioPath);
                Directory.CreateDirectory(fullChartsPath);
                Directory.CreateDirectory(newBackupPath);

                if (!IsBlankInstall())
                {
                    MkOsft.FileMove("sqlCPT.db", App.appDataPath, fullPortfolioPath);
                    MkOsft.FileMove("graph.json", fullChartsPath, fullPortfolioPath);
                    MkOsft.FileMove("graph.json.bak", fullChartsPath, fullPortfolioPath);
                    MkOsft.DirectoryMove(fullBackupPath, newBackupPath, true);
                    MkOsft.FilesDelete("*_backup_*", App.appDataPath);
                }

                var portfolio = new Portfolio
                {
                    Name = "Default Portfolio",
                    Path = Path.Combine(App.PortfoliosPath, initialPortfolioFolder)
                };
                Portfolios.Add(portfolio);

                SavePidToJson(portfolio.Name, fullPortfolioPath, true);

                string portfoliosFile = Path.Combine(portfoliosPath, App.PortfoliosFileName);
                await SavePortfoliosAsync(portfoliosFile, async stream =>
                {
                    await JsonSerializer.SerializeAsync(stream, Portfolios);
                });
            }
            return alreadyMigrated;
        }

        private void SavePidToJson(string pIdName, string path, bool isHidden = false)
        {
            var portfolioNameObject = new { PortfolioName = pIdName };
            string jsonString = JsonSerializer.Serialize(portfolioNameObject, new JsonSerializerOptions { WriteIndented = true });
            string filePath = Path.Combine(path, "pid.json");

            try
            {
                File.WriteAllText(filePath, jsonString);
                if (isHidden) MkOsft.MakeFileHidden(filePath);
            }
            catch (Exception ex)
            {
                Logger?.Error(ex, "Failed to save pid.json file at {0}", filePath);
            }
        }

        private bool IsBlankInstall()
        {
            var portfoliosPath = Path.Combine(App.appDataPath, App.PortfoliosPath);
            var dbFilePath = Path.Combine(App.appDataPath, App.DbName);
            return !Directory.Exists(portfoliosPath) && !File.Exists(dbFilePath);
        }
    }
}