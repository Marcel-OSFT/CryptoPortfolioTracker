//using CoinGecko.Clients;
//using CoinGecko.Interfaces;
//using CoinGecko.Parameters;
using CryptoPortfolioTracker.Models;
using Microsoft.UI.Xaml.Controls;

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


