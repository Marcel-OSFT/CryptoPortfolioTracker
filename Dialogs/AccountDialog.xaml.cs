using CryptoPortfolioTracker.Enums;
//using CoinGecko.Clients;
//using CoinGecko.Interfaces;
//using CoinGecko.Parameters;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Dialogs
{
    public sealed partial class AccountDialog : ContentDialog
    {
        public readonly AccountsViewModel _viewModel;
        public Account newAccount;
        private Account _accountToEdit;

        public AccountDialog(AccountsViewModel viewModel, DialogAction dialogAction = DialogAction.Add, Account accountToEdit = null)
        {
            this.InitializeComponent();
            _viewModel = viewModel;
            _accountToEdit = accountToEdit;
            if (dialogAction != DialogAction.Add)
            {
                AccountNameText.Text = _accountToEdit.Name;
                DescriptionText.Text = _accountToEdit.About;
                Title = "Edit Account";
                PrimaryButtonText = "Accept";
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
    }
}

