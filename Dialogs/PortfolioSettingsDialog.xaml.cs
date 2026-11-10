namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public partial class PortfolioSettingsDialog : BaseDialog, IDialog<bool>
{
    public bool Result { get; private set; } = true;
    private readonly AssetsViewModel _viewModel;
    [ObservableProperty] bool isCardExpanded;
    [ObservableProperty] bool isCardEnabled;
    [ObservableProperty] private bool isScrollBarsExpanded;
    partial void OnIsScrollBarsExpandedChanged(bool value) => _viewModel.AppSettings.IsScrollBarsExpanded = value;
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

