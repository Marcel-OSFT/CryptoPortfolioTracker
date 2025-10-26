namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class AddPrereleaseCoinDialog : ContentDialog
{
    public readonly CoinLibraryViewModel _viewModel;

    public Coin? newCoin;
    private readonly ILocalizer loc = Localizer.Get();

    [ObservableProperty] private string decimalSeparator;
    


    public AddPrereleaseCoinDialog(CoinLibraryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DecimalSeparator = _viewModel.AppSettings.NumberFormat.NumberDecimalSeparator;
        SetDialogTitleAndButtons();
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("PreListingCoinDialog_Title");
        PrimaryButtonText = loc.GetLocalizedString("Common_AcceptButton");
        CloseButtonText = loc.GetLocalizedString("Common_CancelButton");
        IsPrimaryButtonEnabled = CoinName.Text.Length > 0;
    }

    private void Button_Click_AcceptCoin(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        newCoin = CoinBuilder.Create()
            .WithName(CoinName.Text + "_pre-listing")
            .WithSymbol(CoinSymbol.Text.ToUpper())
            .WithApiId(CoinName.Text + CoinSymbol.Text)
            .WithAbout(CoinAbout.Text)
            .CurrentPriceAt(Convert.ToDouble(CoinPrice.Text))
            .WithImage(AppConstants.AppPath + "\\Assets\\QuestionMarkBlue.png")
            .MarketCapRankAt(999999)
            .OfNarrative(_viewModel._narrativeService.GetDefaultNarrative())
            .Build();
    }

    private void CoinName_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        IsPrimaryButtonEnabled = CoinName.Text.Length > 0 && CoinSymbol.Text.Length > 0 && CoinPrice.IsEntryValid;
    }
    private void CoinSymbol_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        IsPrimaryButtonEnabled = CoinName.Text.Length > 0 && CoinSymbol.Text.Length > 0 && CoinPrice.IsEntryValid;
    }
    private void CoinPrice_Changed(object sender, System.EventArgs e)
    {
        IsPrimaryButtonEnabled = CoinName.Text.Length > 0 && CoinSymbol.Text.Length > 0 && CoinPrice.IsEntryValid;
    }

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _viewModel.AppSettings.AppTheme)
        {
            sender.RequestedTheme = _viewModel.AppSettings.AppTheme;
        }
    }

    
}
