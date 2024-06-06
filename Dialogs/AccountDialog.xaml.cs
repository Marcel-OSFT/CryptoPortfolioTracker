using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

public sealed partial class AccountDialog : ContentDialog
{
    public readonly AccountsViewModel _viewModel;
    public Account? newAccount;
    private readonly Account? _accountToEdit;
    private readonly IPreferencesService _preferencesService;

    private readonly ILocalizer loc = Localizer.Get();

    public AccountDialog(IPreferencesService preferencesService, AccountsViewModel viewModel, DialogAction dialogAction = DialogAction.Add, Account? accountToEdit = null)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _accountToEdit = accountToEdit;
        _preferencesService = preferencesService;
        SetDialogTitleAndButtons(dialogAction);
    }

    private void SetDialogTitleAndButtons(DialogAction dialogAction)
    {
        if (dialogAction == DialogAction.Edit)
        {
            AccountNameText.Text = _accountToEdit is not null ? _accountToEdit.Name : string.Empty;
            DescriptionText.Text = _accountToEdit is not null ? _accountToEdit.About : string.Empty;
            Title = loc.GetLocalizedString("AccountDialog_Title_Edit");
            PrimaryButtonText = loc.GetLocalizedString("AccountDialog_PrimaryButton_Edit");
            CloseButtonText = loc.GetLocalizedString("AccountDialog_CloseButton");
            IsPrimaryButtonEnabled = AccountNameText.Text.Length > 0;
        }
        else
        {
            Title = loc.GetLocalizedString("AccountDialog_Title_Add");
            PrimaryButtonText = loc.GetLocalizedString("AccountDialog_PrimaryButton_Add");
            CloseButtonText = loc.GetLocalizedString("AccountDialog_CloseButton");
            IsPrimaryButtonEnabled = AccountNameText.Text.Length > 0;
        }
    }

    private void Button_Click_AcceptAccount(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        var account = new Account
        {
            Name = AccountNameText.Text,
            About = DescriptionText.Text,
        };

        newAccount = account;
    }

    private void AccountName_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        IsPrimaryButtonEnabled = AccountNameText.Text.Length > 0;
    }
    

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _preferencesService.GetAppTheme())
        {
            sender.RequestedTheme = _preferencesService.GetAppTheme();
        }
    }

    
}

