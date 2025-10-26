namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public partial class DashboardSettingsDialog : ContentDialog
{
    private readonly ILocalizer loc = Localizer.Get();
    private readonly DashboardViewModel _viewModel;
    [ObservableProperty] bool isCardExpanded;
    [ObservableProperty] bool isCardEnabled;

    [ObservableProperty]
    private bool isScrollBarsExpanded;
    partial void OnIsScrollBarsExpandedChanged(bool value) => _viewModel.AppSettings.IsScrollBarsExpanded = value;

    [ObservableProperty]
    private int withinRangePerc;
    partial void OnWithinRangePercChanged(int value)
    {
        _viewModel.AppSettings.WithinRangePerc = value;
        DashboardViewModel.Current?.RefreshTargetBubbles();
    }

    [ObservableProperty]
    private int closeToPerc;
    partial void OnCloseToPercChanged(int value) => _viewModel.AppSettings.CloseToPerc = value;

    [ObservableProperty]
    private int maxPieCoins;
    partial void OnMaxPieCoinsChanged(int value)
    {
        _viewModel.AppSettings.MaxPieCoins = value;
        DashboardViewModel.Current?.RefreshPortfolioPie();

    }
        [ObservableProperty]
    private int rsiPeriod;
    async partial void OnRsiPeriodChanged(int value)
    {
        _viewModel.AppSettings.RsiPeriod = value;
        // update dashboard viewmodel display if available
        await DashboardViewModel.Current?.RefreshRsiBubbles();
    }

    [ObservableProperty]
    private int maPeriod;
   async partial void OnMaPeriodChanged(int value)
    {
        _viewModel.AppSettings.MaPeriod = value;
        await DashboardViewModel.Current?.RefreshMaBubbles();
    }

    [ObservableProperty]
    private int maTypeIndex;
    partial void OnMaTypeIndexChanged(int value)
    {
        SetMaTypeByIndex(value);
        DashboardViewModel.Current?.RefreshMaBubbles();
    }

    public DashboardSettingsDialog(DashboardViewModel viewModel)
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
            sender.RequestedTheme = theme ;
        }
    }

    private void InitializeFields()
    {
        var numberFormat = _viewModel.AppSettings.NumberFormat;
        var appCulture = _viewModel.AppSettings.AppCultureLanguage;

        WithinRangePerc = _viewModel.AppSettings.WithinRangePerc;
        CloseToPerc = _viewModel.AppSettings.CloseToPerc;
        MaxPieCoins = _viewModel.AppSettings.MaxPieCoins;
        RsiPeriod = _viewModel.AppSettings.RsiPeriod;
        MaPeriod = _viewModel.AppSettings.MaPeriod;
        MaTypeIndex = _viewModel.AppSettings.MaType switch
        {
            "SMA" => 0,
            "EMA" => 1,
            "WMA" => 2,
            "VWMA" => 3,
            _ => 0,
        };
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("DashboardSettingsDialog_Title");
        PrimaryButtonText = loc.GetLocalizedString("Common_CloseButton");
        IsPrimaryButtonEnabled = true;
    }

    private void SetMaTypeByIndex(int index)
    {
        string value = index switch
        {
            0 => "SMA",
            1 => "EMA",
            2 => "WMA",
            3 => "VWMA",
            _ => "SMA",
        };
        _viewModel.AppSettings.MaType = value;
    }


}
