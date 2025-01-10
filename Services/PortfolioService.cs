
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using CryptoPortfolioTracker.Enums;
using System.Text.Json;
using System.Security.AccessControl;

namespace CryptoPortfolioTracker.Services
{
    /// <summary>
    /// Invoked when the application is launched.
    /// Needs to call InitializeAsync to get the portfolios and connect to the database.
    /// </summary>
    public class PortfolioService
    {
        private static ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(PortfolioService).Name.PadRight(22));
        private readonly IPreferencesService _preferenceService;
        private readonly IPortfolioContextFactory _contextFactory;
        public PortfolioContext Context { get; private set; }
        public string DatabasePath { get; private set; }
        public Portfolio CurrentPortfolio { get; private set; }
        public List<Portfolio> Portfolios { get; set; } = new();

        private Task getPortfolioTask;


        
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
            await LoadInitialPortfolio();
        }

        public async Task SwitchPortfolio(Portfolio portfolio)
        {
            await ConnectPortfolioDatabase(portfolio);

            Logger?.Information($"Switched to Database {DatabasePath}");
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
                bool alreadyMigrated = await MigrateFolderStructureIfNeeded();
                if (alreadyMigrated)
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

        private async Task LoadInitialPortfolio()
        {
            var portfolio = _preferenceService.GetLastPortfolio() ?? Portfolios.FirstOrDefault();
            if (portfolio == null)
            {
                Logger?.Error("No Portfolio found. Creating a new one.");
                portfolio = new Portfolio { Name = "Default", Path = Path.Combine(App.PortfoliosPath, "Default") };
                Portfolios.Add(portfolio);
                _preferenceService.SetLastPortfolio(portfolio);
            }
            await ConnectPortfolioDatabase(portfolio);

            Logger?.Information($"Connected to Database {DatabasePath}");
        }

        private async Task ConnectPortfolioDatabase(Portfolio portfolio)
        {
            DatabasePath = portfolio.Path;
            CurrentPortfolio = portfolio;
            portfolio.LastAccess = DateTime.Now;
            var fullPath = Path.Combine(portfolio.Path, App.DbName);
            Context = _contextFactory.Create("Data Source=|DataDirectory|" + fullPath);
            await CheckDatabase();
        }

        private async Task CheckDatabase()
        {
            try
            {
                Logger?.Information($"Checking Database for portfolio {DatabasePath}");

                if (Context == null)
                {
                    Logger?.Error("Failed to retrieve PortfolioContext from the service container.");
                    return;
                }

                var dbFilename = Path.Combine(App.appDataPath, DatabasePath, App.DbName);

                if (File.Exists(dbFilename))
                {
                    BackupCptFiles(false);
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

                    if (File.Exists(dbFilename)) BackupCptFiles(true);
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
            catch (Exception ex)
            {
                Logger?.Error(ex, "Checking Database failed!");
            }
        }

        private void BackupCptFiles(bool isMigration)
        {
            var dbFile = Path.Combine(App.appDataPath, DatabasePath, App.DbName);
            var preferencesFile = Path.Combine(App.appDataPath, App.PrefFileName);
            var graphFile = Path.Combine(App.appDataPath, DatabasePath, "graph.json");
            var chartsFolder = Path.Combine(App.appDataPath, App.ChartsFolder);
            var tempFolder = Path.Combine(App.appDataPath, App.PortfoliosPath, "Temp");
            var backupFolder = Path.Combine(App.appDataPath, DatabasePath, App.BackupFolder);
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

                Directory.CreateDirectory(tempFolder);
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
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to serialize data from {0}", fileName);
            }
        }

        private List<Portfolio> GetPortfoliosFromFolders()
        {
            var portfoliosPath = Path.Combine(App.appDataPath, App.PortfoliosPath);
            var dirs = Directory.GetDirectories(portfoliosPath);
            foreach (var dir in dirs)
            {
                var pidFilePath = Path.Combine(dir, "pid.json");
                string portfolioName = Path.GetRelativePath(Path.Combine(App.appDataPath, App.PortfoliosPath), dir);

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

                var portfolio = new Portfolio
                {
                    Name = portfolioName,
                    Path = Path.GetRelativePath(App.appDataPath, dir)
                };
                Portfolios.Add(portfolio);
            }
            return Portfolios;
        }

        private async Task<bool> MigrateFolderStructureIfNeeded()
        {
            bool alreadyMigrated = true;
            if (!Directory.Exists(Path.Combine(App.appDataPath, App.PortfoliosPath)))
            {
                alreadyMigrated = false;
                var newPath = Path.Combine(App.appDataPath, App.PortfoliosPath, "Default Portfolio");
                Directory.CreateDirectory(newPath);

                var dbFile = Path.Combine(App.appDataPath, App.DbName);
                var newDbFile = Path.Combine(App.appDataPath, App.PortfoliosPath, "Default Portfolio\\sqlCPT.db");
                if (File.Exists(dbFile))
                {
                    File.Move(dbFile, newDbFile);
                }

                var graphFile = Path.Combine(App.appDataPath, App.ChartsFolder, "graph.json");
                var newGraphFile = Path.Combine(App.appDataPath, App.PortfoliosPath, "Default Portfolio\\graph.json");
                if (File.Exists(graphFile))
                {
                    File.Move(graphFile, newGraphFile);
                }

                var backupGraphFile = Path.Combine(App.appDataPath, App.ChartsFolder, "graph.json.bak");
                var newBackupGraphFile = Path.Combine(App.appDataPath, App.PortfoliosPath, "Default Portfolio\\graph.json.bak");
                if (File.Exists(backupGraphFile))
                {
                    File.Move(backupGraphFile, newBackupGraphFile);
                }

                var backupFolder = Path.Combine(App.appDataPath, App.BackupFolder);
                var newBackupFolder = Path.Combine(App.appDataPath, App.PortfoliosPath, "Default Portfolio\\Backup");

                MkOsft.DirectoryMove(backupFolder, newBackupFolder, true);

                var portfolio = new Portfolio
                {
                    Name = "Default Portfolio",
                    Path = Path.Combine(App.PortfoliosPath, "Default Portfolio")
                };
                Portfolios.Add(portfolio);

                try
                {
                    var portfoliosFile = Path.Combine(App.appDataPath, App.PortfoliosPath, App.PortfoliosFileName);
                    await SavePortfoliosAsync(portfoliosFile, async stream =>
                    {
                        await JsonSerializer.SerializeAsync(stream, Portfolios);
                        Logger.Information("Portfolios data serialized successfully. {0} portfolios)", Portfolios?.Count);
                    });

                    var oldFiles = Directory.GetFiles(App.appDataPath, "*_backup_*");
                    foreach (var file in oldFiles)
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Information("Portfolios data serialization failed.");
                }
            }
            return alreadyMigrated;
        }
    }
}