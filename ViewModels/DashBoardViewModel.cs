using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Converters;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Provider;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

[ObservableRecipient]
public partial class DashboardViewModel : BaseViewModel
{
    public static DashboardViewModel? Current;
    private static ILocalizer loc = Localizer.Get();

    public readonly IDashboardService _dashboardService;
    public readonly IPreferencesService _preferencesService;
    private readonly IGraphService _graphService;
    private readonly IPriceLevelService _priceLevelService;
    [ObservableProperty] public string portfolioName = string.Empty;
    [ObservableProperty] public Portfolio currentPortfolio;

    async partial void OnCurrentPortfolioChanged(Portfolio? oldValue, Portfolio newValue)
    {
        await _dashboardService.CalculateIndicatorsAllCoins();
        await UpdateDashboardAsync();
        PortfolioName = newValue.Name;
    }

    [ObservableProperty] private string glyph = "\uEE47";
    [ObservableProperty] private string glyphPrivacy = "\uE890";

    [ObservableProperty] private FullScreenMode toggleFsMode = FullScreenMode.None;

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
    async partial void OnNeedUpdateDashboardChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            await UpdateDashboardAsync();

            NeedUpdateDashboard = false;
        };
    }

    private async Task UpdateDashboardAsync()
    {
        //await SetSeriesHeatMap(SelectedHeatMapIndex);
        await RefreshHeatMapPoints();
        await GetTop5();
        GetValueGains();
    }

    public DashboardViewModel(IDashboardService dashboardService,
                                IGraphService graphService,
                                IPriceLevelService priceLevelService,
                                IMessenger messenger,
                                IPreferencesService preferencesService) : base(preferencesService)
    {
        Current = this;
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AssetsViewModel).Name.PadRight(22));

        messenger.Register<UpdateDashboardMessage>(this, (r, m) =>
        {
            NeedUpdateDashboard = true;
        });
        messenger.Register<UpdateProgressValueMessage>(this, (r, m) =>
        {
            ProgressValueGraph = m.ProgressValue;
        });
        messenger.Register<GraphUpdatedMessage>(this, (r, m) =>
        {
            SetValuesGraph();
        });

        _preferencesService = preferencesService;
        _dashboardService = dashboardService;
        _graphService = graphService;
        _priceLevelService = priceLevelService;
        IsPrivacyMode = _preferencesService.GetAreValesMasked();
        CurrentPortfolio = _dashboardService.GetPortfolio();

    }

    /// <summary>  
    /// This method is called by the DashboardView_Loaded event.  
    /// </summary>  
    public void ViewLoading()
    {
        CurrentPortfolio = _dashboardService.GetPortfolio();
        PortfolioName = CurrentPortfolio.Name;

        IsPrivacyMode = _preferencesService.GetAreValesMasked();
    }

    public void Terminate()
    {
       
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
            case "PortfolioPie":
                {
                    requestedMode = FullScreenMode.PiePortfolio;
                    break;
                }
            case "AccountsPie":
                {
                    requestedMode = FullScreenMode.PieAccounts;
                    break;
                }
            case "NarrativesPie":
                {
                    requestedMode = FullScreenMode.PieNarratives;
                    break;
                }
            default: break;
        }
        ToggleFsMode = ToggleFsMode == requestedMode ? FullScreenMode.None : requestedMode;
    }

    [RelayCommand]
    public async Task ShowSettingsDialog()
    {
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing DashboardSettings Dialog");
            var dialog = new DashboardSettingsDialog(_preferencesService)
            {
                XamlRoot = DashboardView.Current.XamlRoot
            };
            var result = await App.ShowContentDialogAsync(dialog);

            if (result == ContentDialogResult.Primary)
            {
                // maybe do something
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show DashboardSettings Dialog");
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_DashboardSettingsDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }


    private void ReloadAffectedControls()
    {
        ReloadValueGains();
        ReloadGraph();
    }

    public async Task RefreshRsiBubbles()
    {
        RsiRbContent = $"1D RSI({_preferencesService.GetRsiPeriod()})";
        await _dashboardService.CalculateRsiAllCoins();
        if (SelectedHeatMapIndex == 1) { await RefreshHeatMapPoints(); }
    }

    public async Task RefreshMaBubbles()
    {
        MaRbContent = $"1D {_preferencesService.GetMaType()}({_preferencesService.GetMaPeriod()})";
        await _dashboardService.CalculateMaAllCoins();
        if (SelectedHeatMapIndex == 2) { await RefreshHeatMapPoints(); }
    }

    public void RefreshPortfolioPie()
    {
        var portfolioPie = PieChartControls?.FirstOrDefault(p => string.Equals(p?.Name, "PortfolioPie", StringComparison.OrdinalIgnoreCase));
        if (portfolioPie is not null)
        {
            SetSeriesPie(portfolioPie);
        }
    }

    public async Task RefreshTargetBubbles()
    {
        RsiRbContent = $"1D RSI({_preferencesService.GetRsiPeriod()})";
        if (SelectedHeatMapIndex == 0) 
        {
            SetCustomSeparatorsTarget();
            await _dashboardService.EvaluatePriceLevels();
            await SetSeriesHeatMap(SelectedHeatMapIndex);
        }
    }
}






