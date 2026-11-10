using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json.Linq;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public partial class DashboardSettingsDialog : BaseDialog, IDialog<bool>
{
    public record DashboardSettingsSnapshot
    {
        public int RsiPeriod { get; init; }
        public int MaPeriod { get; init; }
        public string MaType { get; init; }
        public int WithinRangePerc { get; init; }
        public int CloseToPerc { get; init; }

        public int MaxPieCoins { get; init; }
        public bool IsScrollBarsExpanded { get; init; }

    }

    public bool Result { get; private set; }
    private readonly DashboardViewModel _viewModel;
    private readonly IMessenger _messenger;
    private readonly Settings _settings;
    private DashboardSettingsSnapshot originalSettings; // to compare changes on unload
    [ObservableProperty] bool isCardExpanded;
    [ObservableProperty] bool isCardEnabled;

    [ObservableProperty] private bool isScrollBarsExpanded;
    partial void OnIsScrollBarsExpandedChanged(bool value) => _viewModel.AppSettings.IsScrollBarsExpanded = value;
    [ObservableProperty] private int closeToPerc;
    partial void OnCloseToPercChanged(int value) => _viewModel.AppSettings.CloseToPerc = value;

    //*** when below properties change then a DashboardSettingsChangedMessage will be sent on dialog unload
    [ObservableProperty] private int withinRangePerc;

    [ObservableProperty] private int maxPieCoins;
    
    [ObservableProperty] private int rsiPeriod;

    [ObservableProperty] private int maPeriod;

    [ObservableProperty] private int maTypeIndex;

    public DashboardSettingsDialog(DashboardViewModel viewModel, IMessenger messenger, Settings settings)
    {
        _viewModel = viewModel;
        _messenger = messenger;
        _settings = settings;
        originalSettings = new DashboardSettingsSnapshot
        {
            WithinRangePerc = settings.WithinRangePerc,
            MaxPieCoins = settings.MaxPieCoins,
            RsiPeriod = settings.RsiPeriod,
            MaPeriod = settings.MaPeriod,
            MaType = settings.MaType,
        };
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

    private string GetMaTypeByIndex(int index)
    {
        return index switch
        {
            0 => "SMA",
            1 => "EMA",
            2 => "WMA",
            3 => "VWMA",
            _ => "SMA",
        };
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("DashboardSettingsDialog_Title");
        PrimaryButtonText = loc.GetLocalizedString("Common_CloseButton");
        IsPrimaryButtonEnabled = true;
        Result = true;
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

    private void Dialog_Unloaded(object sender, RoutedEventArgs e)
    {
        var changed = new List<string>();

        if (RsiPeriod != originalSettings.RsiPeriod) // placeholder: compare with original copy
        {
            _settings.RsiPeriod = RsiPeriod;
            changed.Add(nameof(Settings.RsiPeriod));
        }

        if (MaPeriod != originalSettings.MaPeriod)
        {
            _settings.MaPeriod = MaPeriod;
            changed.Add(nameof(Settings.MaPeriod));
        }

        var maType = GetMaTypeByIndex(MaTypeIndex);
        if (maType != originalSettings.MaType)
        {
            _settings.MaType = maType;
            changed.Add(nameof(Settings.MaType));
        }

        if (WithinRangePerc != originalSettings.WithinRangePerc)
        {
            _settings.WithinRangePerc = WithinRangePerc;
            changed.Add(nameof(Settings.WithinRangePerc));
        }
       
        if (MaxPieCoins != originalSettings.MaxPieCoins)
        {
            _settings.MaxPieCoins = MaxPieCoins;
            changed.Add(nameof(Settings.MaxPieCoins));
        }

        _messenger.Send(new DashboardSettingsChangedMessage(changed));
    }
}
