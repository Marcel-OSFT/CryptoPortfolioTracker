
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

namespace CryptoPortfolioTracker.ViewModels;

public sealed partial class NarrativesViewModel : BaseViewModel, INotifyPropertyChanged
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static NarrativesViewModel Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public INarrativeService _narrativeService {  get; private set; }
    public IAssetService _assetService { get; private set; }
    [ObservableProperty] public string portfolioName = string.Empty;
    [ObservableProperty] public Portfolio currentPortfolio;

    partial void OnCurrentPortfolioChanged(Portfolio? oldValue, Portfolio newValue)
    {
        PortfolioName = newValue.Name;
    }


    private readonly IPreferencesService _preferencesService;
    private SortingOrder currentSortingOrder;
    private Func<AssetTotals, object> currentSortFunc;
    private SortingOrder currentSortingOrderNarr;
    private Func<Narrative, object> currentSortFuncNarr;


    [ObservableProperty] private string sortGroup;
    
    public Narrative? selectedNarrative = null;
    private bool _isAssetsListViewInitialized;

    [ObservableProperty] private string glyphPrivacy = "\uE890";

    [ObservableProperty] private bool isPrivacyMode;


    partial void OnIsPrivacyModeChanged(bool value)
    {
        GlyphPrivacy = value ? "\uED1A" : "\uE890";
        _preferencesService.SetAreValuesMasked(value);
        _narrativeService.ReloadValues();
        _assetService.ReloadValues();
    }


    public NarrativesViewModel(INarrativeService NarrativeService, IAssetService assetService, IPreferencesService preferencesService) : base(preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(NarrativesViewModel).Name.PadRight(22));
        Current = this;
        _narrativeService = NarrativeService;
        _preferencesService =  preferencesService;
        _assetService = assetService;
        CurrentPortfolio = _assetService.GetPortfolio();

        SortGroup = "Narratives";
        currentSortFunc = x => x.MarketValue;
        currentSortingOrder = SortingOrder.Descending;
        currentSortFuncNarr = x => x.TotalValue;
        currentSortingOrderNarr = SortingOrder.Descending;

        _assetService.IsHidingZeroBalances = _preferencesService.GetHidingZeroBalances();
        IsPrivacyMode = _preferencesService.GetAreValesMasked();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowNarrativeDialogToAddCommand))]
    private bool isExtendedView = false;

    [ObservableProperty] private bool isAssetsExtendedView = false;

    /// <summary>
    /// Initialize async task is called from the View_Loaded event of the associated View
    /// this to prevent to have it called from the ViewModels constructor
    /// </summary>
    /// <returns></returns>
    public async Task ViewLoading()
    {
        CurrentPortfolio = _assetService.GetPortfolio();
        PortfolioName = CurrentPortfolio.Name;

        IsPrivacyMode = _preferencesService.GetAreValesMasked();
        await _narrativeService.PopulateNarrativesList(currentSortingOrderNarr, currentSortFuncNarr, _narrativeService.ShowOnlyAssets);
    }

    public void Terminate()
    {
        selectedNarrative = null;
        IsExtendedView = false;
    }


    [RelayCommand]
    public void SortOnNameNarr(SortingOrder sortingOrder)
    {
        SortNarratives(x => x.Name, sortingOrder);
    }

    [RelayCommand]
    public void SortOnMarketValueNarr(SortingOrder sortingOrder)
    {
        SortNarratives(x => x.TotalValue, sortingOrder);
    }

    [RelayCommand]
    public void SortOnCostBaseNarr(SortingOrder sortingOrder)
    {
        SortNarratives(x => x.CostBase, sortingOrder);
    }

    [RelayCommand]
    public void SortOnPnLNarr(SortingOrder sortingOrder)
    {
        SortNarratives(x => x.ProfitLoss, sortingOrder);
    }

    [RelayCommand]
    public void SortOnPnLPercNarr(SortingOrder sortingOrder)
    {
        SortNarratives(x => x.ProfitLossPerc, sortingOrder);
    }

    private void SortNarratives(Func<Narrative, object> sortFunc, SortingOrder sortingOrder)
    {
        currentSortFuncNarr = sortFunc;
        currentSortingOrderNarr = sortingOrder;
        _narrativeService.SortList(sortingOrder, sortFunc);
    }

    [RelayCommand]
    public void SortOnName(SortingOrder sortingOrder)
    {
        SortAssets(x => x.Coin.Name, sortingOrder);
    }

    [RelayCommand]
    public void SortOn24Hour(SortingOrder sortingOrder)
    {
        SortAssets(x => x.Coin.Change24Hr, sortingOrder);
    }

    [RelayCommand]
    public void SortOn1Month(SortingOrder sortingOrder)
    {
        SortAssets(x => x.Coin.Change1Month, sortingOrder);
    }

    [RelayCommand]
    public void SortOnMarketValue(SortingOrder sortingOrder)
    {
        SortAssets(x => x.MarketValue, sortingOrder);
    }

    [RelayCommand]
    public void SortOnCostBase(SortingOrder sortingOrder)
    {
        SortAssets(x => x.CostBase, sortingOrder);
    }

    [RelayCommand]
    public void SortOnPnL(SortingOrder sortingOrder)
    {
        SortAssets(x => x.ProfitLoss, sortingOrder);
    }

    [RelayCommand]
    public void SortOnPnLPerc(SortingOrder sortingOrder)
    {
        SortAssets(x => x.ProfitLossPerc, sortingOrder);
    }

    private void SortAssets(Func<AssetTotals, object> sortFunc, SortingOrder sortingOrder)
    {
        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;
        _assetService.SortList(sortingOrder, sortFunc);
    }

    [RelayCommand]
    public static void SortOnNetInvestment(SortingOrder sortingOrder)
    {
        //*** disabled in NarrativesView
    }

    [RelayCommand]
    public void ShowOnlyAssets(bool param)
    {
        _narrativeService.ShowOnlyAssets = param;
    }

   

    [RelayCommand(CanExecute = nameof(CanShowNarrativeDialogToAdd))]
    public async Task ShowNarrativeDialogToAdd()
    {
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing NarrativeDialog for Adding");
            var dialog = new NarrativeDialog(_preferencesService , Current, DialogAction.Add)
            {
                XamlRoot = NarrativesView.Current.XamlRoot
            };
            var result = await App.ShowContentDialogAsync(dialog);

            if (result == ContentDialogResult.Primary)
            {
                var NarrativeName = dialog.newNarrative is not null ? dialog.newNarrative.Name : string.Empty;
                Logger.Information("Adding Narrative ({0})", NarrativeName);
                await (await _narrativeService.CreateNarrative(dialog.newNarrative))
                    .Match(Succ: succ => _narrativeService.AddToListNarratives(dialog.newNarrative),
                        Fail: async err =>
                        {
                            await ShowMessageDialog(
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
            await ShowMessageDialog(
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
    public async Task ShowNarrativeDialogToEdit(Narrative narrative)
    {
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Narrative Dialog for Editing");
            var dialog = new NarrativeDialog(_preferencesService , Current, DialogAction.Edit, narrative)
            {
                XamlRoot = NarrativesView.Current.XamlRoot
            };
            var result = await App.ShowContentDialogAsync(dialog);

            if (result == ContentDialogResult.Primary && dialog.newNarrative is not null)
            {
                Logger.Information("Editing Narrative ({0})", narrative.Name);
                //(await _narrativeService.EditNarrative(dialog.newNarrative, narrative))
                //    .IfFail(async err =>
                //    {
                //        await ShowMessageDialog(
                //        loc.GetLocalizedString("Messages_NarrativeUpdateFailed_Title"),
                //        err.Message,
                //        loc.GetLocalizedString("Common_CloseButton"));
                //        Logger.Error(err, "Updating Narrative failed");
                //    });

                await (await _narrativeService.EditNarrative(dialog.newNarrative, narrative ))
                    .Match(Succ: succ => _narrativeService.UpdateListNarratives(narrative),
                        Fail: async err =>
                        {
                            await ShowMessageDialog(
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
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_NarrativeDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }
    private static bool CanEditNarrative(Narrative narrative)
    {
        return !(narrative.Name == "- Not Assigned -");
    }


    [RelayCommand(CanExecute = nameof(CanDeleteNarrative))]
    public async Task DeleteNarrative(Narrative Narrative)
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
                    .Match(Succ: s => _narrativeService.RemoveFromListNarratives(Narrative.Id),
                        Fail: async err =>
                        {
                            await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_NarrativeDeleteFailed_Title"),
                            err.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(err, "Deleting Narrative failed");
                        });
            }
            else
            {
                Logger.Information("Deleting Narrative not allowed");

                await ShowMessageDialog(
                    loc.GetLocalizedString("Messages_NarrativeDeleteNotAllowd_Title"),
                    loc.GetLocalizedString("Messages_NarrativeDeleteNotAllowed_Msg"),
                    loc.GetLocalizedString("Common_CloseButton"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to delete Narrative");
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_NarrativeDeleteFailed_Title"),
                            ex.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
        }
    }
    private bool CanDeleteNarrative(Narrative narrative)
    {
        if (narrative == null || narrative.Name == "- Not Assigned -") return false;
        return !_narrativeService.IsNarrativeHoldingCoins(narrative);   
    }

    [RelayCommand]
    public async Task NarrativeItemClicked(Narrative clickedNarrative)
    {
        //Clicked a new Narrative.... -> Resize Narrative ListView to small and Show Assets for this Narrative
        if (selectedNarrative == null || selectedNarrative != clickedNarrative)
        {
            IsExtendedView = true;
            await ShowAssets(clickedNarrative);
        }
        //clicked the already selected Narrative.... ->
        if (selectedNarrative != null && selectedNarrative == clickedNarrative)
        {
            // if Assets are not shown -> Decrease Narrative Listview to small and show assets for this Narrative
            if (!IsExtendedView)
            {
                IsExtendedView = true;
                await ShowAssets(clickedNarrative);
            }
            else //if Assets are shown -> close Assets List and resize Narratives Listview to full-size
            {
                _assetService.ClearAssetTotalsList();
                IsExtendedView = false;
            }
        }
        selectedNarrative = clickedNarrative;
    }

    [RelayCommand]
    public void HideZeroBalances(bool param)
    {
        _assetService.IsHidingZeroBalances = param;

    }

    [RelayCommand]
    public static void AssetItemClicked(AssetTotals clickedAsset)
    {
        //In the NarrativesView we ignore this command. The AssetsListViewControl is used in the AssetsView as well.
    }

    public async Task ShowAssets(Narrative clickedNarrative)
    {
        await _assetService.PopulateAssetTotalsByNarrativeList(clickedNarrative, currentSortingOrder, currentSortFunc);
    }

    [RelayCommand]
    private void TogglePrivacyMode()
    {
        IsPrivacyMode = !IsPrivacyMode;
    }
}

