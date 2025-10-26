namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public partial class PortfolioSettingsDialog : ContentDialog
{
    private readonly ILocalizer loc = Localizer.Get();
    private readonly AssetsViewModel _viewModel;
    [ObservableProperty] bool isCardExpanded;
    [ObservableProperty] bool isCardEnabled;

    [ObservableProperty]
    private bool isHidingZeroBalances;
    partial void OnIsHidingZeroBalancesChanged(bool value) => _viewModel.AppSettings.IsHidingZeroBalances = value;

    [ObservableProperty]
    private bool isHidingCapitalFlow;
    partial void OnIsHidingCapitalFlowChanged(bool value) => _viewModel.AppSettings.IsHidingCapitalFlow = value;

    public PortfolioSettingsDialog(AssetsViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = this;
        isCardExpanded = true;
        isCardEnabled = true;
        SetDialogTitleAndButtons();
        InitializeFields();
    }

    private async void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        var theme = _viewModel.AppSettings.AppTheme;
        if (sender.ActualTheme != theme)
        {
            sender.RequestedTheme = theme;
        }
    }

    private void InitializeFields()
    {
        var numberFormat = _viewModel.AppSettings.NumberFormat;
        var appCulture = _viewModel.AppSettings.AppCultureLanguage;

        IsHidingZeroBalances = _viewModel.AppSettings.IsHidingZeroBalances;
        IsHidingCapitalFlow = _viewModel.AppSettings.IsHidingCapitalFlow;
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("PortfolioSettingsDialog_Title");
        PrimaryButtonText = loc.GetLocalizedString("Common_CloseButton");
        IsPrimaryButtonEnabled = true;
    }




}

