using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Extensions;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public partial class TransactionDialog : ContentDialog //, INotifyPropertyChanged
{
    private readonly DialogAction dialogAction;
    public Transaction? transactionToEdit;
    public Transaction transactionNew;
    private readonly ITransactionService _transactionService;
    private readonly IPreferencesService _preferencesService;
    public Exception Exception;
    //private readonly DispatcherQueue dispatcherQueue;
    private readonly ILocalizer loc = Localizer.Get();

    [ObservableProperty] private TransactionKind transactionType;
    [ObservableProperty] private List<string> listCoinA;
    [ObservableProperty] private List<string> listCoinB;
    [ObservableProperty] private List<string> listAccountFrom;
    [ObservableProperty] private List<string> listAccountTo;
    [ObservableProperty] private List<string> listFeeCoin;
    [ObservableProperty] private string coinA;
    [ObservableProperty] private string coinB;
    [ObservableProperty] private string accountFrom;
    [ObservableProperty] private string accountTo;
    [ObservableProperty] private bool isAccountsLinked;
    [ObservableProperty] private bool isValidTest = false;
    [ObservableProperty] private string transactionText;
    [ObservableProperty] private string headerAccountFrom;
    [ObservableProperty] private string headerAccountTo;
    [ObservableProperty] private string headerCoinA;
    [ObservableProperty] private string headerCoinB;
    [ObservableProperty] private double maxQtyA;
    [ObservableProperty] private double qtyA;
    [ObservableProperty] private double actualPriceA;
    [ObservableProperty] private double priceA;
    [ObservableProperty] private double qtyB;
    [ObservableProperty] private double priceB;
    [ObservableProperty] private string feeCoin;
    [ObservableProperty] private double feeQty;
    [ObservableProperty] private string note;
    [ObservableProperty] private DateTimeOffset timeStamp;
    [ObservableProperty] private Validator validator;
    [ObservableProperty] private string decimalSeparator;
    [ObservableProperty] private Visibility earningsCheckBoxVisibility;
    [ObservableProperty] private bool isEarnings;


    partial void OnIsEarningsChanged(bool value)
    {
        if (value)
        {
            PriceA = 0;
        }
        else
        {
            GetMaxQtyAndPrice();
        }
    }
    partial void OnTransactionTypeChanged(TransactionKind value)
    {
        if (value == TransactionKind.Deposit)
        {
            EarningsCheckBoxVisibility = Visibility.Visible;
        }
        else EarningsCheckBoxVisibility = Visibility.Collapsed;
    }
    partial void OnListFeeCoinChanged(List<string> value)
    {
        if (ListFeeCoin != null && ListFeeCoin.Count > 0)
        {
            var result = ListFeeCoin.Where(x => x.ToString().ToLower() == "eth ethereum").FirstOrDefault();
            if (result != null)
            {
                FeeCoin = (string)result;
            }
            else { FeeCoin = ""; }
        }
    }
    partial void OnCoinAChanged(string value)
    {
        if (AccountFrom != null && AccountFrom != "")
        {
            GetMaxQtyAndPrice();
        }
        if (FeeCoin is null || FeeCoin == string.Empty)
        {
            FeeCoin = CoinA;
        }
    }
    partial void OnAccountFromChanged(string value)
    {
        if (CoinA != null && CoinA != "")
        {
            GetMaxQtyAndPrice();
        }
    }
    partial void OnQtyAChanged(double value)
    {
        CalculatePriceB();
    }
    
    partial void OnQtyBChanged(double value)
    {
        CalculatePriceB();
    }
    partial void OnPriceAChanged(double value)
    {
        CalculatePriceB();
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TransactionDialog(ITransactionService transactionService, IPreferencesService preferencesService, DialogAction _dialogAction, Transaction? transaction = null)
    {
        InitializeComponent();
        //dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        dialogAction = _dialogAction;
        _transactionService = transactionService;
        _preferencesService = preferencesService;
        transactionToEdit = transaction;
        InitializeAllFields();
        TimeStamp = DateTimeOffset.Parse(DateTime.Now.ToString());
        Validator = new Validator(10, true);
        SetDialogButtonsAndTitle(dialogAction);
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private void SetDialogButtonsAndTitle(DialogAction dialogAction)
    {
        var index = -1;
        if (dialogAction == DialogAction.Edit && transactionToEdit != null)
        {

            for (var i = 0; i < TransactionTypeRadioButtons.Items.Count; i++)
            {
                if ((TransactionTypeRadioButtons.Items[i] is RadioButton rbutton) && rbutton.Content.ToString() == transactionToEdit.Details.TransactionType.AsDisplayString())
                {
                    index = i;
                    break;
                }
            }
            TransactionTypeRadioButtons.SelectedIndex = index;
            Title = loc.GetLocalizedString("TransactionDialog_Title_Edit");
        }
        else
        {
            TransactionTypeRadioButtons.SelectedIndex = 0;
            Title = loc.GetLocalizedString("TransactionDialog_Title_Add");
        }
        PrimaryButtonText = loc.GetLocalizedString("TransactionDialog_PrimaryButton");
        CloseButtonText = loc.GetLocalizedString("TransactionDialog_CloseButton");
    }
    private async void GetMaxQtyAndPrice()
    {
        if (dialogAction == DialogAction.Add && TransactionType != TransactionKind.Deposit)
        {
            var result = await _transactionService.GetMaxQtyAndPrice(CoinA, AccountFrom);
            result.IfSucc(values =>
            {
                MaxQtyA = values[0];
                ActualPriceA = PriceA = TransactionType != TransactionKind.Transfer ? values[1] : values[2];
            });
        }
        else
        {
            MaxQtyA = -1;
            var priceResult = await _transactionService.GetPriceFromLibrary(CoinA);
            ActualPriceA = PriceA = priceResult.Match(price => price, _ => 0);
        }
    }

    private async Task<Transaction> WrapUpTransactionData()
    {
        var newMutations = new List<Mutation>();

        Mutation mutationToAdd;

        switch (TransactionType)
        {
            case TransactionKind.Deposit:
                //** IN portion
                mutationToAdd = (await GetMutation(TransactionType, MutationDirection.In, CoinA, QtyA, PriceA, AccountFrom))
                    .Match(Succ: mutation => mutation, Fail: err => throw err);
                newMutations.Add(mutationToAdd);
                break;
            case TransactionKind.Withdraw:
                //** OUT portion
                mutationToAdd = (await GetMutation(TransactionType, MutationDirection.Out, CoinA, QtyA, PriceA, AccountFrom))
                    .Match(Succ: mutation => mutation, Fail: err => throw err);
                newMutations.Add(mutationToAdd);
                break;
            case TransactionKind.Transfer:
                //** OUT portion
                mutationToAdd = (await GetMutation(TransactionType, MutationDirection.Out, CoinA, QtyA, PriceA, AccountFrom))
                    .Match(Succ: mutation => mutation, Fail: err => throw err);
                newMutations.Add(mutationToAdd);
                //** IN portion
                mutationToAdd = (await GetMutation(TransactionType, MutationDirection.In, CoinA, QtyA, PriceA, AccountTo))
                    .Match(Succ: mutation => mutation, Fail: err => throw err);
                newMutations.Add(mutationToAdd);

                //** OUT portion in case of paid FEE
                if (FeeQty > 0)
                {
                    mutationToAdd = (await GetMutation(TransactionKind.Fee, MutationDirection.Out, FeeCoin, FeeQty, 0, AccountFrom))
                    .Match(Succ: mutation => mutation, Fail: err => throw err);
                    newMutations.Add(mutationToAdd);
                }
                break;
            case TransactionKind.Convert:
            case TransactionKind.Buy:
            case TransactionKind.Sell:
                //Do we have a Tx combined with Transfer
                if (AccountFrom == AccountTo) // Not combined with transfer
                {
                    //** OUT portion
                    mutationToAdd = (await GetMutation(TransactionType, MutationDirection.Out, CoinA, QtyA, PriceA, AccountFrom))
                        .Match(Succ: mutation => mutation, Fail: err => throw err);
                    newMutations.Add(mutationToAdd);
                    //** IN portion
                    mutationToAdd = (await GetMutation(TransactionType, MutationDirection.In, CoinB, QtyB, PriceB, AccountFrom))
                        .Match(Succ: mutation => mutation, Fail: err => throw err);
                    newMutations.Add(mutationToAdd);
                }
                else // combined with Transfer
                {
                    //** OUT portion
                    mutationToAdd = (await GetMutation(TransactionType, MutationDirection.Out, CoinA, QtyA, PriceA, AccountFrom))
                        .Match(Succ: mutation => mutation, Fail: err => throw err);
                    newMutations.Add(mutationToAdd);
                    //** IN portion
                    mutationToAdd = (await GetMutation(TransactionKind.Transfer, MutationDirection.In, CoinB, QtyB, PriceB, AccountTo))
                        .Match(Succ: mutation => mutation, Fail: err => throw err);
                    newMutations.Add(mutationToAdd);
                }
                //** OUT portion in case of paid FEE
                if (FeeQty > 0)
                {
                    mutationToAdd = (await GetMutation(TransactionKind.Fee, MutationDirection.Out, FeeCoin, FeeQty, 0, AccountFrom))
                    .Match(Succ: mutation => mutation, Fail: err => throw err);
                    newMutations.Add(mutationToAdd);
                }
                break;
        }
        var _coinA = CoinA != string.Empty && CoinA.Contains(' ') ? CoinA.Split(" ", 2) : new string[2] { string.Empty, string.Empty };
        var _coinB = CoinB != string.Empty && CoinB.Contains(' ') ? CoinB.Split(" ", 2) : new string[2] { string.Empty, string.Empty };
        var _coinFee = FeeCoin != string.Empty && FeeCoin.Contains(' ') ? FeeCoin.Split(" ", 2) : new string[2] { string.Empty, string.Empty };

        var transactionToAdd = TransactionBuilder.Create()
            .ExecutedOn(DateTime.Parse(TimeStamp.ToString()))
            .WithNote(Note)
            .WithMutations(newMutations)
            .WithDetails(
                transactionDetails => transactionDetails
                    .OfTransactionType(TransactionType)
                    .FromCoinSymbol(_coinA[0])
                    .FromCoinName(_coinA[1])
                    .FromPrice(PriceA)
                    .FromQty(QtyA)
                    .FromAccount(AccountFrom)
                    .ToCoinSymbol(_coinB[0])
                    .ToCoinName(_coinB[1])
                    .ToPrice(PriceB)
                    .ToQty(QtyB)
                    .ToAccount(AccountTo)
                    .FeeCoinSymbol(_coinFee[0])
                    .FeeCoinName(_coinFee[1])
                    .FeeQty(FeeQty))
            .ForAsset(dialogAction == DialogAction.Edit && transactionToEdit != null ? transactionToEdit.RequestedAsset : null)
            .WithId(dialogAction == DialogAction.Edit && transactionToEdit != null ? transactionToEdit.Id : 0)
            .Build();

        return transactionToAdd;
    }
    private async Task<Result<Mutation>> GetMutation(TransactionKind type, MutationDirection direction, string _symbolName, double qty, double price, string _account)
    {
        try
        {
            var coinResult = await _transactionService.GetCoinBySymbol(_symbolName);
            var coin = coinResult.IfFail(err => throw err);

            var accountResult = await _transactionService.GetAccountByName(_account);
            var account = accountResult.IfFail(err => throw err);
            
            var assetIdResult = await _transactionService.GetAssetIdByCoinAndAccount(coin, account);
            var assetId = assetIdResult.IfFail(err => throw err);

            var mutation = MutationBuilder.Create()
                .OfType(type)
                .QtyOf(qty)
                .PriceOf(price)
                .Direction(direction)
                .WithAsset(
                    asset => asset
                        .WithCoin(coin)
                        .WithAccount(account)
                        .WithId(assetId))
                .Build();

            return mutation;
        }
        catch (Exception ex)
        {
            return new Result<Mutation>(ex);
        }
    }

    private void CalculatePriceB()
    {
        if (QtyB == 0)
        {
            PriceB = 0;
        }
        else
        {
            PriceB = QtyA * PriceA / QtyB;
        }
        OnPropertyChanged(nameof(QtyA));
    }

    private void ConfigureDialogFields(string explainType, string assetTextA, string assetTextB, string accountTextA, string accountTextB, bool isAccountLinkEnabled = true)
    {
        TransactionText = explainType;
        HeaderAccountFrom = accountTextA;
        HeaderAccountTo = accountTextB;
        HeaderCoinA = assetTextA;
        HeaderCoinB = assetTextB;

        IsAccountsLinked = isAccountLinkEnabled;
        TBoxPriceA.Visibility = isAccountLinkEnabled ? Visibility.Visible : Visibility.Collapsed;

        CoinSelectionB.Visibility = string.IsNullOrEmpty(HeaderCoinB) ? Visibility.Collapsed : Visibility.Visible;
        QtyAndPriceSelectionB.Visibility = string.IsNullOrEmpty(HeaderCoinB) ? Visibility.Collapsed : Visibility.Visible;

        AccountSelectionB.Visibility = string.IsNullOrEmpty(HeaderAccountTo) ? Visibility.Collapsed : Visibility.Visible;
        LinkAccountsButton.Visibility = isAccountLinkEnabled && !string.IsNullOrEmpty(HeaderAccountTo) ? Visibility.Visible : Visibility.Collapsed;

        FeeSelection.Visibility = string.IsNullOrEmpty(HeaderCoinB) && string.IsNullOrEmpty(HeaderAccountTo) ? Visibility.Collapsed : Visibility.Visible;
    }

    private static string ClearStringIfNoMatchWithList(string _string, List<string> list)
    {
        return list.Contains(_string) ? _string : string.Empty;
    }

    private void InitializeAllFields()
    {
        DecimalSeparator = _preferencesService.GetNumberFormat().NumberDecimalSeparator;

        CoinA = CoinB = AccountFrom = AccountTo = Note = FeeCoin = "";
        HeaderCoinA = HeaderCoinB = HeaderAccountFrom = HeaderAccountTo = "";

        QtyA = MaxQtyA = PriceA = PriceB = QtyB = FeeQty = -1;
        QtyA = MaxQtyA = PriceA = PriceB = QtyB = FeeQty = 0;


        ListCoinA = ListCoinA ?? new List<string>();
        ListCoinA.Clear();

        ListCoinB = ListCoinB ?? new List<string>();
        ListCoinB.Clear();

        ListAccountFrom = ListAccountFrom ?? new List<string>();
        ListAccountFrom.Clear();

        ListAccountTo = ListAccountTo ?? new List<string>();
        ListAccountTo.Clear();

        ListFeeCoin = ListFeeCoin ?? new List<string>();
        ListFeeCoin.Clear();
    }

    private async void TransactionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        InitializeAllFields();
        var CoinFromHeader = loc.GetLocalizedString("TransactionDialog_CoinFromHeader");
        var CoinToHeader = loc.GetLocalizedString("TransactionDialog_CoinToHeader");
        var CoinHeader = loc.GetLocalizedString("TransactionDialog_CoinHeader");
        var AccountFromHeader = loc.GetLocalizedString("TransactionDialog_AccountFromHeader");
        var AccountToHeader = loc.GetLocalizedString("TransactionDialog_AccountToHeader");

        if (sender is RadioButtons rb)
        {
            var selectedIndex = rb.SelectedIndex;
            var coinSymbolsTask = selectedIndex switch
            {
                0 => _transactionService.GetCoinSymbolsFromLibrary(),
                1 => _transactionService.GetCoinSymbolsFromAssets(),
                2 => _transactionService.GetCoinSymbolsFromAssets(),
                3 => _transactionService.GetCoinSymbolsExcludingUsdtUsdcFromAssets(),
                4 => _transactionService.GetUsdtUsdcSymbolsFromAssets(),
                5 => _transactionService.GetCoinSymbolsExcludingUsdtUsdcFromAssets(),
                _ => Task.FromResult(new Result<List<string>>(new List<string>()))
            };

            var coinSymbolsResult = await coinSymbolsTask;
            ListCoinA = coinSymbolsResult.Match(Succ: list => list, Fail: err => new List<string>());

            switch (selectedIndex)
            {
                case 0: // Deposit
                    Validator.RegisterEntriesToValidate(new int[4] { 0, 2, 4, 5 });
                    TransactionType = TransactionKind.Deposit;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Deposit_Explainer"), CoinHeader, "", AccountToHeader, "");
                    ListCoinB = new List<string>();
                    ListAccountTo = new List<string>();
                    ListFeeCoin = new List<string>();
                    break;
                case 1: // Withdraw
                    Validator.RegisterEntriesToValidate(new int[4] { 0, 2, 4, 5 });
                    TransactionType = TransactionKind.Withdraw;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Withdraw_Explainer"), CoinHeader, "", AccountFromHeader, "");
                    ListCoinB = new List<string>();
                    ListAccountTo = new List<string>();
                    ListFeeCoin = new List<string>();
                    break;
                case 2: // Transfer
                    Validator.RegisterEntriesToValidate(new int[6] { 0, 2, 3, 4, 8, 9 });
                    TransactionType = TransactionKind.Transfer;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Transfer_Explainer"), CoinHeader, "", AccountFromHeader, AccountToHeader, false);
                    ListFeeCoin = ListCoinA;
                    ListCoinB = new List<string>();
                    break;
                case 3: // Convert
                    Validator.RegisterEntriesToValidate(new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                    TransactionType = TransactionKind.Convert;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Convert_Explainer"), CoinFromHeader, CoinToHeader, AccountFromHeader, AccountToHeader);
                    ListCoinB = (await _transactionService.GetCoinSymbolsExcludingUsdtUsdcFromLibrary()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountTo = new List<string>();
                    ListFeeCoin = ListCoinA;
                    break;
                case 4: // Buy
                    Validator.RegisterEntriesToValidate(new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                    TransactionType = TransactionKind.Buy;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Buy_Explainer"), CoinFromHeader, CoinToHeader, AccountFromHeader, AccountToHeader);
                    ListCoinB = (await _transactionService.GetCoinSymbolsExcludingUsdtUsdcFromLibrary()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountTo = new List<string>();
                    ListFeeCoin = (await _transactionService.GetCoinSymbolsFromAssets()).Match(Succ: list => list, Fail: err => new List<string>());
                    break;
                case 5: // Sell
                    Validator.RegisterEntriesToValidate(new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                    TransactionType = TransactionKind.Sell;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Sell_Explainer"), CoinFromHeader, CoinToHeader, AccountFromHeader, AccountToHeader);
                    ListCoinB = (await _transactionService.GetUsdtUsdcSymbolsFromLibrary()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountTo = new List<string>();
                    ListFeeCoin = ListCoinA;
                    break;
            }

            if (dialogAction == DialogAction.Add)
            {
                TimeStamp = DateTimeOffset.Parse(DateTime.Now.ToString());
            }
            else if (dialogAction == DialogAction.Edit && transactionToEdit != null)
            {
                CoinA = transactionToEdit.Details.CoinASymbol + " " + transactionToEdit.Details.CoinAName;
                CoinB = transactionToEdit.Details.CoinBSymbol + " " + transactionToEdit.Details.CoinBName;
                AccountFrom = transactionToEdit.Details.AccountFrom;
                AccountTo = transactionToEdit.Details.AccountTo;
                Validator.UnRegisterEntriesToValidate(new int[4] { 0, 1, 2, 3 });

                QtyA = transactionToEdit.Details.QtyA;
                QtyB = transactionToEdit.Details.QtyB;
                PriceA = transactionToEdit.Details.PriceA;
                PriceB = transactionToEdit.Details.PriceB;
                FeeCoin = transactionToEdit.Details.FeeCoinSymbol + " " + transactionToEdit.Details.FeeCoinName;
                FeeQty = transactionToEdit.Details.FeeQty;
                Note = transactionToEdit.Note;
                TimeStamp = DateTimeOffset.Parse(transactionToEdit.TimeStamp.ToString());
                TransactionTypeRadioButtons.IsEnabled = false;
                ASBoxCoinA.IsEnabled = false;
                ASBoxCoinB.IsEnabled = false;
                ASBoxAccountFrom.IsEnabled = false;
                ASBoxAccountTo.IsEnabled = false;
                IsEarnings = PriceA == 0;
            }
        }
    }

    private async void ASBoxCoinA_TextChanged(object sender, EventArgs e)
    {
        if (sender is AutoSuggestBoxWithValidation asBox)
        {
            var entry = asBox.MyText ?? string.Empty;

            if (ListCoinA.Contains(entry))
            {
                await SetEntriesBasedOnCoinA(entry);
            }
            else
            {
                ListAccountFrom?.Clear();
                AccountFrom = string.Empty;
            }
        }
    }

    private async void ASBoxCoinA_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var selectedText = args.SelectedItem.ToString() ?? string.Empty;
        await SetEntriesBasedOnCoinA(selectedText);
    }
    
    private async Task SetEntriesBasedOnCoinA(string coin)
    {
        List<string> accountNames;
        List<string> coinSymbols;

        switch (TransactionType)
        {
            case TransactionKind.Deposit:
                accountNames = await GetAccountNames();
                break;
            case TransactionKind.Withdraw:
            case TransactionKind.Transfer:
                accountNames = await GetAccountNames(coin);
                break;
            case TransactionKind.Convert:
                accountNames = await GetAccountNames(coin);
                coinSymbols = await GetCoinSymbolsExcludingUsdtUsdcFromLibraryExcluding(coin);
                ListCoinB = coinSymbols;
                ListAccountTo = await GetAccountNames();
                CoinB = ClearStringIfNoMatchWithList(CoinB, ListCoinB);
                break;
            case TransactionKind.Buy:
                accountNames = await GetAccountNames(coin);
                ListAccountTo = await GetAccountNames();
                coinSymbols = await GetCoinSymbolsExcludingUsdtUsdcFromLibrary();
                ListCoinB = coinSymbols;
                CoinB = ClearStringIfNoMatchWithList(CoinB, ListCoinB);
                break;
            case TransactionKind.Sell:
                accountNames = await GetAccountNames(coin);
                ListAccountTo = await GetAccountNames();
                CoinB = ClearStringIfNoMatchWithList(CoinB, ListCoinB);
                break;
            default:
                accountNames = new List<string>();
                break;
        }

        ListAccountFrom = accountNames;
        AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
    }

    private async Task<List<string>> GetAccountNames(string coin)
    {
        return (await _transactionService.GetAccountNames(coin))
            .Match(Succ: list => list, Fail: err => new List<string>());
    }

    private async Task<List<string>> GetAccountNames()
    {
        return (await _transactionService.GetAccountNames(string.Empty))
            .Match(Succ: list => list, Fail: err => new List<string>());
    }
    private async Task<List<string>> GetCoinSymbolsExcludingUsdtUsdcFromLibraryExcluding(string coin)
    {
        return (await _transactionService.GetCoinSymbolsExcludingUsdtUsdcFromLibraryExcluding(coin))
            .Match(Succ: list => list, Fail: err => new List<string>());
    }

    private async Task<List<string>> GetCoinSymbolsExcludingUsdtUsdcFromLibrary()
    {
        return (await _transactionService.GetCoinSymbolsExcludingUsdtUsdcFromLibrary())
            .Match(Succ: list => list, Fail: err => new List<string>());
    }

    private async void ASBoxAccountFrom_TextChanged(object sender, EventArgs e)
    {
        switch (TransactionType)
        {
            case TransactionKind.Transfer:
                if (AccountFrom == AccountTo)
                {
                    AccountTo = "";
                }
                ListAccountTo = (await _transactionService.GetAccountNamesExcluding(AccountFrom))
                    .Match(Succ: list => list, Fail: err => new List<string>());
                break;
            case TransactionKind.Convert:
            case TransactionKind.Buy:
            case TransactionKind.Sell:
                if (IsAccountsLinked && ListAccountFrom.Contains(AccountFrom))
                {
                    AccountTo = AccountFrom;
                }
                break;
        }
    }

    private async void ASBoxAccountFrom_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        AccountFrom = args.SelectedItem.ToString() ?? string.Empty;
        if (IsAccountsLinked)
        {
            AccountTo = AccountFrom;
        }

        if (TransactionType == TransactionKind.Transfer)
        {
            ListAccountTo = (await _transactionService.GetAccountNamesExcluding(AccountFrom))
                .Match(Succ: list => list, Fail: err => new List<string>());
            AccountTo = ClearStringIfNoMatchWithList(AccountTo, ListAccountTo);
        }
    }
    private void LinkAccountsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        IsAccountsLinked = !IsAccountsLinked;
        AccountTo = IsAccountsLinked ? AccountFrom : string.Empty;
    }
    
    private void QtyOrPrice_pressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is RegExTextBox tbox)
        {
            tbox.SelectAll();
        }
    }

    private void TBoxPriceA_TextChanged(object sender, EventArgs e)
    {
        if (sender is RegExTextBox { Text: "" })
        {
            PriceA = ActualPriceA;
            OnPropertyChanged(nameof(PriceA));
        }
    }
    
    public async void PrimaryButton_AcceptTransaction(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        try
        {
            Validator.Stop();
            transactionNew = await WrapUpTransactionData();
        }
        catch (Exception ex)
        {
            transactionNew = new();
            Exception = ex;
        }
    }
   
    private void CloseButton_Cancel(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Validator.Stop();
    }

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        var appTheme = _preferencesService.GetAppTheme();
        if (sender.ActualTheme != appTheme)
        {
            sender.RequestedTheme = appTheme;
        }
    }

    private void EarningsCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        PriceA = 0;
    }


    private void TBoxQtyA_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tbox)
        {
            if (double.TryParse(tbox.Text, NumberStyles.Any, new CultureInfo(loc.GetCurrentLanguage()) { NumberFormat = _preferencesService.GetNumberFormat() }, out double value))
            {
                if (MaxQtyA != -1 && value > MaxQtyA)
                {
                    tbox.Text = MaxQtyA.ToString(CultureInfo.CurrentCulture);
                }
            }
        }
    }

}

