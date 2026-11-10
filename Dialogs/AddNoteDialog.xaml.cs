namespace CryptoPortfolioTracker.Dialogs;

public sealed partial class AddNoteDialog : BaseDialog, IDialog<string>
{
    public string? Result { get; private set; }
    public string newNote = string.Empty;
    private readonly CoinLibraryViewModel _viewModel;

    public AddNoteDialog(CoinLibraryViewModel viewModel, AddNoteDialogParameters parameters)
    {
        _viewModel = viewModel;
        InitializeComponent();
        NoteText.Text = parameters.coin.Note;
        SetDialogTitleAndButtons(parameters.coin);
        Result = "canceled";
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
        Result = newNote;
    }

    
}


