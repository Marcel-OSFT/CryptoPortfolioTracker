using System.Security.Cryptography;
using Windows.Security.Credentials;

namespace CryptoPortfolioTracker.ViewModels;

public partial class SettingsViewModel : BaseViewModel, INotifyPropertyChanged
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static SettingsViewModel Current;
    public Settings AppSettings => base.AppSettings; // expose AppSettings publicly so that it can be used in dialogs called by this ViewModel


#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    [ObservableProperty]
    private string password;

    [ObservableProperty]
    private string duressPassword;

    [ObservableProperty]
    private string currentPassword;

    [ObservableProperty]
    private string newPassword;

    [ObservableProperty]
    private string currentDuressPassword;

    [ObservableProperty]
    private string newDuressPassword;

    

    [ObservableProperty]
    private ElementTheme appTheme;
    partial void OnAppThemeChanged(ElementTheme value) => AppSettings.AppTheme = value;

    [ObservableProperty]
    private int numberFormatIndex;
    partial void OnNumberFormatIndexChanged(int value) => SetNumberSeparatorsFromIndex(value);
    
    [ObservableProperty]
    private int appCultureIndex;
    partial void OnAppCultureIndexChanged(int value) => SetCulturePreferenceFromIndex(value);

    [ObservableProperty]
    private double fontSize;
    partial void OnFontSizeChanged(double value) => AppSettings.FontSize = (AppFontSize)value;

    [ObservableProperty]
    private bool isCheckForUpdate;
    partial void OnIsCheckForUpdateChanged(bool value) => AppSettings.IsCheckForUpdate = value;

    [ObservableProperty]
    private bool isScrollBarsExpanded;
    partial void OnIsScrollBarsExpandedChanged(bool value) => AppSettings.IsScrollBarsExpanded = value;

    [ObservableProperty]
    private bool areValuesMasked;
    partial void OnAreValuesMaskedChanged(bool value) => AppSettings.AreValuesMasked = value;

    [ObservableProperty]
    private bool isPasswordSet;

    [ObservableProperty]
    private bool isDuressPasswordSet;

    /// <summary>
    /// Checks if credentials exist in the Windows Credential Locker and sets IsPasswordSet/IsDuressPasswordSet accordingly.
    /// Call this from InitializeFields or when loading the SettingsView.
    /// </summary>
    public void CheckPasswordCredentials()
    {
        var vault = new PasswordVault();

        // Check main password
        try
        {
            var credential = vault.Retrieve("CryptoPortfolioTracker", "Password");
            IsPasswordSet = credential != null;
        }
        catch
        {
            IsPasswordSet = false;
        }

        // Check duress password
        try
        {
            var credential = vault.Retrieve("CryptoPortfolioTracker", "DuressPassword");
            IsDuressPasswordSet = credential != null;
        }
        catch
        {
            IsDuressPasswordSet = false;
        }
    }

    public SettingsViewModel(Settings appSettings) : base(appSettings)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(SettingsViewModel).Name.PadRight(22));
        Current = this;

        InitializeFields();
    }

    private void InitializeFields()
    {
        IsCheckForUpdate = AppSettings.IsCheckForUpdate;
        FontSize = (double)AppSettings.FontSize;
        IsScrollBarsExpanded = AppSettings.IsScrollBarsExpanded;
        AppTheme = AppSettings.AppTheme;
        NumberFormatIndex = AppSettings.NumberFormat.NumberDecimalSeparator == "," ? 0 : 1;
        AppCultureIndex = AppSettings.AppCultureLanguage[..2].ToLower() == "nl" ? 0 : 1;
        
        CheckPasswordCredentials();
    }

    
    private void SetCulturePreferenceFromIndex(int index)
    {
        string language = index == 0 ? "nl" : "en-US";
        AppSettings.AppCultureLanguage = language;
    }

    private void SetNumberSeparatorsFromIndex(int index)
    {
        var nf = new NumberFormatInfo
        {
            NumberDecimalSeparator = index == 0 ? "," : ".",
            NumberGroupSeparator = index == 0 ? "." : ","
        };
        AppSettings.NumberFormat = nf;
    }

    [RelayCommand]
    public async Task CheckUpdateNow()
    {
        Logger.Information("Checking for updates");
        var appUpdater = new AppUpdater();
        var loc = Localizer.Get();
        var result = await appUpdater.Check(AppConstants.VersionUrl, AppConstants.ProductVersion);

        if (result == AppUpdaterResult.NeedUpdate)
        {
            Logger.Information("Update Available");
            var dlgResult = await ShowMessageDialog(
                loc.GetLocalizedString("Messages_UpdateChecker_NewVersionTitle"),
                loc.GetLocalizedString("Messages_UpdateChecker_NewVersionMsg"),
                loc.GetLocalizedString("Common_DownloadButton"),
                loc.GetLocalizedString("Common_CancelButton"));

            if (dlgResult == ContentDialogResult.Primary)
            {
                Logger.Information("Downloading update");
                var downloadResult = await appUpdater.DownloadSetupFile();
                
                if (downloadResult == AppUpdaterResult.DownloadSuccesfull)
                {
                    //*** wait till there is no other dialog box open
                    await App.DialogCompletionTask;
                    Logger.Information("Download Succesfull");
                    var installRequest = await ShowMessageDialog(
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadSuccesTitle"),
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadSuccesMsg"),
                        loc.GetLocalizedString("Common_InstallButton"),
                        loc.GetLocalizedString("Common_CancelButton"));

                    if (installRequest == ContentDialogResult.Primary)
                    {
                        Logger.Information("Closing Application and Installing Update");
                        appUpdater.ExecuteSetupFile();
                    }
                }
                else
                {
                    Logger.Warning("Download failed");
                    await ShowMessageDialog(
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadFailedTitle"),
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadFailedMsg"),
                        loc.GetLocalizedString("Common_CloseButton"));
                }
            }
        }
        else
        {
            Logger.Information("Application is up-to-date");
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_UpdateChecker_UpToDate_Title"),
                loc.GetLocalizedString("Messages_UpdateChecker_UpToDate_Msg"),
                loc.GetLocalizedString("Common_OkButton"));
        }
    }

    [RelayCommand]
    private async Task ChangePassword()
    {
        var vault = new PasswordVault();
        var loc = Localizer.Get();

        if (!IsPasswordSet)
        {
            // No password set yet, allow setting new password directly
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                await ShowMessageDialog(
                    loc.GetLocalizedString("Messages_Password_New_Required_Title"),
                    loc.GetLocalizedString("Messages_Password_New_Required_Msg"),
                    loc.GetLocalizedString("Common_OkButton"));
                return;
            }

            var newHash = AuthenticationService.HashPassword(NewPassword);
            vault.Add(new PasswordCredential("CryptoPortfolioTracker", "Password", newHash));
            IsPasswordSet = true;
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_Password_Set_Title"),
                loc.GetLocalizedString("Messages_Password_Set_Msg"),
                loc.GetLocalizedString("Common_OkButton"));
        }
        else
        {
            // Password exists, require current password
            if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
                return;
            var credential = vault.Retrieve("CryptoPortfolioTracker", "Password");
            string storedHash = credential.Password;
            if (AuthenticationService.VerifyPassword(CurrentPassword, storedHash))
            {
                var newHash = AuthenticationService.HashPassword(NewPassword);
                vault.Remove(credential);
                vault.Add(new PasswordCredential("CryptoPortfolioTracker", "Password", newHash));
                await ShowMessageDialog(
                    loc.GetLocalizedString("Messages_Password_Changed_Title"),
                    loc.GetLocalizedString("Messages_Password_Changed_Msg"),
                    loc.GetLocalizedString("Common_OkButton"));
            }
            else
            {
                await ShowMessageDialog(
                    loc.GetLocalizedString("Messages_Password_Incorrect_Title"),
                    loc.GetLocalizedString("Messages_Password_Incorrect_Msg"),
                    loc.GetLocalizedString("Common_OkButton"));
            }
        }

        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
    }

    [RelayCommand]
    private async Task ChangeDuressPassword()
    {
        var loc = Localizer.Get();
        var vault = new PasswordVault();
        if (!IsDuressPasswordSet)
        {
            // No password set yet, allow setting new password directly
            if (string.IsNullOrWhiteSpace(NewDuressPassword))
            {
                await ShowMessageDialog(
                    loc.GetLocalizedString("Messages_duressPassword_New_Required_Title"),
                    loc.GetLocalizedString("Messages_duressPassword_New_Required_Msg"),
                    loc.GetLocalizedString("Common_OkButton"));
                return;
            }

            var newHash = AuthenticationService.HashPassword(NewDuressPassword);
            vault.Add(new PasswordCredential("CryptoPortfolioTracker", "DuressPassword", newHash));
            IsDuressPasswordSet = true;
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_duressPassword_Set_Title"),
                loc.GetLocalizedString("Messages_duressPassword_Set_Msg"),
                loc.GetLocalizedString("Common_OkButton"));
        }
        else
        {
            // Password exists, require current password
            if (string.IsNullOrWhiteSpace(CurrentDuressPassword) || string.IsNullOrWhiteSpace(NewDuressPassword))
                return;
            var credential = vault.Retrieve("CryptoPortfolioTracker", "DuressPassword");
            string storedHash = credential.Password;
            if (AuthenticationService.VerifyPassword(CurrentDuressPassword, storedHash))
            {
                var newHash = AuthenticationService.HashPassword(NewDuressPassword);
                vault.Remove(credential);
                vault.Add(new PasswordCredential("CryptoPortfolioTracker", "DuressPassword", newHash));
                await ShowMessageDialog(
                    loc.GetLocalizedString("Messages_duressPassword_Changed_Title"),
                    loc.GetLocalizedString("Messages_duressPassword_Changed_Msg"),
                    loc.GetLocalizedString("Common_OkButton"));
            }
            else
            {
                await ShowMessageDialog(
                    loc.GetLocalizedString("Messages_duressPassword_Incorrect_Title"),
                    loc.GetLocalizedString("Messages_duressPassword_Incorrect_Msg"),
                    loc.GetLocalizedString("Common_OkButton"));
            }
        }

        CurrentDuressPassword = string.Empty;
        NewDuressPassword = string.Empty;
    }

    
}

