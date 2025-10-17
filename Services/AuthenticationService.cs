using CryptoPortfolioTracker.Helpers;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.UI;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Services;

public class AuthenticationService
{
    private readonly IPreferencesService _preferencesService;
    private readonly byte[] _keyBytes;
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;
    private static string AuthStateFile => Path.Combine(App.AppDataPath, "authstate.json");

    public AuthenticationService(IPreferencesService preferencesService, byte[] keyBytes)
    {
        _preferencesService = preferencesService;
        _keyBytes = keyBytes;
    }

    public async Task<bool> AuthenticateUserAsync(Window? splash)
    {
        var loc = Localizer.Get();
        var vault = new PasswordVault();
        string? passwordHash = null;
        string? duressPasswordHash = null;
        try { passwordHash = vault.Retrieve("CryptoPortfolioTracker", "Password")?.Password; } catch { }
        try { duressPasswordHash = vault.Retrieve("CryptoPortfolioTracker", "DuressPassword")?.Password; } catch { }
        if (string.IsNullOrEmpty(passwordHash) && string.IsNullOrEmpty(duressPasswordHash))
            return true;

        var state = LoadAuthState();
        var now = DateTime.UtcNow;
        if (state.LockoutUntil.HasValue && state.LockoutUntil.Value > now)
        {
            var remaining = state.LockoutUntil.Value - now;
            //await App.ShowErrorMessage($"Too many failed attempts. Please try again in {remaining.Minutes:D2}:{remaining.Seconds:D2} minutes.");
            await App.ShowMessageDialog(
                loc.GetLocalizedString("Messages_Authentication_Attempts_Title"),
                loc.GetLocalizedString("Messages_Authentication_Attempts_MsgPrefix") + $" {remaining.Minutes:D2}:{ remaining.Seconds:D2} " + loc.GetLocalizedString("Messages_Authentication_Attempts_MsgSuffix"),
                loc.GetLocalizedString("Common_OkButton"));

            return false;
        }
        if (state.LockoutUntil.HasValue && state.LockoutUntil.Value <= now)
        {
            state.FailedAttempts = 0;
            state.LockoutUntil = null;
            SaveAuthState(state);
        }

        var userId = _preferencesService.GetUserID();
        if (string.IsNullOrEmpty(userId))
        {
            userId = Guid.NewGuid().ToString();
            _preferencesService.SetUserID(userId);
        }

        var _theme = _preferencesService.GetAppTheme();
        var passwordBox = new PasswordBox { PlaceholderText = loc.GetLocalizedString("Messages_Authentication_PlaceholderText") };//"Enter your password" };
        var forgotPasswordLink = new HyperlinkButton
        {
            Content = loc.GetLocalizedString("Messages_Authentication_ForgotPassword"), //"Forgot password?",
            Foreground = _theme == ElementTheme.Light ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Blue) : new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 88, 166, 255)),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        };
        forgotPasswordLink.Click += (s, e) =>
        {
            string mailto = string.Empty;
            var test = _preferencesService.GetAppCultureLanguage();
            if (_preferencesService.GetAppCultureLanguage() == "nl")
            {
                mailto = $"mailto:mk_osft@hotmail.com" +
                    $"?subject=App%20Access%20Help" +
                    $"&body=Hallo,%0A" +
                    $"Ik heb wat hulp nodig met mijn app.%0A%0A" +
                    $"User ID: {userId}%0A" +
                    $"CPT Versie: {App.ProductVersion}%0A%0A" +
                    $"Geef mij een tijdelijke code zodat ik weer toegang krijg.%0A%0A" +
                    $"Met vriendelijke groet,%0AUw dankbare CPT gebruiker";
            }
            else
            {
                mailto = $"mailto:mk_osft@hotmail.com" +
                    $"?subject=App%20Access%20Help" +
                    $"&body=Hello,%0A" +
                    $"I need some help with the app.%0A%0A" +
                    $"User ID: {userId}%0A" +
                    $"CPT Version: {App.ProductVersion}%0A%0A" +
                    $"Please provide me with a temporary code to get access again.%0A%0A" +
                    $"Best regards,%0AYour grateful CPT user";
            }
            Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
        };

        var stackPanel = new StackPanel();
        stackPanel.Children.Add(passwordBox);
        stackPanel.Children.Add(forgotPasswordLink);

        var dialog = new ContentDialog
        {
            Title = loc.GetLocalizedString("Messages_Authentication_Required"), // "Authentication Required",
            Content = stackPanel,
            PrimaryButtonText = loc.GetLocalizedString("Common_OkButton"), //"OK",
            CloseButtonText = loc.GetLocalizedString("Common_CancelButton"), //"Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = splash?.Content.XamlRoot,
            RequestedTheme = _preferencesService.GetAppTheme()
        };

        while (true)
        {
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return false;

            string entered = passwordBox.Password ?? string.Empty;

            if (!string.IsNullOrEmpty(passwordHash) && SettingsViewModel.VerifyPassword(entered, passwordHash))
            {
                App.IsDuressMode = false;
                state.FailedAttempts = 0;
                state.LockoutUntil = null;
                SaveAuthState(state);
                return true;
            }
            if (!string.IsNullOrEmpty(duressPasswordHash) && SettingsViewModel.VerifyPassword(entered, duressPasswordHash))
            {
                App.IsDuressMode = true;
                state.FailedAttempts = 0;
                state.LockoutUntil = null;
                SaveAuthState(state);
                return true;
            }

            // Check for reset code (8 hex chars)
            if (entered.Length >= 6 && entered.Substring(0,6) == "RESET-")
            {
                if (await ValidateResetCodeAsync(userId, entered.Substring(6)))
                {
                    state.FailedAttempts = 0;
                    state.LockoutUntil = null;
                    SaveAuthState(state);
                    PasswordCredential credential = null;
                    try { credential = vault.Retrieve("CryptoPortfolioTracker", "Password"); } catch { }
                    if (credential != null)
                    {
                        vault.Remove(credential);
                    }
                    PasswordCredential duressCredential = null;
                    try {duressCredential = vault.Retrieve("CryptoPortfolioTracker", "DuressPassword"); } catch { }
                    if (duressCredential != null)
                    {
                        vault.Remove(duressCredential);
                    }
                    //await App.ShowErrorMessage("Password has been reset. Please set a new password in Settings.");
                    await App.ShowMessageDialog(
                        loc.GetLocalizedString("Messages_Authentication_PasswordReset_Title"),
                        loc.GetLocalizedString("Messages_Authentication_PasswordReset_Msg"),
                        loc.GetLocalizedString("Common_OkButton"));
                    return true;
                }
                else
                {
                    //await App.ShowErrorMessage("Invalid reset code.");
                    await App.ShowMessageDialog(
                        loc.GetLocalizedString("Messages_Authentication_InvalidCode_Title"),
                        loc.GetLocalizedString("Messages_Authentication_InvalidCode_Msg"),
                        loc.GetLocalizedString("Common_OkButton"));
                }
            }

            state.FailedAttempts++;
            if (state.FailedAttempts >= MaxFailedAttempts)
            {
                state.LockoutUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                SaveAuthState(state);
                //await App.ShowErrorMessage($"Too many failed attempts. Locked out for {LockoutMinutes} minutes.");
                await App.ShowMessageDialog(
                    loc.GetLocalizedString("Messages_Authentication_Attempts_Title"),
                    loc.GetLocalizedString("Messages_Authentication_Attempts_MsgPrefix1") + $" {LockoutMinutes} " + loc.GetLocalizedString("Messages_Authentication_Attempts_MsgSuffix"),
                    loc.GetLocalizedString("Common_OkButton"));

                return false;
            }
            else
            {
                SaveAuthState(state);
                dialog.Title = loc.GetLocalizedString("Messages_Authentication_Failed") + $" ({state.FailedAttempts}/{MaxFailedAttempts})"; // $"Authentication Failed ({state.FailedAttempts}/{MaxFailedAttempts})";
                passwordBox.Password = string.Empty;
            }
        }
    }

    private static AuthState LoadAuthState()
    {
        try
        {
            if (File.Exists(AuthStateFile))
            {
                var json = File.ReadAllText(AuthStateFile);
                return JsonConvert.DeserializeObject<AuthState>(json) ?? new AuthState();
            }
        }
        catch { }
        return new AuthState();
    }

    private static void SaveAuthState(AuthState state)
    {
        try
        {
            var json = JsonConvert.SerializeObject(state);
            File.WriteAllText(AuthStateFile, json);
        }
        catch { }
    }

    private static async Task<DateTime?> GetNetworkUtcTimeAsync()
    {
        try
        {
            const string ntpServer = "pool.ntp.org";
            var ntpData = new byte[48];
            ntpData[0] = 0x1B;
            using (var udpClient = new UdpClient())
            {
                udpClient.Connect(ntpServer, 123);
                await udpClient.SendAsync(ntpData, ntpData.Length);
                var result = await udpClient.ReceiveAsync();
                ntpData = result.Buffer;
            }
            const byte serverReplyTime = 40;
            ulong intPart = (ulong)ntpData[serverReplyTime] << 24 |
                            (ulong)ntpData[serverReplyTime + 1] << 16 |
                            (ulong)ntpData[serverReplyTime + 2] << 8 |
                            (ulong)ntpData[serverReplyTime + 3];
            ulong fractPart = (ulong)ntpData[serverReplyTime + 4] << 24 |
                              (ulong)ntpData[serverReplyTime + 5] << 16 |
                              (ulong)ntpData[serverReplyTime + 6] << 8 |
                              (ulong)ntpData[serverReplyTime + 7];
            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            var networkDateTime = new DateTime(1900, 1, 1).AddMilliseconds((long)milliseconds);
            return networkDateTime.ToUniversalTime();
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> GenerateResetCodeAsync(string userId)
    {
        var networkTime = await GetNetworkUtcTimeAsync();
        var utcNow = networkTime ?? DateTime.UtcNow;
        var timeWindow = utcNow.ToString("yyyyMMddHH");
        var data = $"{userId}:{timeWindow}:{System.Text.Encoding.UTF8.GetString(_keyBytes)}";
        return SHA256(data).Substring(0, 8).ToUpper();
    }

    public async Task<bool> ValidateResetCodeAsync(string userId, string code)
    {
        var networkTime = await GetNetworkUtcTimeAsync();
        var utcNow = networkTime ?? DateTime.UtcNow;
        foreach (var offset in new[] { 0, -1 })
        {
            var timeWindow = utcNow.AddHours(offset).ToString("yyyyMMddHH");
            var data = $"{userId}:{timeWindow}:{System.Text.Encoding.UTF8.GetString(_keyBytes)}";
            var expected = SHA256(data).Substring(0, 8).ToUpper();
            if (expected == code) return true;
        }
        return false;
    }

    private static string SHA256(string input)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }

    
}
