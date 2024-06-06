using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

public sealed partial class AddNoteDialog : ContentDialog
{
    public string newNote = string.Empty;
    private readonly ILocalizer loc = Localizer.Get();
    private readonly IPreferencesService _preferencesService;

    public AddNoteDialog(Coin coin, IPreferencesService preferencesService)
    {
        InitializeComponent();
        _preferencesService = preferencesService;
        NoteText.Text = coin.Note;
        SetDialogTitleAndButtons(coin);
    }

    private void SetDialogTitleAndButtons(Coin coin)
    {
        Title = loc.GetLocalizedString("NoteDialog_Title") + " " + coin.Name;
        PrimaryButtonText = loc.GetLocalizedString("NoteDialog_PrimaryButton");
        CloseButtonText = loc.GetLocalizedString("NoteDialog_CloseButton");
    }

    private void Button_Click_AcceptNote(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        newNote = NoteText.Text;
    }

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _preferencesService.GetAppTheme())
        {
            sender.RequestedTheme = _preferencesService.GetAppTheme();
        }
    }
}


