
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
using System.Text.Json;
using LanguageExt.Common;
using LanguageExt;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.ViewModels;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

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
        private IPriceUpdateService _priceUpdateService;
        private IGraphUpdateService _graphUpdateService;

        public readonly IPortfolioContextFactory _contextFactory;
        public readonly IUpdateContextFactory _updateContextFactory;
        public PortfolioContext Context { get; set; }
        public UpdateContext UpdateContext { get; set; }

        [ObservableProperty] private Portfolio? currentPortfolio;
        [ObservableProperty] private ObservableCollection<Portfolio> portfolios = new();

        partial void OnCurrentPortfolioChanged(Portfolio value)
        {
            CurrentPortfolio.LastAccess = DateTime.Now; ;
        }

        public bool IsInitialPortfolioLoaded { get; private set; } = false;

        public PortfolioService(IPortfolioContextFactory contextFactory, IUpdateContextFactory updateContextFactory, IPreferencesService preferencesService)
        {
            _contextFactory = contextFactory;
            _updateContextFactory = updateContextFactory;
            _preferenceService = preferencesService;
            
        }

        /// <summary>
        /// Gets the available Portfolios and connects to the Database
        /// </summary>
        public async Task InitializeAsync()
        {
            await GetPortfolios();
            IsInitialPortfolioLoaded = await LoadInitialPortfolio();

            _graphUpdateService = App.Container.GetService<IGraphUpdateService>();
           
            _priceUpdateService = App.Container.GetService<IPriceUpdateService>();
        }

        public async Task<Result<bool>> SwitchPortfolio(Portfolio portfolio)
        {
            var result = await ConnectPortfolioDatabase(portfolio);
            return result.Match(
                Succ: succ =>
                {
                    Logger?.Information($"Switched to Database {portfolio.Signature}");
                    return new Result<bool>(true);
                },
                Fail: err =>
                {
                    Logger?.Error(err, $"Failed to switch to Database {portfolio.Signature}");
                    return new Result<bool>(err);
                });
        }

        private async Task GetPortfolios()
        {
            var loadResult = await LoadPortfoliosFromJson();
            loadResult.Match(
                Right: succes =>
                {
                    return;
                },
                Left: async error => {
                    if (await MigrateFolderStructureIfNeeded())
                    {
                        Portfolios = GetPortfoliosFromFolders();
                        await SavePortfoliosToJson();
                    }
                });

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
                    Logger?.Information($"Connected to Database {portfolio.Signature}");
                    return true;
                }
                else
                {
                    Logger?.Error($"Failed to connect to Database {portfolio.Signature}");
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
                var connectResult = Connect(portfolio);
                if (connectResult.IsFaulted) return new Result<bool>(connectResult.Exception);

                //var relativePath = Path.GetRelativePath(App.AppDataPath, Path.Combine(App.PortfoliosPath, portfolio.Signature, App.DbName));
                //Context = _contextFactory.Create($"Data Source=|DataDirectory|{relativePath}");
                //await CheckDatabase(portfolio.Signature);
                CurrentPortfolio = Portfolios.Where(x => x.Signature == portfolio.Signature).FirstOrDefault();

                _preferenceService.SetLastPortfolio(CurrentPortfolio);
               

                string portfoliosFile = Path.Combine(App.PortfoliosPath, App.PortfoliosFileName);
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

        public async Task<Either<Error,bool>> Connect(Portfolio portfolio)
        {
            try
            {
                var relativePath = Path.GetRelativePath(App.AppDataPath, Path.Combine(App.PortfoliosPath, portfolio.Signature, App.DbName));
                Context = _contextFactory.Create($"Data Source=|DataDirectory|{relativePath}");
                UpdateContext = _updateContextFactory.Create($"Data Source=|DataDirectory|{relativePath}");
                
                await CheckDatabase(portfolio.Signature);
                return true;
            }
            catch (Exception ex)
            {
                return Error.New(ex);
            }
        }

        public Either<Error,bool> Disconnect()
        {
            try
            {
                Context?.Database.CloseConnection();
                Context?.Dispose();
                Context = null;

                UpdateContext?.Database.CloseConnection();
                UpdateContext?.Dispose();
                UpdateContext = null;

                // Force garbage collection to ensure all objects are released
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Logger.Information("Disconnected from the database.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to disconnect from the database.");
                return Error.New(ex);
            }
        }

        private async Task CheckDatabase(string portfolioSignature)
        {
            Logger?.Information($"Checking Database for portfolio {portfolioSignature}");

            if (Context == null)
            {
                Logger?.Error("Failed to retrieve PortfolioContext from the service container.");
                return;
            }

            CreateRestorePoint(portfolioSignature);

            var pendingMigrations = await Context.Database.GetPendingMigrationsAsync();
            var initPriceLevelsEntity = pendingMigrations.Any(m => m.Contains("AddPriceLevelsEntity"));
            var initNarrativesEntity = pendingMigrations.Any(m => m.Contains("AddNarrativesEntity"));

            pendingMigrations = null;

            await Context.Database.MigrateAsync();

            var appliedMigrations = await Context.Database.GetAppliedMigrationsAsync();
            foreach (var migration in appliedMigrations)
            {
                Logger?.Information("Applied Migrations {0}", migration);
            }
        }

        private static void CreateRestorePoint(string portfolioSignature)
        {
            string backupName;

            var backupFolder = Path.Combine(App.PortfoliosPath, portfolioSignature, App.BackupFolder);

            var searchPatternVersion = $"*{App.ProductVersion.Replace(".", "-")}*";

            try
            {
                //Check if RestorePoint for this version already exists
                Directory.CreateDirectory(backupFolder); //ensure directory exists

                var files = Directory.GetFiles(backupFolder, searchPatternVersion);

                if (files.Any())
                {
                    var standardBackupFiles = Directory.GetFiles(backupFolder, "*_s_*");

                    // Sort the files by LastWriteTime
                    var sortedFiles = standardBackupFiles
                        .Select(file => new FileInfo(file))
                        .OrderBy(fileInfo => fileInfo.LastWriteTime)
                        .ToArray();

                    if (sortedFiles.Length > 0)
                    {
                        var timeSpan = DateTime.Now - sortedFiles[sortedFiles.Length - 1].LastWriteTime;

                        // do not create a new restore point if the last one was created less than 2 days ago
                        if (timeSpan.Days < 2) return;

                        if (sortedFiles.Length > 5)
                        {
                            //delete the oldest one, to keep no more then 5 regular restore files
                            File.Delete(sortedFiles[0].FullName);
                        }
                    }
                    backupName = $"{App.PrefixBackupName}_s_{DateTime.Now:yyyyMMdd-HHmmss}.{App.ExtentionBackup}";
                }
                else
                {
                    backupName = $"{App.PrefixBackupName}_{App.ProductVersion.Replace(".", "-")}_{DateTime.Now:yyyyMMdd-HHmmss}.{App.ExtentionBackup}";
                }

                var saveResult = SaveRestorePoint(portfolioSignature, backupName);
                saveResult.Match(
                    Right: succ =>
                    {
                        Logger.Information("Restore Point created successfully for {0}", portfolioSignature);
                    },
                    Left: err =>
                    {
                        Logger.Error("Failed to create Restore Point for {0}: {1}", portfolioSignature, err.Message);
                    });
            }
            catch (Exception ex)
            {
                Logger?.Error(ex, "BackUp CPT files failed!");
            }
        }

        /// <summary>
        /// Creates a restore point of the specified portfolio by copying its database and related files to a temporary folder,
        /// then compressing the folder into a zip file.
        /// </summary>
        /// <param name="portfolioSignature">The unique identifier of the portfolio to back up.</param>
        /// <param name="backupName">The name of the backup file to create, without the path.</param>
        /// <returns>
        /// An Either type containing an Error if the operation fails, or a boolean value indicating success.
        /// </returns>
        public static Either<Error, bool> SaveRestorePoint(string portfolioSignature, string backupName)
        {
            try
            {
                var tempWithSignatureFolder = Path.Combine(App.PortfoliosPath, "Temp", portfolioSignature);
                var tempFolder = Path.Combine(App.PortfoliosPath, "Temp");
                var dbFile = Path.Combine(App.PortfoliosPath, portfolioSignature, App.DbName);
               // var pidFile = Path.Combine(App.PortfoliosPath, portfolioSignature, "pid.json");
               // var preferencesFile = Path.Combine(App.AppDataPath, App.PrefFileName);
                var graphFile = Path.Combine(App.PortfoliosPath, portfolioSignature, "graph.json");
                var backupFolder = Path.Combine(App.PortfoliosPath, portfolioSignature, App.BackupFolder);

                MkOsft.DirectoryCreate(tempFolder, true);
                MkOsft.DirectoryCreate(tempWithSignatureFolder, true);

                MkOsft.FileCopy(dbFile, tempWithSignatureFolder);
               // MkOsft.FileCopy(preferencesFile, tempWithSignatureFolder);
                MkOsft.FileCopy(graphFile, tempWithSignatureFolder);
                //MkOsft.FileCopy(pidFile, tempWithSignatureFolder);

                Directory.CreateDirectory(App.ChartsFolder); //ensure folder is present
                MkOsft.DirectoryCopy(App.ChartsFolder, Path.Combine(tempFolder, "MarketCharts"), true);

                //first create tempZipFile to avoid the exception of the file already exists
                //then move it
                string tempZipPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".cpt");
                ZipFile.CreateFromDirectory(tempFolder, tempZipPath);
                File.Move(tempZipPath, Path.Combine(backupFolder, backupName), true);

                Directory.Delete(tempFolder, true);
            }
            catch (Exception ex)
            {
                return Error.New(ex);
            }
            return true;
        }


        /// <summary>
        /// Creates a backup of the specified portfolio by copying its database and related files to a temporary folder,
        /// then compressing the folder into a zip file.
        /// </summary>
        /// <param name="portfolioSignature">The unique identifier of the portfolio to back up.</param>
        /// <param name="fileName">Full path and name of the backup file to create.</param>
        /// <returns>
        /// An Either type containing an Error if the operation fails, or a boolean value indicating success.
        /// </returns>
        public static Either<Error, bool> SaveBackup(string portfolioSignature, string fileName)
        {
            try
            {
                var tempWithSignatureFolder = Path.Combine(App.PortfoliosPath, "Temp", portfolioSignature);
                var tempFolder = Path.Combine(App.PortfoliosPath, "Temp");
                var dbFile = Path.Combine(App.PortfoliosPath, portfolioSignature, App.DbName);
                var pidFile = Path.Combine(App.PortfoliosPath, portfolioSignature, "pid.json");
                // var preferencesFile = Path.Combine(App.AppDataPath, App.PrefFileName);
                var graphFile = Path.Combine(App.PortfoliosPath, portfolioSignature, "graph.json");
                var backupFolder = Path.Combine(App.PortfoliosPath, portfolioSignature, App.BackupFolder);

                MkOsft.DirectoryCreate(tempFolder, true);
                MkOsft.DirectoryCreate(tempWithSignatureFolder, true);

                MkOsft.FileCopy(dbFile, tempWithSignatureFolder);
                // MkOsft.FileCopy(preferencesFile, tempWithSignatureFolder);
                MkOsft.FileCopy(graphFile, tempWithSignatureFolder);
                MkOsft.FileCopy(pidFile, tempWithSignatureFolder);

                Directory.CreateDirectory(App.ChartsFolder); //ensure folder is present
                MkOsft.DirectoryCopy(App.ChartsFolder, Path.Combine(tempFolder, "MarketCharts"), true);

                //first create tempZipFile to avoid the exception of the file already exists
                //then move it
                string tempZipPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".bak");
                ZipFile.CreateFromDirectory(tempFolder, tempZipPath);
                File.Move(tempZipPath, fileName, true);
                
                Directory.Delete(tempFolder, true);
            }
            catch (Exception ex)
            {
                return Error.New(ex);
            }
            return true;
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

        private static ObservableCollection<Portfolio> GetPortfoliosFromFolders()
        {
            var dirs = Directory.GetDirectories(App.PortfoliosPath);
            var portfolios = new ObservableCollection<Portfolio>();

            foreach (var dir in dirs)
            {
                var pidFilePath = Path.Combine(dir, "pid.json");
                string portfolioId = GetPidFromJson(pidFilePath); ;

                portfolios.Add(new Portfolio
                {
                    Name = portfolioId,
                    Signature = Path.GetRelativePath(App.PortfoliosPath, dir)
                });
            }
            return portfolios;
        }

        private async Task<bool> MigrateFolderStructureIfNeeded()
        {
            bool alreadyMigrated = true;

            if (!Directory.Exists(App.PortfoliosPath))
            {
                string initialPortfolioFolder = App.DefaultPortfolioGuid;
                string fullPortfolioPath = Path.Combine(App.PortfoliosPath, initialPortfolioFolder);
                string oldBackupPath = Path.Combine(App.AppDataPath, App.BackupFolder);
                string newBackupPath = Path.Combine(fullPortfolioPath, "Backup");

                alreadyMigrated = false;

                Directory.CreateDirectory(fullPortfolioPath);
                Directory.CreateDirectory(App.ChartsFolder);
                Directory.CreateDirectory(newBackupPath);

                if (!IsBlankInstall())
                {
                    MkOsft.FileMove(Path.Combine(App.AppDataPath ,"sqlCPT.db"), fullPortfolioPath);
                    MkOsft.FileMove(Path.Combine(App.ChartsFolder, "graph.json"), fullPortfolioPath);
                    MkOsft.FileMove(Path.Combine(App.ChartsFolder, "graph.json.bak"), fullPortfolioPath);
                    MkOsft.DirectoryMove(oldBackupPath, newBackupPath, true);
                    MkOsft.FilesDelete("*_backup_*", App.AppDataPath);
                    RenameBackupFilesToRestorePoints(newBackupPath);
                }

                var portfolio = new Portfolio
                {
                    Name = "Default Portfolio",
                    Signature = initialPortfolioFolder
                };
                Portfolios.Add(portfolio);

                SavePidToJson(portfolio, true);

                string portfoliosFile = Path.Combine(App.PortfoliosPath, App.PortfoliosFileName);
                await SavePortfoliosAsync(portfoliosFile, async stream =>
                {
                    await JsonSerializer.SerializeAsync(stream, Portfolios);
                });
            }
            return alreadyMigrated;
        }

        private static void RenameBackupFilesToRestorePoints(string newBackupPath)
        {
            var files = Directory.GetFiles(newBackupPath, $"*.{App.ExtentionBackup}");
            foreach (var file in files)
            {
                var newFileName = file.Replace("CPTbackup", "RestorePoint");
                File.Move(file, newFileName);
            }
        }

        private static void SavePidToJson(Portfolio portfolio, bool isHidden = false)
        {
            var portfolioNameObject = new { PortfolioName = portfolio.Name };

            var path = Path.Combine(App.PortfoliosPath, portfolio.Signature);

            string jsonString = JsonSerializer.Serialize(portfolioNameObject, new JsonSerializerOptions { WriteIndented = true });
            string filePath = Path.Combine(path, "pid.json");

            try
            {
                // ensure that the file is not hidden
                MkOsft.MakeFileHidden(filePath, true);
                File.WriteAllText(filePath, jsonString);
                if (isHidden) MkOsft.MakeFileHidden(filePath);
            }
            catch (Exception ex)
            {
                Logger?.Error(ex, "Failed to save pid.json file at {0}", filePath);
            }
        }

        private static bool IsBlankInstall()
        {
            var dbFilePath = Path.Combine(App.AppDataPath, App.DbName);
            return !Directory.Exists(App.PortfoliosPath) && !File.Exists(dbFilePath);
        }

        public static ObservableCollection<Backup> GetBackups(string portfolioSignature)
        {
            ObservableCollection<Backup> backups = new();
            var backupPath = Path.Combine(App.PortfoliosPath, portfolioSignature, App.BackupFolder);
            if (Directory.Exists(backupPath))
            {
                var files = Directory.GetFiles(backupPath, $"*.{App.ExtentionBackup}");
                foreach (var file in files)
                {
                    var backup = new Backup { FileName = Path.GetFileName(file), BackupDate = File.GetCreationTime(file) };
                    backups.Add(backup);
                }
                return backups;
            }
            else return new();
        }

        public bool DoesPortfolioNameExist(string name)
        {
            try
            {
                string normalizedName = MkOsft.NormalizeName(name);
                return Portfolios.Any(x => x.Name.ToLower() == normalizedName.ToLower());
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Either<Error,bool>> RenamePortfolio(Portfolio oldPortfolio, Portfolio newPortfolio)
        {
            try
            {
                var portfolio = Portfolios.FirstOrDefault(p => p.Name.ToLower() == oldPortfolio.Name.ToLower());
                if (portfolio != null)
                {
                    portfolio.Name = newPortfolio.Name;
                    await SavePortfoliosToJson();
                    SavePidToJson(portfolio, true);

                    // Notify the UI that the collection has changed
                    var index = Portfolios.IndexOf(portfolio);
                    Portfolios.RemoveAt(index);
                    Portfolios.Insert(index, portfolio);

                    //check if the name of the current portfolio was changed
                    if (CurrentPortfolio.Name == newPortfolio.Name)
                    {
                        OnPropertyChanged("CurrentPortfolio");
                        _preferenceService.SetLastPortfolio(CurrentPortfolio);
                    }

                    ////check if the name of the current portfolio was changed
                    //if (CurrentPortfolio.Name == oldPortfolio.Name)
                    //{
                    //    CurrentPortfolio.Name = newPortfolio.Name;
                    //    _preferenceService.SetLastPortfolio(CurrentPortfolio);
                    //}
                }
                return true;
            }
            catch (Exception ex)
            {
                return Error.New(ex);
            }
        }

        public async Task<Either<Error, bool>> AddPortfolio(Portfolio portfolio, bool needNewSignature = true)
        {
            try
            {
                if (portfolio != null)
                {
                    if (portfolio.Signature == string.Empty || needNewSignature)
                    {
                        portfolio.Signature = Guid.NewGuid().ToString();
                    }
                    var path = Path.Combine(App.PortfoliosPath, portfolio.Signature);
                    var backupPath = Path.Combine(path, App.BackupFolder);

                    MkOsft.DirectoryCreate(backupPath);
                    SavePidToJson(portfolio, true);

                    Portfolios.Add(portfolio);
                    await SavePortfoliosToJson();

                    //// Notify the UI that the collection has changed
                    //var index = Portfolios.IndexOf(portfolio);
                    //Portfolios.RemoveAt(index);
                    //Portfolios.Insert(index, portfolio);

                }
                return true;
            }
            catch (Exception ex)
            {
                return Error.New(ex);
            }
        }

        public async Task<Either<Error, bool>> DeletePortfolio(Portfolio portfolio)
        {
            try
            {
                if (portfolio != null)
                {
                    var path = Path.Combine(App.PortfoliosPath, portfolio.Signature);
                    MkOsft.DirectoryDelete(path, true);
                    Portfolios.Remove(portfolio);
                    await SavePortfoliosToJson();
                    
                }
                return true;
            }
            catch (Exception ex)
            {
                return Error.New(ex);
            }
        }
        private async Task SavePortfoliosToJson()
        {
            string portfoliosFile = Path.Combine(App.PortfoliosPath, App.PortfoliosFileName);
            await SavePortfoliosAsync(portfoliosFile, async stream =>
            {
                await JsonSerializer.SerializeAsync(stream, Portfolios);
                Logger.Information("Portfolios data serialized successfully. {0} portfolios)", Portfolios?.Count);
            });
        }

        private async Task<Either<Error,bool>> LoadPortfoliosFromJson()
        {
            try
            {
                string portfoliosFile = Path.Combine(App.PortfoliosPath, App.PortfoliosFileName);

                if (!File.Exists(portfoliosFile)) return Error.New("File Not Exists");

                await LoadPortfoliosAsync(portfoliosFile, async stream =>
                {
                    Portfolios = await JsonSerializer.DeserializeAsync<ObservableCollection<Portfolio>>(stream);
                    Logger.Information("Portfolios data de-serialized successfully. {0} portfolios)", Portfolios?.Count);
                });
                return true;
            }
            catch (Exception ex)
            {
                return Error.New(ex);
            }
        }

        internal async Task PauseUpdateServices(bool isDisconnecting = false)
        {
            _priceUpdateService.Pause(isDisconnecting);
            _graphUpdateService.Pause(isDisconnecting);

            while (_priceUpdateService.IsUpdating || _graphUpdateService.IsUpdating)
            {
                await Task.Delay(100);
            }
        }

        internal void ResumeUpdateServices()
        {
            _priceUpdateService.Resume();
            _graphUpdateService.Resume();
        }

        public Result<bool> DeleteRestorePoint(string portfolioSignature, string fileName)
        {
            if (portfolioSignature == string.Empty || fileName == string.Empty) return new Result<bool>(false);
            try
            {
                var backupPath = Path.Combine(App.PortfoliosPath, portfolioSignature, App.BackupFolder);
                var filePath = Path.Combine(backupPath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to remove restore point {fileName} from portfolio {portfolioSignature}");
                return new Result<bool>(ex);
            }
            
        }

        internal async Task<Either<Error, bool>> CopyPortfolio(Portfolio portfolio, Portfolio newPortfolio, bool needPartialCopy)
        {
            newPortfolio.Signature = Guid.NewGuid().ToString();
            var destSignaturePath = Path.Combine(App.PortfoliosPath, newPortfolio.Signature);
            var sourceSignaturePath = Path.Combine(App.PortfoliosPath, portfolio.Signature);

            try
            {
                MkOsft.DirectoryCopy(sourceSignaturePath, destSignaturePath, false);
                SavePidToJson(newPortfolio, true);
                Portfolios.Add(newPortfolio);
                await SavePortfoliosToJson();

                if (needPartialCopy)
                {
                    // use UpdateContext to temporarely connect with the new portfolio
                    // for that pause both UpdateServices
                    await PauseUpdateServices();
                    DisconnectUpdateContext();
                    await ConnectUpdateContext(newPortfolio);

                    await RemoveAssetsFromPortfolio(UpdateContext);

                    DisconnectUpdateContext();
                    await ConnectUpdateContext(portfolio);
                    ResumeUpdateServices();
                }
                return true;
            }
            catch (Exception ex)
            {
                return Error.New(ex);
            }
        }

        private async Task RemoveAssetsFromPortfolio(UpdateContext context)
        {
            if (context == null) { return; };

            context.Assets.RemoveRange(context.Assets);
            context.Transactions.RemoveRange(context.Transactions);
            foreach(var coin in context.Coins)
            {
                coin.IsAsset = false;
            }
            await context.SaveChangesAsync();

        }

        public Task<Either<Error, bool>> ConnectUpdateContext(Portfolio portfolio)
        {
            try
            {
                var relativePath = Path.GetRelativePath(App.AppDataPath, Path.Combine(App.PortfoliosPath, portfolio.Signature, App.DbName));
                UpdateContext = _updateContextFactory.Create($"Data Source=|DataDirectory|{relativePath}");
                Logger.Information($"UpdateContext Connected to {portfolio.Signature}");
                return Task.FromResult<Either<Error, bool>>(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to connect UpdateContext to {portfolio.Signature}");
                return Task.FromResult<Either<Error, bool>>(Error.New(ex));
            }
        }

        public Either<Error, bool> DisconnectUpdateContext()
        {
            try
            {
                UpdateContext?.Database.CloseConnection();
                UpdateContext?.Dispose();
                UpdateContext = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                Logger.Information($"UpdateContext Disconnected");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to disconnect UpdateContext.");
                return Error.New(ex);
            }
        }

        internal static string GetPidFromJson(string? pidFile)
        {
            string pid = string.Empty;
            try
            {
                if (File.Exists(pidFile))
                {
                    var jsonString = File.ReadAllText(pidFile);
                    var jsonDoc = JsonDocument.Parse(jsonString);
                    if (jsonDoc.RootElement.TryGetProperty("PortfolioName", out var nameElement))
                    {
                        pid = nameElement.GetString() ?? pid;
                    }
                }
                return pid;
            }
            catch (Exception ex)
            {
                Logger?.Error(ex, "Failed to read or parse pid.json in file {0}", pidFile);
                return string.Empty;
            }
        }

        internal Portfolio GetPortfolioBySignature(string signature)
        {
           return Portfolios.Where(x => x.Signature == signature).FirstOrDefault();
        }
    }
}