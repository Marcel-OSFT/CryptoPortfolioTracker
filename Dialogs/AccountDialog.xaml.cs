using System;
using CryptoPortfolioTracker.Enums;
//using CoinGecko.Clients;
//using CoinGecko.Interfaces;
//using CoinGecko.Parameters;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt.ClassInstances.Pred;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs
{
    public sealed partial class AccountDialog : ContentDialog
    {
        public readonly AccountsViewModel _viewModel;
        public Account newAccount;
        private readonly Account _accountToEdit;

        private ILocalizer loc = Localizer.Get();

        public AccountDialog(AccountsViewModel viewModel, DialogAction dialogAction = DialogAction.Add, Account accountToEdit = null)
        {
            this.InitializeComponent();
            
            _viewModel = viewModel;
            _accountToEdit = accountToEdit;
            SetDialogTitleAndButtons(dialogAction);
        }

        private void SetDialogTitleAndButtons(DialogAction dialogAction)
        {
             if (dialogAction == DialogAction.Edit)
            {
                AccountNameText.Text = _accountToEdit.Name;
                DescriptionText.Text = _accountToEdit.About;
                Title = loc.GetLocalizedString("AccountDialog_Title_Edit");
                PrimaryButtonText = loc.GetLocalizedString("AccountDialog_PrimaryButton_Edit");
                CloseButtonText = loc.GetLocalizedString("AccountDialog_CloseButton");
                IsPrimaryButtonEnabled = false ;
            }
            else
            {
                Title = loc.GetLocalizedString("AccountDialog_Title_Add");
                PrimaryButtonText = loc.GetLocalizedString("AccountDialog_PrimaryButton_Add");
                CloseButtonText = loc.GetLocalizedString("AccountDialog_CloseButton");
                IsPrimaryButtonEnabled = false;
            }
        }

        private void Button_Click_AcceptAccount(ContentDialog sender, ContentDialogButtonClickEventArgs e)
        {
            Account account = new Account
            {
                Name = AccountNameText.Text,
                About = DescriptionText.Text,
            };

            newAccount = account;
        }

        private void AccountName_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            IsPrimaryButtonEnabled = AccountNameText.Text.Length > 0 ? true : false;
        }

        private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
        {
            if (sender.ActualTheme != App.userPreferences.AppTheme) sender.RequestedTheme = App.userPreferences.AppTheme;
        }
    }
}

