namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class AddPriceLevelsDialog : BaseDialog, IDialog<ICollection<PriceLevel>>
{
    public ICollection<PriceLevel> Result { get; private set; }
    private readonly PriceLevelsViewModel _viewModel;
    private readonly Settings _appSettings;
    [ObservableProperty] private double tpValue;
    [ObservableProperty] private string tpNote = string.Empty;

    [ObservableProperty] private double buyValue;
    [ObservableProperty] private string buyNote =string.Empty;

    [ObservableProperty] private double stopValue;
    [ObservableProperty] private string stopNote = string.Empty;


    [ObservableProperty] private bool isValidTest = false;
   // [ObservableProperty] private Validator validator;
    [ObservableProperty] private string decimalSeparator;

    public ICollection<PriceLevel> newPriceLevels { get; set; }
     

    public AddPriceLevelsDialog(PriceLevelsViewModel viewModel, Settings appSettings, AddPriceLevelsDialogParameters parameters)
    {
        _viewModel = viewModel;
        InitializeComponent();
        // DataContext = this;
        _appSettings = appSettings;

        DecimalSeparator = _appSettings.NumberFormat.NumberDecimalSeparator;
        newPriceLevels = new List<PriceLevel>(); // Initialize newPriceLevels
        InitializeFields(parameters.coin.PriceLevels);

        SetDialogTitleAndButtons(parameters.coin);
    }


    private void InitializeFields(ICollection<PriceLevel> priceLevels)
    {
        //newPriceLevels = priceLevels;

        foreach (var level in priceLevels)
        {
            if (level.Type == PriceLevelType.TakeProfit)
            {
                TpValue = level.Value;
                TpNote = level.Note;
            }
            else if (level.Type == PriceLevelType.Buy)
            {
                BuyValue = level.Value;
                BuyNote = level.Note;
            }
            else if (level.Type == PriceLevelType.Stop)
            {
                StopValue = level.Value;
                StopNote = level.Note;
            }
        }
    }

    private void SetDialogTitleAndButtons(Coin coin)
    {
        Title = loc.GetLocalizedString("PriceLevelsDialog_Title") + " " + coin.Name;
        PrimaryButtonText = loc.GetLocalizedString("PriceLevelsDialog_PrimaryButton");
        CloseButtonText = loc.GetLocalizedString("PriceLevelsDialog_CloseButton");
    }

    private void Button_Click_Accept(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        newPriceLevels = new List<PriceLevel>
        {
            new PriceLevel
            {
                Type = PriceLevelType.TakeProfit,
                Value = TpValue,
                Note = TpNote
            },
            new PriceLevel
            {
                Type = PriceLevelType.Buy,
                Value = BuyValue,
                Note = BuyNote
            },
            new PriceLevel
            {
                Type = PriceLevelType.Stop,
                Value = StopValue,
                Note = StopNote
            }
        };

        Result = newPriceLevels;
    }

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _appSettings.AppTheme)
        {
            sender.RequestedTheme = _appSettings.AppTheme;
        }
    }
}


