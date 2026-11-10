
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace CryptoPortfolioTracker.Dialogs;

public enum AuthenticationDialogAction
{
    None,
    Authenticated,
    Cancelled,
    ForgotPassword
}

public sealed partial class AuthenticationDialog : BaseDialog, IDialog<AuthenticationDialogResult>
{
    public AuthenticationDialogResult Result { get; private set; }

    private string _password = string.Empty;

    public AuthenticationDialog(string titleText = null)
    {
        InitializeComponent();


        TitleText = titleText ?? loc.GetLocalizedString("Messages_Authentication_Required");

        OkButtonText = loc.GetLocalizedString("Common_OkButton");

        CancelButtonText = loc.GetLocalizedString("Common_CancelButton");

        PasswordLabel = loc.GetLocalizedString("Messages_Authentication_Password");

        PasswordPlaceholder = loc.GetLocalizedString("Messages_Authentication_PlaceholderText");

        ForgotPasswordText = loc.GetLocalizedString("Messages_Authentication_ForgotPassword");
    }

    public string TitleText { get; }
    public string OkButtonText { get; }
    public string CancelButtonText { get; }
    public string PasswordLabel { get; }
    public string PasswordPlaceholder { get; }
    public string ForgotPasswordText { get; }

    public string Password => _password;

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _password = PasswordInput.Password;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = new AuthenticationDialogResult(_password, AuthenticationDialogAction.Authenticated);
    }

    private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = new AuthenticationDialogResult(string.Empty, AuthenticationDialogAction.Cancelled);
    }

    private void ForgotPasswordLink_Click(object sender, RoutedEventArgs e)
    {
        Result = new AuthenticationDialogResult(string.Empty, AuthenticationDialogAction.ForgotPassword);
        Hide();
    }
}

