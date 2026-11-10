using CryptoPortfolioTracker.Infrastructure.Response.Coins;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class AddPrereleaseCoinDialog : BaseDialog, IDialog<Coin?>
{
    public Coin? Result { get; private set; }
    //public readonly CoinLibraryViewModel _viewModel;
    private readonly INarrativeService _narrativeService;
    public Coin? newCoin;

    [ObservableProperty] private string decimalSeparator;

    public AddPrereleaseCoinDialog(Settings settings, INarrativeService narrativeService)
    {
        InitializeComponent();
       // _viewModel = viewModel;
        _narrativeService = narrativeService;
        DecimalSeparator = settings.NumberFormat.NumberDecimalSeparator;
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
            .WithPriceLevel(priceLevel => priceLevel
                .OfType(PriceLevelType.TakeProfit))
            .WithPriceLevel(priceLevel => priceLevel
                .OfType(PriceLevelType.Buy))
            .WithPriceLevel(priceLevel => priceLevel
                .OfType(PriceLevelType.Stop))
            .WithPriceLevel(priceLevel => priceLevel
                .OfType(PriceLevelType.Ema))
            .OfNarrative(_narrativeService.GetDefaultNarrative())
            .Build();

        Result = newCoin;
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

}
