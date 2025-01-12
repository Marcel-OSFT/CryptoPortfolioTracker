using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Converters;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Provider;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

public class UpdateDashboardMessage
{
    // You can add properties here if you need to pass additional data with the message
}

public class UpdateProgressValueMessage
{
    public int ProgressValue { get;}

    public UpdateProgressValueMessage(int value)
    {
        ProgressValue = value;
    }
}

public class IsUpdatingGraphMessage
{
    public bool IsUpdating { get; }

    public IsUpdatingGraphMessage(bool value)
    {
        IsUpdating = value;
    }
}

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
        await _dashboardService.CalculateRsiAllCoins();
        await UpdateDashboardAsync();
        PortfolioName = newValue.Name;
    }

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
        messenger.Register<IsUpdatingGraphMessage>(this, (r, m) =>
        {
            IsUpdatingGraph = m.IsUpdating;
        });

        _preferencesService = preferencesService;
        _dashboardService = dashboardService;
        _graphService = graphService;
        _priceLevelService = priceLevelService;
        IsPrivacyMode = _preferencesService.GetAreValesMasked();
        CurrentPortfolio = _dashboardService.GetPortfolio();

    }

    /// <summary>  
    /// This method is called by the DashboardView_Loading event.  
    /// </summary>  
    public void ViewLoading()
    {
        CurrentPortfolio = _dashboardService.GetPortfolio();
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






