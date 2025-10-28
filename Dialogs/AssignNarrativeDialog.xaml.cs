namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class AssignNarrativeDialog : ContentDialog
{
    private readonly CoinLibraryViewModel _viewModel;

    private readonly ILocalizer loc = Localizer.Get();

    [ObservableProperty] private List<Narrative> narratives;
    [ObservableProperty] private Narrative initialNarrative = new();
    public Narrative? selectedNarrative;
    private Coin _coin;


    public AssignNarrativeDialog(Coin coin, CoinLibraryViewModel viewModel)
    {
        _coin = coin;
        _viewModel = viewModel;
        Narratives = _viewModel.narratives ?? new List<Narrative>();
        InitialNarrative = Narratives.Where(x => x.Name == coin.Narrative.Name).FirstOrDefault();

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
        selectedNarrative = cbNarratives.SelectedItem as Narrative;
    }


    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _viewModel.AppSettings.AppTheme)
        {
            sender.RequestedTheme = _viewModel.AppSettings.AppTheme;
        }
    }

    
}

