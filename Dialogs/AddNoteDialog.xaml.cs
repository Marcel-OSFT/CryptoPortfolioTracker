//using CoinGecko.Clients;
//using CoinGecko.Interfaces;
//using CoinGecko.Parameters;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace CryptoPortfolioTracker.Dialogs;

public sealed partial class AddNoteDialog : ContentDialog
{
    public string newNote;

    private readonly ResourceLoader rl = new();

    public AddNoteDialog(Coin coin)
    {
        this.InitializeComponent();
        NoteText.Text = coin.Note;
        SetDialogTitleAndButtons(coin);
    }

    private void SetDialogTitleAndButtons(Coin coin)
    {
            Title = rl.GetString("NoteDialog_Title") + " " + coin.Name; ;
            PrimaryButtonText = rl.GetString("NoteDialog_PrimaryButton");
            CloseButtonText = rl.GetString("NoteDialog_CloseButton");
    }

    private void Button_Click_AcceptNote(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        newNote = NoteText.Text;
    }

}


