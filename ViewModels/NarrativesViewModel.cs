
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using WinUI3Localizer;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Windows.UI.ViewManagement;
using System.Collections.ObjectModel;
using LanguageExt;

namespace CryptoPortfolioTracker.ViewModels;

public sealed partial class NarrativesViewModel : BaseViewModel, INotifyPropertyChanged, IRecipient<UpdatePricesMessage>
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static NarrativesViewModel Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public INarrativeService _narrativeService {  get; private set; }
    public Settings AppSettings => base._appSettings; // expose AppSettings publicly so that it can be used in dialogs called by this ViewModel

    public IAssetService _assetService { get; private set; }

    private readonly IAppState _appState;

    [ObservableProperty] public partial string ExplainerText { get; set; }
    [ObservableProperty] public partial string ViewTitle { get; set; }

    //[ObservableProperty] public string portfolioName = string.Empty;
    public string PortfolioName => _appState.CurrentPortfolio?.Name ?? string.Empty;
    //[ObservableProperty] public Portfolio currentPortfolio;

    //partial void OnCurrentPortfolioChanged(Portfolio? oldValue, Portfolio newValue)
    //{
    //    PortfolioName = newValue.Name;
    //}


    private SortingOrder currentSortingOrder;
    private Func<AssetTotalsDisplay, object> currentSortFunc;
    private SortingOrder currentSortingOrderNarr;
    private Func<NarrativeDisplay, object> currentSortFuncNarr;


    [ObservableProperty] private string sortGroup;
    
    public NarrativeDisplay? selectedNarrative = null;
    private bool _isAssetsListViewInitialized;

    [ObservableProperty] private string glyphPrivacy = "\uE890";

    [ObservableProperty] private bool isPrivacyMode;


    partial void OnIsPrivacyModeChanged(bool value)
    {
        GlyphPrivacy = value ? "\uED1A" : "\uE890";
        AppSettings.AreValuesMasked = value;
        MaskRelatedUiRefresh();
    }

    [ObservableProperty] public partial ObservableCollection<NarrativeDisplay> ListNarratives { get; set; } = new ObservableCollection<NarrativeDisplay>();
    [ObservableProperty] public partial ObservableCollection<AssetTotalsDisplay>? ListAssetTotals { get; set; } = new ObservableCollection<AssetTotalsDisplay>();
    [ObservableProperty] public partial int? SelectedNarrativeId { get; private set; }

    public NarrativesViewModel(INarrativeService NarrativeService, IAssetService assetService, IAppState appState ,Settings appSettings, IDialogService dialogService, IMessenger messenger) : base(appSettings, dialogService, messenger)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(NarrativesViewModel).Name.PadRight(22));
        Current = this;
        _narrativeService = NarrativeService;
        _assetService = assetService;
        _appState = appState;
        _appState.CurrentPortfolioChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PortfolioName));
            // reload data if needed
        };

        messenger.Register<UpdatePricesMessage>(this);
        //CurrentPortfolio = _assetService.GetPortfolio();
        SortGroup = "Narratives";
        currentSortFunc = x => x.MarketValue;
        currentSortingOrder = SortingOrder.Descending;
        currentSortFuncNarr = x => x.TotalValue;
        currentSortingOrderNarr = SortingOrder.Descending;

        _assetService.IsHidingZeroBalances = AppSettings.IsHidingZeroBalances;
        IsPrivacyMode =AppSettings.AreValuesMasked;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowNarrativeDialogToAddCommand))]
    private bool isExtendedView = false;

    [ObservableProperty] private bool isAssetsExtendedView = false;


    public void Receive(UpdatePricesMessage message)
    {
        _ = RefreshNarrativesList();
        _ = RefreshAssetTotalsList();
    }


    /// <summary>
    /// Initialize async task is called from the View_Loaded event of the associated View
    /// this to prevent to have it called from the ViewModels constructor
    /// </summary>
    /// <returns></returns>
    public async Task ViewLoading()
    {
        ExplainerText = loc.GetLocalizedString("Narratives_Explainer");
        ViewTitle = loc.GetLocalizedString("Narratives_Title");
        //CurrentPortfolio = _assetService.GetPortfolio();
        //PortfolioName = CurrentPortfolio.Name;

        IsPrivacyMode =AppSettings.AreValuesMasked;
        await PopulateNarrativesList();
    }

    public void Terminate()
    {
        _narrativeService.ClearNarrativesList();
        ListNarratives.Clear();
        selectedNarrative = null;
        IsExtendedView = false;
    }

    private void MaskRelatedUiRefresh()
    {
        var tempListNarratives = ListNarratives;
        ListNarratives = null;
        ListNarratives = tempListNarratives;

        var tempListAssetTotals = ListAssetTotals;
        ListAssetTotals = null;
        ListAssetTotals = tempListAssetTotals;
    }

    [RelayCommand]
    public void SortOnNameNarr(SortingOrder sortingOrder)
    {
        SortNarratives(sortingOrder, x => x.Name);
    }

    [RelayCommand]
    public void SortOnMarketValueNarr(SortingOrder sortingOrder)
    {
        SortNarratives(sortingOrder, x => x.TotalValue);
    }

    [RelayCommand]
    public void SortOnCostBaseNarr(SortingOrder sortingOrder)
    {
        SortNarratives(sortingOrder, x => x.CostBase);
    }

    [RelayCommand]
    public void SortOnPnLNarr(SortingOrder sortingOrder)
    {
        SortNarratives(sortingOrder, x => x.ProfitLoss);
    }

    [RelayCommand]
    public void SortOnPnLPercNarr(SortingOrder sortingOrder)
    {
        SortNarratives(sortingOrder, x => x.ProfitLossPerc);
    }


    [RelayCommand]
    public void SortOnName(SortingOrder sortingOrder)
    {
        SortAssetTotals(sortingOrder, x => x.Coin.Name);
    }

    [RelayCommand]
    public void SortOn24Hour(SortingOrder sortingOrder)
    {
        SortAssetTotals(sortingOrder, x => x.Coin.Change24Hr);
    }

    [RelayCommand]
    public void SortOn1Month(SortingOrder sortingOrder)
    {
        SortAssetTotals(sortingOrder, x => x.Coin.Change1Month);
    }

    [RelayCommand]
    public void SortOnMarketValue(SortingOrder sortingOrder)
    {
        SortAssetTotals(sortingOrder, x => x.MarketValue);
    }

    [RelayCommand]
    public void SortOnCostBase(SortingOrder sortingOrder)
    {
        SortAssetTotals(sortingOrder, x => x.CostBase);
    }

    [RelayCommand]
    public void SortOnPnL(SortingOrder sortingOrder)
    {
        SortAssetTotals(sortingOrder, x => x.ProfitLoss);
    }

    [RelayCommand]
    public void SortOnPnLPerc(SortingOrder sortingOrder)
    {
        SortAssetTotals(sortingOrder, x => x.ProfitLossPerc);
    }


    private void SortAssetTotals(SortingOrder sortingOrder, Func<AssetTotalsDisplay, object> sortFunc)
    {
        if (ListAssetTotals is null || !ListAssetTotals.Any()) return;

        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;

        if (currentSortingOrder == SortingOrder.Ascending)
        {
            ListAssetTotals = new ObservableCollection<AssetTotalsDisplay>(ListAssetTotals.OrderBy(currentSortFunc));
        }
        else
        {
            ListAssetTotals = new ObservableCollection<AssetTotalsDisplay>(ListAssetTotals.OrderByDescending(currentSortFunc));
        }

    }
    private void SortNarratives(SortingOrder sortingOrder, Func<NarrativeDisplay, object> sortFunc)
    {
        if (ListNarratives is null || !ListNarratives.Any()) return;

        currentSortFuncNarr = sortFunc;
        currentSortingOrderNarr = sortingOrder;

        if (currentSortingOrderNarr == SortingOrder.Ascending)
        {
            ListNarratives = new ObservableCollection<NarrativeDisplay>(ListNarratives.OrderBy(currentSortFuncNarr));
        }
        else
        {
            ListNarratives = new ObservableCollection<NarrativeDisplay>(ListNarratives.OrderByDescending(currentSortFuncNarr));
        }

    }

    [RelayCommand]
    public static void SortOnNetInvestment(SortingOrder sortingOrder)
    {
        //*** disabled in NarrativesView
    }


    [RelayCommand(CanExecute = nameof(CanShowNarrativeDialogToAdd))]
    public async Task ShowNarrativeDialogToAdd()
    {
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing NarrativeDialog for Adding");

            var newNarrative = await _dialogService.ShowAsync<NarrativeDialog, Narrative>(new NarrativeDialogParameters(DialogAction.Add, null));

            if (newNarrative != null)
            {
                Logger.Information("Adding Narrative ({0})", newNarrative.Name);
                await (await _narrativeService.CreateNarrative(newNarrative))
                    .Match(Succ: async succ => await AddToListNarratives(newNarrative),
                        Fail: async err =>
                        {
                            await _dialogService.ShowMessageAsync(
                                loc.GetLocalizedString("Messages_NarrativeAddFailed_Title"),
                                err.Message,
                                loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(err, "Adding Narrative failed");
                        });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Showing Narrative Dialog failed");
            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_NarrativeDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }
    private bool CanShowNarrativeDialogToAdd()
    {
        return !IsExtendedView;
    }

    [RelayCommand (CanExecute = nameof(CanEditNarrative))]
    public async Task ShowNarrativeDialogToEdit(NarrativeDisplay narrative)
    {
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Narrative Dialog for Editing");

            var domainNarrative = ModelMapper.MapToDomain(narrative);
            var newNarrative = await _dialogService.ShowAsync<NarrativeDialog, Narrative>(new NarrativeDialogParameters(DialogAction.Edit, domainNarrative));

            if (newNarrative != null)
            {
                Logger.Information("Editing Narrative ({0})", narrative.Name);

                await (await _narrativeService.EditNarrative(newNarrative, domainNarrative))
                    .Match(Succ: async succ => await UpdateListNarratives(domainNarrative),
                        Fail: async err =>
                        {
                            await _dialogService.ShowMessageAsync(
                                loc.GetLocalizedString("Messages_NarrativeUpdateFailed_Title"),
                                err.Message,
                                loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(err, "Updating Narrative failed");
                        });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Narrative Dialog");
            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_NarrativeDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }
    private static bool CanEditNarrative(NarrativeDisplay narrative)
    {
        return !(narrative.Name == "- Not Assigned -" || narrative.Name.ToLower() == "stablecoin");
    }


    [RelayCommand(CanExecute = nameof(CanDeleteNarrative))]
    public async Task DeleteNarrative(NarrativeDisplay Narrative)
    {
        var loc = Localizer.Get();
        // *** Delete option normally never available when an Narrative contains assets
        //*** but nevertheless lets check it...
        try
        {
            Logger.Information("Deletion request for Narrative ({0})", Narrative.Name);
            var IsDeleteAllowed = (await _narrativeService.NarrativeHasNoCoins(Narrative.Id))
            .Match(Succ: succ => succ, Fail: err => { return false; });

            if (IsDeleteAllowed)
            {
                Logger.Information("Deleting Narrative");
                await (await _narrativeService.RemoveNarrative(Narrative.Id))
                    .Match(Succ: async s => await RemoveFromListNarratives(Narrative.Id),
                        Fail: async err =>
                        {
                            await _dialogService.ShowMessageAsync(
                                loc.GetLocalizedString("Messages_NarrativeDeleteFailed_Title"),
                                err.Message,
                                loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(err, "Deleting Narrative failed");
                        });
            }
            else
            {
                Logger.Information("Deleting Narrative not allowed");

                await _dialogService.ShowMessageAsync(
                    loc.GetLocalizedString("Messages_NarrativeDeleteNotAllowd_Title"),
                    loc.GetLocalizedString("Messages_NarrativeDeleteNotAllowed_Msg"),
                    loc.GetLocalizedString("Common_CloseButton"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to delete Narrative");
            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_NarrativeDeleteFailed_Title"),
                            ex.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
        }
    }
    private bool CanDeleteNarrative(NarrativeDisplay narrative)
    {
        if (narrative == null || narrative.Name == "- Not Assigned -") return false;

        var item = ListNarratives.FirstOrDefault(x => x.Id == narrative.Id);
        return item != null ? item.IsHoldingCoins ? false : true : false;
    }

    [RelayCommand]
    public async Task NarrativeItemClicked(NarrativeDisplay clickedNarrative)
    {
        //Clicked a new Narrative.... -> Resize Narrative ListView to small and Show Assets for this Narrative
        if (selectedNarrative == null || selectedNarrative.Id != clickedNarrative.Id)
        {
            IsExtendedView = true;

            await ShowAssets(clickedNarrative);
            selectedNarrative = clickedNarrative;
        }
        //clicked the already selected Narrative.... ->
        else if (selectedNarrative != null && selectedNarrative.Id == clickedNarrative.Id)
        {
            // if Assets are not shown -> Decrease Narrative Listview to small and show assets for this Narrative
            if (!IsExtendedView)
            {
                IsExtendedView = true;
                await ShowAssets(clickedNarrative);
                selectedNarrative = clickedNarrative;
            }
            else //if Assets are shown -> close Assets List and resize Narratives Listview to full-size
            {
                IsExtendedView = false;
                //_assetService.ClearAssetTotalsList();
                ListAssetTotals.Clear();
                selectedNarrative = null;
            }
        }
        
    }

    [RelayCommand]
    public async Task HideZeroBalances(bool param)
    {
        _assetService.IsHidingZeroBalances = param;
        await ShowAssets(selectedNarrative);

    }

    [RelayCommand]
    public static void AssetItemClicked(AssetTotalsDisplay clickedAsset)
    {
        //In the NarrativesView we ignore this command. The AssetsListViewControl is used in the AssetsView as well.
    }

    public async Task ShowAssets(NarrativeDisplay clickedNarrative)
    {
        var list = (await _assetService.PopulateAssetTotalsByNarrativeList(ModelMapper.MapToDomain(clickedNarrative)))
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        var sorted = currentSortingOrder == SortingOrder.Ascending
            ? list.OrderBy(currentSortFunc).ToList()
            : list.OrderByDescending(currentSortFunc).ToList();

        ListAssetTotals = new ObservableCollection<AssetTotalsDisplay>(sorted);
    }

    [RelayCommand]
    private void TogglePrivacyMode()
    {
        IsPrivacyMode = !IsPrivacyMode;
    }


    private async Task PopulateNarrativesList()
    {
        var list = (await _narrativeService.PopulateNarrativesList())
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        var sorted = currentSortingOrderNarr == SortingOrder.Ascending
            ? list.OrderBy(currentSortFuncNarr).ToList()
            : list.OrderByDescending(currentSortFuncNarr).ToList();

        ListNarratives = new ObservableCollection<NarrativeDisplay>(sorted);

    }

    private async Task AddToListNarratives(Narrative narrative)
    {
        var result = await _narrativeService.AddToListNarratives(narrative);
        if (result)
        {
            ListNarratives.Add(ModelMapper.MapToDisplay(narrative));
        }
    }

    private async Task UpdateListNarratives(Narrative narrative)
    {
        var selectedNarrative = SelectedNarrativeId;
        var UpdatedNarratives = (await _narrativeService.UpdateListNarratives(narrative))
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        ReplaceOrAddUpdatedItems(ListNarratives, UpdatedNarratives, x => x.Id);
        
        if (selectedNarrative.HasValue)
        {
            var selectedItem = ListNarratives.FirstOrDefault(x => x.Id == selectedNarrative.Value);
            SelectedNarrativeId = selectedItem?.Id;
        }

    }

    private async Task RemoveFromListNarratives(int narrativeId)
    {
        var result = await _narrativeService.RemoveFromListNarratives(narrativeId);
        if (result)
        {
            var narrativeToRemove = ListNarratives.FirstOrDefault(x => x.Id == narrativeId);
            if (narrativeToRemove != null)
            {
                ListNarratives.Remove(narrativeToRemove);
            }
        }
    }

    private async Task RefreshAssetTotalsList()
    {
        if (ListAssetTotals == null || ListAssetTotals.Count == 0) { return; }

        var selectedNarrative = ListNarratives.FirstOrDefault(x => x.Id == SelectedNarrativeId.Value);

        var refreshedList = (await _assetService.PopulateAssetTotalsByNarrativeList(ModelMapper.MapToDomain(selectedNarrative)))
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        foreach (var item in refreshedList)
        {
            // Find the corresponding item in the existing collection and update its properties
            var existingItem = ListAssetTotals?.FirstOrDefault(x => x.Coin.Id == item.Coin.Id);
            if (existingItem != null)
            {
                existingItem.Coin.Price = item.Coin.Price;
                existingItem.Coin.Change1Month = item.Coin.Change1Month;
                existingItem.Coin.Change24Hr = item.Coin.Change24Hr;

                existingItem.MarketValue = item.MarketValue;
                existingItem.ProfitLoss = item.ProfitLoss;
                existingItem.ProfitLossPerc = item.ProfitLossPerc;
                existingItem.NetProfitLossPerc = item.NetProfitLossPerc;
            }
        }
        var copy = new List<AssetTotalsDisplay>(ListAssetTotals);
        var sorted = currentSortingOrder == SortingOrder.Ascending
            ? copy.OrderBy(currentSortFunc).ToList()
            : copy.OrderByDescending(currentSortFunc).ToList();

        CollectionReorderHelper.ApplyOrderedSequence(ListAssetTotals, sorted, x => x.Coin.Id);
    }

    private async Task RefreshNarrativesList()
    {
        if (ListNarratives == null || ListNarratives.Count == 0) { return; }
        var refreshedList = await _narrativeService.PopulateNarrativesList();
        foreach (var item in refreshedList)
        {
            // Find the corresponding item in the existing collection and update its properties
            var existingItem = ListNarratives?.FirstOrDefault(x => x.Id == item.Id);
            if (existingItem != null)
            {
                existingItem.TotalValue = item.TotalValue;
            }
        }
    }


}

