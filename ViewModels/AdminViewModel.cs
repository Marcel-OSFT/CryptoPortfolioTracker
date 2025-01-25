using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Serilog;
using System.Threading.Tasks;
using System;
using Serilog.Core;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Dialogs;
using System.Linq;
using LanguageExt;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinUI3Localizer;
using LanguageExt.Common;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.IO;
using CryptoPortfolioTracker.Helpers;
using CryptoPortfolioTracker.Enums;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CryptoPortfolioTracker.Views;
using Microsoft.UI;
using Windows.UI;
using System.Collections.Generic;
using System.Diagnostics;

namespace CryptoPortfolioTracker.ViewModels
{

    [ObservableObject]
    public partial class Backup
    {
        [ObservableProperty] private string fileName;
        public DateTime BackupDate { get; set; }
    }
   
    public partial class AdminViewModel : BaseViewModel
    {
        public static AdminViewModel Current;
        private readonly IMessenger _messenger;
        private readonly IPreferencesService _preferencesService;
        public PortfolioService _portfolioService { get; private set; }
        [ObservableProperty] private ObservableCollection<Backup> backups = new();
        [ObservableProperty] private Backup? selectedBackup;
        [ObservableProperty] private string? selectedBrowseFolder;
        [ObservableProperty] private Portfolio? selectedPortfolio;
        
        partial void OnSelectedPortfolioChanged(Portfolio? oldValue, Portfolio? newValue)
        {
            Backups = newValue != null ? PortfolioService.GetBackups(newValue.Signature) : new();
            SelectedBackup = Backups.FirstOrDefault();
            
        }

