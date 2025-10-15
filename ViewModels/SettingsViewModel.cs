using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

public partial class SettingsViewModel : BaseViewModel, INotifyPropertyChanged
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static SettingsViewModel Current;
   
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private readonly IPreferencesService _preferencesService;
    //private UserPreferences userPref;

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

    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 100_000;

    [ObservableProperty]
    private ElementTheme appTheme;
    partial void OnAppThemeChanged(ElementTheme value) => _preferencesService.SetAppTheme(value);

    [ObservableProperty]
    private int numberFormatIndex;
    partial void OnNumberFormatIndexChanged(int value) => SetNumberSeparators(value);
    
    [ObservableProperty]
    private int appCultureIndex;
    partial void OnAppCultureIndexChanged(int value) => SetCulturePreference(value);

    [ObservableProperty]
    private double fontSize;
    partial void OnFontSizeChanged(double value) => _preferencesService.SetFontSize((AppFontSize)value);

    [ObservableProperty]
    private bool isCheckForUpdate;
    partial void OnIsCheckForUpdateChanged(bool value) => _preferencesService.SetCheckingForUpdate(value);

    [ObservableProperty]
    private bool isScrollBarsExpanded;
    partial void OnIsScrollBarsExpandedChanged(bool value) => _preferencesService.SetExpandingScrollBars(value);

    [ObservableProperty]
    private bool areValuesMasked;
    partial void OnAreValuesMaskedChanged(bool value) => _preferencesService.SetAreValuesMasked(value);

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

    public SettingsViewModel(IPreferencesService preferencesService) : base(preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(SettingsViewModel).Name.PadRight(22));
        Current = this;
        _preferencesService = preferencesService;
       
        InitializeFields();
    }

    private void InitializeFields()
    {
        var preferences = _preferencesService;
        var numberFormat = preferences.GetNumberFormat();
        var appCulture = preferences.GetAppCultureLanguage();

        NumberFormatIndex = numberFormat.NumberDecimalSeparator == "," ? 0 : 1;
        AppCultureIndex = appCulture[..2].ToLower() == "nl" ? 0 : 1;
        IsCheckForUpdate = preferences.GetCheckingForUpdate();
        FontSize = (double)preferences.GetFontSize();
        IsScrollBarsExpanded = preferences.GetExpandingScrollBars();
        AppTheme = preferences.GetAppTheme();

        CheckPasswordCredentials();
    }

    private void SetNumberSeparators(int index)
    {
        var nf = new NumberFormatInfo
        {
            NumberDecimalSeparator = index == 0 ? "," : ".",
            NumberGroupSeparator = index == 0 ? "." : ","
        };
        _preferencesService.SetNumberFormat(nf);
    }
    private void SetCulturePreference(int index)
    {
        string culture = index == 0 ? "nl" : "en-US";
        _preferencesService.SetAppCultureLanguage(culture);
    }

    [RelayCommand]
    private void SavePassword()
    {
        if (!string.IsNullOrWhiteSpace(Password))
        {
            var hash = HashPassword(Password);
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential("CryptoPortfolioTracker", "Password", hash));
            Password = string.Empty; // Clear after save
        }
    }

    [RelayCommand]
    private void SaveDuressPassword()
    {
        if (!string.IsNullOrWhiteSpace(DuressPassword))
        {
            var hash = HashPassword(DuressPassword);
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential("CryptoPortfolioTracker", "DuressPassword", hash));
            DuressPassword = string.Empty; // Clear after save
        }
    }
    /// <summary>
    /// Hashes a password using PBKDF2.
    /// </summary>
    private static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] salt = new byte[SaltSize];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] key = pbkdf2.GetBytes(KeySize);

        var hashBytes = new byte[SaltSize + KeySize];
        Buffer.BlockCopy(salt, 0, hashBytes, 0, SaltSize);
        Buffer.BlockCopy(key, 0, hashBytes, SaltSize, KeySize);

        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return string.IsNullOrEmpty(password);

        var hashBytes = Convert.FromBase64String(storedHash);
        var salt = new byte[SaltSize];
        Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] key = pbkdf2.GetBytes(KeySize);

        for (int i = 0; i < KeySize; i++)
        {
            if (hashBytes[i + SaltSize] != key[i])
                return false;
        }
        return true;
    }

    [RelayCommand]
    public async Task CheckUpdateNow()
    {
        Logger.Information("Checking for updates");
        var appUpdater = new AppUpdater();
        var loc = Localizer.Get();
        var result = await appUpdater.Check(App.VersionUrl, App.ProductVersion);

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

            var newHash = HashPassword(NewPassword);
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
            if (VerifyPassword(CurrentPassword, storedHash))
            {
                var newHash = HashPassword(NewPassword);
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

            var newHash = HashPassword(NewDuressPassword);
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
            if (VerifyPassword(CurrentDuressPassword, storedHash))
            {
                var newHash = HashPassword(NewDuressPassword);
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

