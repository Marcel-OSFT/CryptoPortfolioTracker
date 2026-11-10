namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class AssignNarrativeDialog : BaseDialog, IDialog<Narrative>
{
    public Narrative? Result { get; private set; }
    private readonly CoinLibraryViewModel _viewModel;

    private Coin _coin;
    [ObservableProperty] private List<Narrative> narratives;
    [ObservableProperty] private Narrative selectedNarrative;
    [ObservableProperty] private string about;

    partial void OnSelectedNarrativeChanged(Narrative? oldValue, Narrative newValue)
    {
        About = newValue?.About ?? string.Empty;
    }

    public AssignNarrativeDialog(CoinLibraryViewModel viewModel, AssignNarrativeDialogParameters parameters)
    {
        _coin = parameters.coin;
        _viewModel = viewModel;
        Narratives = parameters.narratives ?? new List<Narrative>();
        SelectedNarrative = Narratives.Where(x => x.Name == _coin.Narrative.Name).FirstOrDefault();

        InitializeComponent();
        SetDialogTitleAndButtons();
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("AssignNarrativeDialog_Title_Edit") + " " + _coin.Name;
        PrimaryButtonText = loc.GetLocalizedString("AssignNarrativeDialog_PrimaryButton_Edit");
        CloseButtonText = loc.GetLocalizedString("NarrativeDialog_CloseButton");
       
    }

    private void Button_Click_AcceptNarrative(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        //selectedNarrative = cbNarratives.SelectedItem as Narrative;
        Result = SelectedNarrative;
    }

}

