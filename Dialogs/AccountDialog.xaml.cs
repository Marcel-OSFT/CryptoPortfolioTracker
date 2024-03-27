using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;

using Microsoft.UI.Xaml.Controls;

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

//using CoinGecko.Clients;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
//using CoinGecko.Interfaces;
//using CoinGecko.Parameters;
using System.Diagnostics;
using Newtonsoft.Json;


using System.Net.Http;
using Microsoft.UI.Dispatching;
using Windows.Networking.Connectivity;
using System.Collections.ObjectModel;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Documents;
using Windows.Graphics.Imaging;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Windows.Storage.Streams;
using Windows.Storage;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Infrastructure;
using Windows.UI.Popups;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI;
using CryptoPortfolioTracker.Enums;

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

