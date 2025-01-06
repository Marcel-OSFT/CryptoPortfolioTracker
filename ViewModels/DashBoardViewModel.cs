using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Converters;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.ObjectModel;
using Windows.Security.Authentication.Web.Provider;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    public static DashboardViewModel? Current;
    private static ILocalizer loc = Localizer.Get();

    public readonly IDashboardService _dashboardService;
    public readonly IPreferencesService _preferencesService;
    private readonly IGraphService _graphService;
    private readonly IPriceLevelService _priceLevelService;


    


    [ObservableProperty] private string glyph = "\uEE47";
    [ObservableProperty] private string glyphPrivacy = "\uE890";

    [ObservableProperty] private FullScreenMode toggleFsMode;

    partial void OnToggleFsModeChanged(FullScreenMode value)
    {
        Glyph = value == FullScreenMode.None ? "\uEE47" : "\uEE49";
    }

    private static Func<double, string> labelerYAxis = value => string.Format("$ {0:N0}", value);

    [ObservableProperty] private bool isPrivacyMode;

    partial void OnIsPrivacyModeChanged(bool value)
    {
        GlyphPrivacy = value ? "\uED1A" : "\uE890";

        labelerYAxis = value ? value => "****" : value => string.Format("$ {0:N0}", value);

        _preferencesService.SetAreValuesMasked(value);

        ReloadAffectedControls();
    }

    [ObservableProperty] bool needUpdateDashboard = false;
    partial void OnNeedUpdateDashboardChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            SetSeriesHeatMap(selectedHeatMapIndex);
            GetTop5();
            GetValueGains();

            NeedUpdateDashboard = false;
        };
    }

    public DashboardViewModel(IDashboardService dashboardService,
                                IGraphService graphService,
                                IPriceLevelService priceLevelService,
                                IPreferencesService preferencesService) : base(preferencesService)
    {
        Current = this;

        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AssetsViewModel).Name.PadRight(22));
        _preferencesService = preferencesService;
        _dashboardService = dashboardService;
        _graphService = graphService;
        _priceLevelService = priceLevelService;
        IsPrivacyMode = _preferencesService.GetAreValesMasked();

        ConstructHeatMap();
        ConstructGraph();
        ConstructPie();
        ConstructTop5();
        ConstructValueGains();
    }

    public void Initialize()
    {
        IsPrivacyMode = _preferencesService.GetAreValesMasked();
    }

    [RelayCommand]
    private void TogglePrivacyMode()
    {
        IsPrivacyMode = !IsPrivacyMode;
    }

    [RelayCommand]
    private void ToggleFullScreenMode(object mode)
    {
        if (Enum.IsDefined(typeof(FullScreenMode), mode))
        {
            var requestedMode = (FullScreenMode)mode;
            ToggleFsMode = ToggleFsMode == requestedMode ? FullScreenMode.None : requestedMode;
        }
    }

    [RelayCommand]
    private void PieToggleFullScreenMode(PieChartControl pie)
    {
        var requestedMode = FullScreenMode.None;

        switch (pie.Name)
        {
            case "Portfolio":
                {
                    requestedMode = FullScreenMode.PiePortfolio;
                    break;
                }
            case "Accounts":
                {
                    requestedMode = FullScreenMode.PieAccounts;
                    break;
                }
            case "Narratives":
                {
                    requestedMode = FullScreenMode.PieNarratives;
                    break;
                }
            default: break;
        }
        ToggleFsMode = ToggleFsMode == requestedMode ? FullScreenMode.None : requestedMode;
    }

    private void ReloadAffectedControls()
    {
        ReloadValueGains();
        ReloadGraph();
    }
}