        public AdminViewModel(PortfolioService portfolioService, IMessenger messenger, IPreferencesService preferencesService) : base(preferencesService)
        {
            Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AdminViewModel).Name.PadRight(22));
            Current = this;
            _preferencesService = preferencesService;
            _portfolioService = portfolioService;
            _messenger = messenger;
        }

        /// <summary>  
        /// This method is called by the AdminPortfolioView_Loading event.  
        /// </summary>  
        public void AdminViewLoading()
        {
            SelectedPortfolio =  _portfolioService.CurrentPortfolio;
            
        }
        
        [RelayCommand]
        private async void OpenPickFolder()
        {
            StorageFolder selectedFolder = await PickFolderAsync();
            if (selectedFolder != null)
            {
                // Handle the selected folder
                SelectedBrowseFolder = selectedFolder.Path;
            }
            else
            {
                SelectedBrowseFolder = "No folder was selected.";
            }
        }

        private async Task<StorageFolder> PickFolderAsync()
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            // WinUI 3 requires setting the window handle for the picker
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            return folder;

        }

        [RelayCommand]
        private async void AddPortfolio()
        {
            var loc = Localizer.Get();
            
            // Show the rename dialog
            PortfolioDialog dialog = new PortfolioDialog(_preferencesService, this);

            dialog.RequestedTheme = _preferencesService.GetAppTheme();
            dialog.XamlRoot = MainPage.Current.XamlRoot;

            var result = await App.ShowContentDialogAsync(dialog);
            var portfolio = dialog.newPortfolio;

            if (result == ContentDialogResult.Primary && portfolio is not null)
            {
                Logger.Information("Adding Portfolio ({0})", portfolio.Name);
                var renameResult = await _portfolioService.AddPortfolio(portfolio);
                renameResult.Match(
                    Right: _ =>
                    {

                        Logger.Information("Adding Portfolio successful");
                    },
                    Left: async error =>
                    {
                        await ShowMessageDialog(
                        loc.GetLocalizedString("Messages_PortfolioAddFailed_Title"),
                        error.Message,
                        loc.GetLocalizedString("Common_CloseButton"));
                        Logger.Error(error, "Adding Portfolio failed");
                    });
            }
            
        }

        [RelayCommand]
        private async void RenamePortfolio(Portfolio portfolio)
        {
            var loc = Localizer.Get();
            if (SelectedPortfolio != null)
            {
                // Show the rename dialog
                PortfolioDialog dialog = new PortfolioDialog(_preferencesService, this, Enums.DialogAction.Edit, portfolio);

                dialog.RequestedTheme = _preferencesService.GetAppTheme();
                dialog.XamlRoot = MainPage.Current.XamlRoot;

                var result =  await App.ShowContentDialogAsync(dialog);

                if (result == ContentDialogResult.Primary && dialog.newPortfolio is not null)
                {
                    Logger.Information("Renaming Portfolio ({0})", portfolio.Name);
                    var renameResult = await _portfolioService.RenamePortfolio(portfolio, dialog.newPortfolio);
                    renameResult.Match(
                        Right: _ =>
                        {
                            SelectedPortfolio = portfolio;
                            Logger.Information("Renaming Portfolio successful");
                        },
                        Left: async error =>
                        {
                            await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_PortfolioRenameFailed_Title"),
                            error.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(error, "Renaming Portfolio failed");
                        });
                }
            }
        }

        //private void UpdatePortfoliosExt(Portfolio oldPortfolio, Portfolio newPortfolio)
        //{
        //    var portfolioExt = Portfolios.FirstOrDefault(x => x.Name.ToLower() == oldPortfolio.Name.ToLower());

        //    if (portfolioExt != null)
        //    {
        //        portfolioExt.Name = newPortfolio.Name;
        //        // Notify the UI that the collection has changed
        //        var index = Portfolios.IndexOf(portfolioExt);
        //        Portfolios.RemoveAt(index);
        //        Portfolios.Insert(index, portfolioExt);
        //    }
        //}

        [RelayCommand]
        private async void CopyPortfolio(Portfolio portfolio)
        {
            if (portfolio != null)
            {
                // Show the copy dialog
                CopyPortfolioDialog dialog = new CopyPortfolioDialog(_preferencesService, this, portfolio);

                dialog.RequestedTheme = _preferencesService.GetAppTheme();
                dialog.XamlRoot = MainPage.Current.XamlRoot;

                var result = await App.ShowContentDialogAsync(dialog);

                if (result == ContentDialogResult.Primary && dialog.NewPortfolioName != string.Empty)
                {
                    var newPortfolio = new Portfolio { Name = dialog.NewPortfolioName };
                    Logger.Information("Copying Portfolio ({0})", portfolio.Name);
                    var copyResult = await _portfolioService.CopyPortfolio(portfolio, newPortfolio, dialog.NeedPartialCopy);
                    copyResult.Match(
                        Right: _ =>
                        {
                            Logger.Information("Copying Portfolio successful");
                        },
                        Left: async error =>
                        {
                            var loc = Localizer.Get();
                            await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_PortfolioCopyFailed_Title"),
                            error.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(error, "Copying Portfolio failed");
                        });
                }
            }
        }


        [RelayCommand(CanExecute = nameof(CanDeletePortfolio))]
        private void DeletePortfolioDummy(Portfolio portfolio)
        {
            // this command is dummy because the final command is run through the Flyout that 
            // shows when the user clicks on the delete button.
        }
        

        [RelayCommand]
        private async void DeletePortfolio(Portfolio portfolio)
        {
            //*** what are the conditions to allow deletion of a portfolio? ***
            //*** - no restrictions, except that active portfolio can't be deleted
            //*** - must have at least one portfolio to remain after deletion, which 
            //***   will follow automaticcaly out previous point
            //*** - it is up to the user to make a backup prior to deletion. Option is available so don't have to take care of here

            if (portfolio is null) return;

            if (portfolio != _portfolioService.CurrentPortfolio)
            {
                var deleteResult = await _portfolioService.DeletePortfolio(portfolio);
                deleteResult.Match(
                    Right: _ =>
                    {
                        SelectedPortfolio = _portfolioService.CurrentPortfolio;
                        Logger.Information("Succesfully deleted Portfolio ({0})", portfolio.Name);
                    },
                    Left: async error =>
                    {
                        var loc = Localizer.Get();
                        await ShowMessageDialog(
                        loc.GetLocalizedString("Messages_PortfolioDeleteFailed_Title"),
                        error.Message,
                        loc.GetLocalizedString("Common_CloseButton"));
                        Logger.Error(error, "Deleting Portfolio failed");
                    });
            }
        }

        private bool CanDeletePortfolio(Portfolio portfolio)
        {
            return SelectedPortfolio != _portfolioService.CurrentPortfolio;
        }


        [RelayCommand(CanExecute = nameof(CanBackupPortfolio))]
        private async void BackupPortfolio(Portfolio portfolio) //****** to be implemented and checked
        {
            if (portfolio != null)
            {
                string backupFilename = await GetBackupFilenameAsync();
                if (!string.IsNullOrEmpty(backupFilename))
                {
                    var backupResult = PortfolioService.SaveBackup(portfolio.Signature, backupFilename);
                    backupResult.Match(
                        Right: succ =>
                        {
                            Logger.Information("Backup of portfolio {PortfolioName} successful", portfolio.Name);
                        },
                        Left: err =>
                        {
                            Logger.Error("Backup of portfolio {PortfolioName} failed: {ErrorMessage}", portfolio.Name, err.Message);
                        }
                    );
                }
            }
        }

        private bool CanBackupPortfolio(Portfolio portfolio)
        {
            return SelectedPortfolio != null;
        }

        public async Task<string> GetBackupFilenameAsync()
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Backup File", new List<string>() { ".bak" });
            savePicker.SuggestedFileName = "backup";

            // WinUI requires a window handle for the picker
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            StorageFile file = await savePicker.PickSaveFileAsync();
            return file?.Path;
        }

        [RelayCommand]
        private async void RestorePortfolio(Portfolio portfolio)
        {
            if (portfolio == null || SelectedBackup == null) return;

            if (_portfolioService.CurrentPortfolio == portfolio)
            {
                var isSuccesfull = await PauseServicesAndDisconnect(portfolio);
                if (isSuccesfull)
                {
                    var sourcePathZip = Path.Combine(App.PortfoliosPath, portfolio.Signature, App.BackupFolder);
                    var backupFilePath = Path.Combine(sourcePathZip, SelectedBackup.FileName);
                    await DoRestore(portfolio, backupFilePath);
                }
                await ResumeServicesAndConnect(portfolio);
            }
            else
            {
                var sourcePathZip = Path.Combine(App.PortfoliosPath, portfolio.Signature, App.BackupFolder);
                var backupFilePath = Path.Combine(sourcePathZip, SelectedBackup.FileName);

                var restoreResult = await DoRestore(portfolio, backupFilePath);
                restoreResult.Match(
                    Right: succ =>
                    {
                        _messenger.Send(new ShowAnimationMessage(Colors.Green));
                    },
                    Left: err =>
                    {
                        _messenger.Send(new ShowAnimationMessage(Colors.Red));
                    });
            }
        }

        private async Task ResumeServicesAndConnect(Portfolio portfolio)
        {
            var connectResult = await _portfolioService.Connect(portfolio);
            connectResult.Match(
                Right: _ =>
                {
                    Logger.Information("Reconnected to portfolio {PortfolioName}", portfolio.Name);
                    _portfolioService.ResumeUpdateServices();
                    _messenger.Send(new ShowAnimationMessage(Colors.Green));
                },
                Left: async error =>
                {
                    var loc = Localizer.Get();
                    await ShowMessageDialog(
                        loc.GetLocalizedString("Messages_PortfolioRestoreConnectFailed_Title"),
                        error.Message,
                        loc.GetLocalizedString("Common_CloseButton"));
                    Logger.Error(error, "Reconnecting to portfolio {PortfolioName} failed, try to restore from another Restore Point", portfolio.Name);
                    _messenger.Send(new ShowAnimationMessage(Colors.Red));
                });
        }

        private async Task<bool> PauseServicesAndDisconnect(Portfolio portfolio)
        {
            bool isSuccesfull = false;

            await _portfolioService.PauseUpdateServices(true);
            var disconnectResult = _portfolioService.Disconnect();

            disconnectResult.Match(
                Right: _ =>
                {
                    // Introduce a short delay to ensure the file handles are released
                    System.Threading.Thread.Sleep(100);
                    Logger.Information("Disconnected from portfolio {PortfolioName}", portfolio.Name);
                    isSuccesfull = true;
                },
                Left: async error =>
                {
                    var loc = Localizer.Get();
                    await ShowMessageDialog(
                        loc.GetLocalizedString("Messages_PortfolioRestoreDisconnectFailed_Title"),
                        error.Message,
                        loc.GetLocalizedString("Common_CloseButton"));
                    Logger.Error(error, $"Disconnecting from portfolio {portfolio.Name} failed. Restore cannot be applied.");
                    _messenger.Send(new ShowAnimationMessage(Colors.Red));
                    isSuccesfull = false;
                });
            return isSuccesfull;
        }

        private async Task<Either<Error,bool>> DoRestore(Portfolio portfolio, string backupFilePath)
        {
            var tempFolder = Path.Combine(App.AppDataPath, "temp");
            Either<Error, bool> result = new();
            try
            {
                var restoreSignatureResult = RestoreSignatureFolder(tempFolder, portfolio, backupFilePath);
                switch (restoreSignatureResult)
                {
                    case RestoreResult.Succesfull:
                        {
                            var restoreChartsResult = RestoreNeededMarketCharts(Path.Combine(tempFolder, "MarketCharts"), App.ChartsFolder);
                            restoreChartsResult.Match(
                                Right: _ =>
                                {
                                    // show succesfull-animation
                                    result = true;
                                },
                                Left: async err =>
                                {
                                    var loc = Localizer.Get();
                                    await ShowMessageDialog(
                                    loc.GetLocalizedString("Messages_PortfolioRestoreWarning_Title"),
                                    loc.GetLocalizedString("Messages_PortfolioRestoreWarning_Message"),
                                    loc.GetLocalizedString("Common_CloseButton"));
                                    Logger.Error(err, "Restore of portfolio {PortfolioName} succesfull, but with a warning on restoring MarketCharts", portfolio.Name);
                                    result = true;

                                    //show unSuccesfull-animation
                                });
                            break;
                        }
                    case RestoreResult.ErrorNonCritical:
                    case RestoreResult.None:
                    case RestoreResult.ErrorCriticalCanContinue:
                        {
                            Logger.Error($"Restoring Signature Folder returned unsuccesfull (ErrorCriticalCanContinue) -> continued with existing portfolio");
                            var loc = Localizer.Get();
                            await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_PortfolioRestoreCriticalError_Title"),
                            loc.GetLocalizedString("Messages_PortfolioRestoreCriticalError_Message"),
                            loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error($"Restore of portfolio {portfolio.Name} failed! No changes where made");
                            result = true;
                            break;
                        }
                    case RestoreResult.ErrorCriticalNeedRecovery:
                        {
                            Logger.Error($"Restoring Signature Folder returned unsuccesfull (ErrorCriticalNeedRecovery) -> Changes need to be reversed");
                            RevertRestore(portfolio);

                            var loc = Localizer.Get();
                            await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_PortfolioRestoreCriticalError_Title"),
                            loc.GetLocalizedString("Messages_PortfolioRestoreCriticalError_Message"),
                            loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error($"Restore of portfolio {portfolio.Name} failed! Changes are reverted");
                            result = true;
                            break;
                        }
                    default: break;
                }
                return result;
            }
            catch (Exception ex)
            {
                return Error.New(ex.Message);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempFolder, true);
                }
                catch { }
            }
        }

        [RelayCommand(CanExecute = nameof(CanRestorePortfolioDummy))]
        private async void RestorePortfolioDummy(Portfolio portfolio)
        {
            // this command is dummy because the final command is run through the Flyout that 
            // shows when the user clicks on the restore button.
        }

        private bool CanRestorePortfolioDummy(Portfolio portfolio)
        {
            return SelectedBackup != null;
        }

        private RestoreResult RestoreSignatureFolder(string tempFolder, Portfolio portfolio, string backupFilePath)
        {
            var signatureDestPath = Path.Combine(App.PortfoliosPath, portfolio.Signature);
            var signatureTempPath = Path.Combine(tempFolder, portfolio.Signature);
            

            RestoreResult result = RestoreResult.None;

            try
            {
                if (Directory.Exists(tempFolder))
                {
                    // ensure an empty Temp folder
                    Directory.Delete(tempFolder, true);
                }

                ZipFile.ExtractToDirectory(backupFilePath, tempFolder);

                // Confirm existence of sqlCPT.db in tempFolder
                if (!File.Exists(Path.Combine(signatureTempPath, "sqlCPT.db")))
                {
                    Logger.Error("Database not found in backup");
                    return RestoreResult.ErrorCriticalCanContinue;
                }

                //Check if Destination folder exists. It might be a restore from a backup of a currently non-existing portfolio 
                if (!Directory.Exists(signatureDestPath))
                {
                    Directory.CreateDirectory(signatureDestPath);
                }

                SecureMandatoryFiles(signatureDestPath);

                var moveResult = MoveFilesToDestination(tempFolder, signatureDestPath, signatureTempPath);
                moveResult.Match(
                    Right: _ =>
                    {
                        result = RestoreResult.Succesfull;
                    },
                    Left: err =>
                    {
                        result = RestoreResult.ErrorCriticalNeedRecovery;
                    });
                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        private Either<Error, bool> MoveFilesToDestination(string tempFolder, string signatureDestPath, string signatureTempPath)
        {
            // Check if tempFolder contains the signature folder or single files (old style backup)
            var sourcePath = Directory.Exists(signatureTempPath) ? signatureTempPath : tempFolder;
            var files = Directory.GetFiles(sourcePath, "*.*");
            try
            {
                foreach (var file in files)
                {
                    //if pidFile exists at destination then skip moving it
                    if (file.Contains("pid.json"))
                    {
                        var destFile = Path.Combine(signatureDestPath, Path.GetFileName(file));
                        if (File.Exists(destFile))
                        {
                            continue;
                        }
                    }   
                    MkOsft.FileMove(file, signatureDestPath);
                }
                CleanUpSignatureFolder(signatureDestPath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error moving files to destination");

                return Error.New(ex.Message);
            }
        }

        private static void SecureMandatoryFiles(string signatureDestPath)
        {
            try
            {
                var files = Directory.GetFiles(signatureDestPath);
                foreach (var file in files)
                {
                    if (!file.Contains("pid.json"))
                    {
                        File.Move(file, file + ".sec");
                    }
                }
            }
            catch { }
        }

        private static void RevertRestore(Portfolio portfolio)
        {
            var signatureDestPath = Path.Combine(App.PortfoliosPath, portfolio.Signature);
            try
            {
                var files = Directory.GetFiles(signatureDestPath, "*.sec");
                foreach (var file in files)
                {
                    var destFile = file.Substring(0, file.Length - 4);
                    File.Move(file, destFile, true);
                }
            }
            catch { }
        }


        private Either<Error,bool> RestoreNeededMarketCharts(string chartsTempPath, string chartsFolder)
        {
            //*** what are the conditions to restore market charts? ***
            //*** - restore if MarketChart does not exist
            //*** - restore if MarketChart exists but is older than the one in the backup
            try
            {
                if (!Directory.Exists(chartsTempPath)) return true;
                
                if (!Directory.Exists(chartsFolder))
                {
                    MkOsft.DirectoryMove(chartsTempPath, chartsFolder, true);
                }
                else
                {
                    var tempFiles = Directory.GetFiles(chartsTempPath, "MarketChart*");
                    var destFiles = Directory.GetFiles(chartsFolder, "MarketChart*");
                    foreach (var tempFile in tempFiles)
                    {
                        var destFile = destFiles.FirstOrDefault(x => Path.GetFileName(x) == Path.GetFileName(tempFile));
                        if (destFile == null)
                        {
                            File.Move(tempFile, Path.Combine(chartsFolder, Path.GetFileName(tempFile)));
                        }
                        else
                        {
                            var tempFileInfo = new FileInfo(tempFile);
                            var destFileInfo = new FileInfo(destFile);
                            if (tempFileInfo.LastWriteTime > destFileInfo.LastWriteTime)
                            {
                                File.Delete(destFile);
                                File.Move(tempFile, Path.Combine(chartsFolder, Path.GetFileName(tempFile)));
                            }
                        }
                    }
                }
                CleanUpMarketChartsFolder();
                return true;
            }
            catch (Exception ex)
            {
                return Error.New(ex.Message);
            }
        }


        private void CleanUpMarketChartsFolder()
        {
            try
            {
                if (!Directory.Exists(App.ChartsFolder)) return;
                var filesToRemove = Directory.GetFiles(App.ChartsFolder)
                                              .Where(file => !Path.GetFileName(file).StartsWith("MarketChart"))
                                              .ToList();
                foreach (var file in filesToRemove)
                {
                    File.Delete(file);
                }
            }
            catch { }
            
        }
        private static void CleanUpSignatureFolder(string signatureDestPath)
        {
            var prefsFile = Path.Combine(signatureDestPath, "prefs.xml");
            var graphBakFile = Path.Combine(signatureDestPath, "graph.json.bak");

            try
            {
                if (File.Exists(prefsFile)) File.Delete(prefsFile);
                if (File.Exists(graphBakFile)) File.Delete(graphBakFile);
            }
            catch {  }

        }

        [RelayCommand]
        private void BrowseBackup()
        {
            // Implement browse logic to allow user to select a backup file
        }

        [RelayCommand(CanExecute = nameof(CanDeleteRestorePoint))]
        private void DeleteRestorePoint(Backup backup)
        {
            if (backup != null)
            {
                var deleteResult = _portfolioService.DeleteRestorePoint(selectedPortfolio.Signature,backup.FileName);
                if (deleteResult.IsSuccess)
                {
                    Backups.Remove(backup);
                }
                if (Backups.Any()) SelectedBackup = Backups.First();
            }
        }

        private bool CanDeleteRestorePoint(Backup backup)
        {
            return SelectedBackup != null;
        }

        [RelayCommand]
        private async void BrowseAndRestorePortfolio()
        {

            // Show the copy dialog
            RestorePortfolioDialog dialog = new RestorePortfolioDialog(_portfolioService, _preferencesService, this);

            dialog.RequestedTheme = _preferencesService.GetAppTheme();
            dialog.XamlRoot = MainPage.Current.XamlRoot;

            var result = await App.ShowContentDialogAsync(dialog);

            if (result == ContentDialogResult.Primary && dialog.Portfolio != null)
            {

                if (dialog.Portfolio == _portfolioService.CurrentPortfolio)
                {
                    var isSuccesfull = await PauseServicesAndDisconnect(dialog.Portfolio);
                    if (isSuccesfull)
                    {
                        var restoreResult = await DoRestore(dialog.Portfolio, dialog.FileName);
                        restoreResult.Match(
                            Right: succ =>
                            {
                                _messenger.Send(new ShowAnimationMessage(Colors.Green, true, dialog.Portfolio));
                            },
                            Left: err =>
                            {
                                _messenger.Send(new ShowAnimationMessage(Colors.Red, true, dialog.Portfolio));
                            });
                    }
                    await ResumeServicesAndConnect(dialog.Portfolio);
                }
                else
                {
                    var restoreResult = await DoRestore(dialog.Portfolio, dialog.FileName);
                    restoreResult.Match(
                        Right: succ =>
                        {
                            _messenger.Send(new ShowAnimationMessage(Colors.Green, true, dialog.Portfolio));
                        },
                        Left: err =>
                        {
                            _messenger.Send(new ShowAnimationMessage(Colors.Red, true, dialog.Portfolio));
                        });
                }




               
            }
        }
        

    }
}
