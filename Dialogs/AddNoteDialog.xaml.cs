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

namespace CryptoPortfolioTracker.Dialogs
{
    public sealed partial class AddNoteDialog : ContentDialog
    {
        public string newNote;
        public AddNoteDialog(Coin coin)
        {
            this.InitializeComponent();
            NoteText.Text = coin.Note;
        }

        private void Button_Click_AcceptNote(ContentDialog sender, ContentDialogButtonClickEventArgs e)
        {
            newNote = NoteText.Text;
        }

    }
}


