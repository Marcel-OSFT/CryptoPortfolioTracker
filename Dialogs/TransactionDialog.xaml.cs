using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    private readonly DispatcherQueue dispatcherQueue;
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
        if (listFeeCoin != null && listFeeCoin.Count > 0)
        {
            var result = listFeeCoin.Where(x => x.ToString().ToLower() == "eth ethereum").FirstOrDefault();
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
            FeeCoin = coinA;
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

   
    #region Constructor(s)

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TransactionDialog(ITransactionService transactionService, IPreferencesService preferencesService, DialogAction _dialogAction, Transaction? transaction = null)
    {
        InitializeComponent();
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
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
    #endregion Constructors

    #region Methods  
    
    private async void GetMaxQtyAndPrice()
    {
        //in case of a 'Deposit' the MaxQtyA doesn't need to be set....
        //doesn't need to be set as wel for an EDIT transaction
        if (dialogAction == DialogAction.Add && transactionType != TransactionKind.Deposit)
        {
            (await _transactionService.GetMaxQtyAndPrice(CoinA, AccountFrom)).IfSucc(result =>
            {
                MaxQtyA = result[0];
                if (transactionType != TransactionKind.Transfer)
                {
                    ActualPriceA = PriceA = result[1];
                }
                else
                {
                    ActualPriceA = PriceA = result[2];
                }
            });
        }
        else
        {
            MaxQtyA = -1;
            ActualPriceA = PriceA = (await _transactionService.GetPriceFromLibrary(CoinA)).Match(Succ: price => price, Fail: err => 0);
        }
    }
    private async Task<Transaction> WrapUpTransactionData()
    {
        var transactionToAdd = new Transaction();
        var newMutations = new List<Mutation>();

        Mutation mutationToAdd;

        switch (transactionType.ToString())
        {
            case "Deposit":
                //** IN portion
                mutationToAdd = (await GetMutation(transactionType, MutationDirection.In, CoinA, QtyA, PriceA, AccountFrom))
                    .Match(Succ: mutation => mutation, Fail: err => throw err);
                newMutations.Add(mutationToAdd);
                break;
            case "Withdraw":
                //** OUT portion
                mutationToAdd = (await GetMutation(transactionType, MutationDirection.Out, CoinA, QtyA, PriceA, AccountFrom))
                    .Match(Succ: mutation => mutation, Fail: err => throw err);
                newMutations.Add(mutationToAdd);
                break;
            case "Transfer":
                //** OUT portion
                mutationToAdd = (await GetMutation(transactionType, MutationDirection.Out, CoinA, QtyA, PriceA, AccountFrom))
                    .Match(Succ: mutation => mutation, Fail: err => throw err);
                newMutations.Add(mutationToAdd);
                //** IN portion
                mutationToAdd = (await GetMutation(transactionType, MutationDirection.In, CoinA, QtyA, PriceA, AccountTo))
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
            case "Convert":
            case "Buy":
            case "Sell":
                //Do we have a Tx combined with TRansfer
                if (AccountFrom == AccountTo) // Not combined with transfer
                {
                    //** OUT portion
                    mutationToAdd = (await GetMutation(transactionType, MutationDirection.Out, CoinA, QtyA, PriceA, AccountFrom))
                        .Match(Succ: mutation => mutation, Fail: err => throw err);
                    newMutations.Add(mutationToAdd);
                    //** IN portion
                    mutationToAdd = (await GetMutation(transactionType, MutationDirection.In, CoinB, QtyB, PriceB, AccountFrom))
                        .Match(Succ: mutation => mutation, Fail: err => throw err);
                    newMutations.Add(mutationToAdd);
                }
                else // combined with Transfer
                {
                    //** OUT portion
                    mutationToAdd = (await GetMutation(transactionType, MutationDirection.Out, CoinA, QtyA, PriceA, AccountFrom))
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
        var _coinB = CoinB != string.Empty && CoinB.Contains(' ') ? CoinB.Split(" ", 2) : new string[2] {string.Empty, string.Empty };
        var _coinFee = FeeCoin != string.Empty && FeeCoin.Contains(' ') ? FeeCoin.Split(" ", 2) : new string[2] { string.Empty, string.Empty };

        var transactionDetails = new TransactionDetails
        {
            CoinASymbol = _coinA[0],
            CoinAName = _coinA[1],
            CoinBSymbol = _coinB[0],
            CoinBName = _coinB[1],
            FeeCoinSymbol = _coinFee[0],
            FeeCoinName = _coinFee[1],
            AccountFrom = AccountFrom,
            AccountTo = AccountTo,
            FeeQty = FeeQty,
            PriceA = PriceA,
            PriceB = PriceB,
            QtyA = QtyA,
            QtyB = QtyB,
            TransactionType = transactionType,
        };
        transactionToAdd.Note = Note;
        transactionToAdd.TimeStamp = DateTime.Parse(TimeStamp.ToString());

        transactionToAdd.Details = transactionDetails;
        transactionToAdd.Mutations = newMutations;

        if (dialogAction == DialogAction.Edit && transactionToEdit != null)
        {
            transactionToAdd.RequestedAsset = transactionToEdit.RequestedAsset;
            transactionToAdd.Id = transactionToEdit.Id;
        }
        return transactionToAdd;
    }
    private async Task<Result<Mutation>> GetMutation(TransactionKind type, MutationDirection direction, string symbolName, double qty, double price, string account)
    {
        var mutation = new Mutation();
        try
        {
            // mutation.Asset.Coin.Symbol = symbol;
            mutation.Asset.Coin = (await _transactionService.GetCoinBySymbol(symbolName))
                .Match(Succ: coin => coin, Fail: err => throw new Exception(err.Message, err));
            //mutation.Asset.Account.Name = account;
            mutation.Asset.Account = (await _transactionService.GetAccountByName(account))
                .Match(Succ: account => account, Fail: err => throw new Exception(err.Message, err));
            mutation.Asset.Id = (await _transactionService.GetAssetIdByCoinAndAccount(mutation.Asset.Coin, mutation.Asset.Account))
                                 .Match(Succ: account => account, Fail: err => throw new Exception(err.Message, err));
            mutation.Qty = qty;
            mutation.Price = price;
            //mutation.Transaction = transaction;
            mutation.Direction = direction;
            mutation.Type = type;
        }
        catch (Exception ex)
        {
            return new Result<Mutation>(ex);
        }
        return mutation;
    }
    private void CalculatePriceB()
    {
        if (QtyB == 0)
        {
            PriceB = 0;
            return;
        }
        PriceB = QtyA * PriceA / QtyB;
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
        if (isAccountLinkEnabled)
        {
            TBoxPriceA.Visibility = Visibility.Visible;
        }
        else //transfer
        {
            TBoxPriceA.Visibility = Visibility.Collapsed;
        }

        if (HeaderCoinB == "")
        {
            CoinSelectionB.Visibility = Visibility.Collapsed;
            QtyAndPriceSelectionB.Visibility = Visibility.Collapsed;
        }
        else
        {
            CoinSelectionB.Visibility = Visibility.Visible;
            QtyAndPriceSelectionB.Visibility = Visibility.Visible;
        }
        if (HeaderAccountTo == "")
        {
            AccountSelectionB.Visibility = Visibility.Collapsed;
            LinkAccountsButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            AccountSelectionB.Visibility = Visibility.Visible;
            if (isAccountLinkEnabled)
            {
                LinkAccountsButton.Visibility = Visibility.Visible;
            }
            else
            {
                LinkAccountsButton.Visibility = Visibility.Collapsed;
            }
        }
        if (HeaderCoinB == "" && HeaderAccountTo == "")
        {
            FeeSelection.Visibility = Visibility.Collapsed;
        }
        else
        {
            FeeSelection.Visibility = Visibility.Visible;
        }

    }
    private string ClearStringIfNoMatchWithList(string _string, List<string> list)
    {
        if (!list.Contains(_string))
        {
            _string = "";
        }
        return _string;
    }
    private void InitializeAllFields()
    {
        DecimalSeparator = _preferencesService.GetNumberFormat().NumberDecimalSeparator;

        CoinA = CoinB = AccountFrom = AccountTo = Note = FeeCoin = "";
        HeaderCoinA = HeaderCoinB = HeaderAccountFrom = HeaderAccountTo = "";

        QtyA = 0;
        MaxQtyA = 0;
        PriceA = 0;
        PriceB = 0;
        QtyB = 0;
        FeeQty = 0;

        if (ListCoinA != null)
        {
            ListCoinA.Clear();
        }
        else
        {
            ListCoinA = new List<string>();
        }

        if (ListCoinB != null)
        {
            ListCoinB.Clear();
        }
        else
        {
            ListCoinB = new List<string>();
        }

        if (ListAccountFrom != null)
        {
            ListAccountFrom.Clear();
        }
        else
        {
            ListAccountFrom = new List<string>();
        }

        if (ListAccountTo != null)
        {
            ListAccountTo.Clear();
        }
        else
        {
            ListAccountTo = new List<string>();
        }

        if (ListFeeCoin != null)
        {
            ListFeeCoin.Clear();
        }
        else
        {
            ListFeeCoin = new List<string>();
        }
    }
    #endregion Methods

    #region TransactionType Events
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
            switch (rb.SelectedIndex)
            {
                case 0: //Deposit                       
                    Validator.RegisterEntriesToValidate(new int[4] { 0, 2, 4, 5 });
                    TransactionType = TransactionKind.Deposit;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Deposit_Explainer"), CoinHeader, "", AccountToHeader, "");
                    ListCoinA = (await _transactionService
                        .GetCoinSymbolsFromLibrary())
                        .Match(Succ: list => list, Fail: err => new List<string>());
                    ListCoinB = new List<string>();
                    ListAccountTo = new List<string>();
                    ListFeeCoin = new List<string>();
                    break;
                case 1: //Withdraw
                    Validator.RegisterEntriesToValidate(new int[4] { 0, 2, 4, 5 });
                    TransactionType =  TransactionKind.Withdraw;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Withdraw_Explainer"), CoinHeader, "", AccountFromHeader, "");
                    ListCoinA = (await _transactionService
                        .GetCoinSymbolsFromAssets())
                        .Match(Succ: list => list, Fail: err => new List<string>());
                    ListCoinB = new List<string>();
                    ListAccountTo = new List<string>();
                    ListFeeCoin = new List<string>();
                    break;
                case 2: //Transfer
                    Validator.RegisterEntriesToValidate(new int[6] { 0, 2, 3, 4, 8, 9 });
                    TransactionType =  TransactionKind.Transfer;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Transfer_Explainer"), CoinHeader, "", AccountFromHeader, AccountToHeader, false);
                    ListCoinA = (await _transactionService
                        .GetCoinSymbolsFromAssets())
                        .Match(Succ: list => list, Fail: err => new List<string>());
                    ListFeeCoin = ListCoinA;
                    ListCoinB = new List<string>();
                    break;
                case 3: //Convert
                    Validator.RegisterEntriesToValidate(new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                    TransactionType =  TransactionKind.Convert;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Convert_Explainer"), CoinFromHeader, CoinToHeader, AccountFromHeader, AccountToHeader);
                    ListCoinA = (await _transactionService
                        .GetCoinSymbolsExcludingUsdtUsdcFromAssets())
                        .Match(Succ: list => list, Fail: err => new List<string>());
                    ListCoinB = (await _transactionService
                        .GetCoinSymbolsExcludingUsdtUsdcFromLibrary())
                        .Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountTo = new List<string>();
                    ListFeeCoin = ListCoinA;
                    break;
                case 4: //Buy
                    Validator.RegisterEntriesToValidate(new int[10] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                    TransactionType =  TransactionKind.Buy;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Buy_Explainer"), CoinFromHeader, CoinToHeader, AccountFromHeader, AccountToHeader);
                    ListCoinA = (await _transactionService
                        .GetUsdtUsdcSymbolsFromAssets())
                        .Match(Succ: list => list, Fail: err => new List<string>());
                    ListCoinB = (await _transactionService
                        .GetCoinSymbolsExcludingUsdtUsdcFromLibrary())
                        .Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountTo = new List<string>();
                    ListFeeCoin = (await _transactionService
                        .GetCoinSymbolsFromAssets())
                        .Match(Succ: list => list, Fail: err => new List<string>());
                    break;
                case 5: //Sell
                    Validator.RegisterEntriesToValidate(new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                    TransactionType =  TransactionKind.Sell;
                    ConfigureDialogFields(loc.GetLocalizedString("TransactionDialog_Sell_Explainer"), CoinFromHeader, CoinToHeader, AccountFromHeader, AccountToHeader);
                    ListCoinA = (await _transactionService
                        .GetCoinSymbolsExcludingUsdtUsdcFromAssets())
                        .Match(Succ: list => list, Fail: err => new List<string>());
                    ListCoinB = (await _transactionService
                        .GetUsdtUsdcSymbolsFromLibrary())
                        .Match(Succ: list => list, Fail: err => new List<string>());
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
                // above items are set and can not change while editing
                // so they don't need to be validated and need to be unregistered
                Validator.UnRegisterEntriesToValidate(new int[4] { 0, 1, 2, 3});

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
                IsEarnings = PriceA == 0 ;
            }
        }
    }
    #endregion TransactionType Events

    #region ASBoxCoinA Events       
    private async void ASBoxCoinA_TextChanged(object sender, EventArgs e)
    {
        var asBox = (sender as AutoSuggestBoxWithValidation);
        var entry = (asBox is not null) ? asBox.MyText : string.Empty; 
        
        if (ListCoinA.Contains(entry))
        {
           await SetEntriesBasedOnCoinA(entry);
        }
        else
        {
            if (ListAccountFrom is not null)
            {
                ListAccountFrom = new List<string>();
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
        switch (transactionType.ToString())
        {
            case "Deposit": //->ASBoxCoinA
                ListAccountFrom = (await _transactionService
                    .GetAccountNames())
                    .Match(Succ: list => list, Fail: err => new List<string>());
                AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
                break;
            case "Withdraw": //->ASBoxCoinA
                ListAccountFrom = (await _transactionService
                    .GetAccountNames(coin))
                    .Match(Succ: list => list, Fail: err => new List<string>());
                AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
                break;
            case "Transfer": //->ASBoxCoinA
                ListAccountFrom = (await _transactionService
                    .GetAccountNames(coin))
                    .Match(Succ: list => list, Fail: err => new List<string>());
                AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
                break;
            case "Convert": //->ASBoxCoinA
                ListAccountFrom = (await _transactionService
                    .GetAccountNames(coin))
                    .Match(Succ: list => list, Fail: err => new List<string>());
                AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
                ListCoinB = (await _transactionService
                    .GetCoinSymbolsExcludingUsdtUsdcFromLibraryExcluding(coin))
                    .Match(Succ: list => list, Fail: err => new List<string>());
                ListAccountTo = (await _transactionService
                    .GetAccountNames())
                    .Match(Succ: list => list, Fail: err => new List<string>());
                CoinB = ClearStringIfNoMatchWithList(CoinB, ListCoinB);
                break;
            case "Buy": //->ASBoxCoinA
                ListAccountFrom = (await _transactionService
                    .GetAccountNames(coin))
                    .Match(Succ: list => list, Fail: err => new List<string>());
                AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
                ListAccountTo = (await _transactionService
                    .GetAccountNames())
                    .Match(Succ: list => list, Fail: err => new List<string>());
                ListCoinB.Clear();
                var list = (await _transactionService
                    .GetCoinSymbolsExcludingUsdtUsdcFromLibrary())
                    .Match(Succ: list => list, Fail: err => new List<string>());
                ListCoinB.AddRange(list);
                CoinB = ClearStringIfNoMatchWithList(CoinB, ListCoinB); //string.empty

                break;
            case "Sell": //->ASBoxCoinA
                ListAccountFrom = (await _transactionService
                    .GetAccountNames(coin))
                    .Match(Succ: list => list, Fail: err => new List<string>());
                AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
                CoinB = ClearStringIfNoMatchWithList(CoinB, ListCoinB);
                ListAccountTo = (await _transactionService
                    .GetAccountNames())
                    .Match(Succ: list => list, Fail: err => new List<string>());
                break;
        }
    }
    #endregion ASBoxCoinA Events

    #region ASBoxAccountFrom Events
    private async void ASBoxAccountFrom_TextChanged(object sender, EventArgs e)
    {
        var _list = new List<string>();

        switch (transactionType.ToString())
        {
            case "Transfer": //->ASBoxAccountFrom
                _list = ListAccountFrom;
                if (AccountFrom == AccountTo)
                {
                    AccountTo = "";
                }
                ListAccountTo = (await _transactionService
                    .GetAccountNamesExcluding(AccountFrom))
                    .Match(Succ: list => list, Fail: err => new List<string>());
                break;
            case "Convert":
            case "Buy":
            case "Sell":
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
            AccountTo = args.SelectedItem.ToString() ?? string.Empty;
        }
        switch (transactionType.ToString())
        {
            case "Transfer":
                ListAccountTo = (await _transactionService
                    .GetAccountNamesExcluding(AccountFrom))
                    .Match(Succ: list => list, Fail: err => new List<string>());
                AccountTo = AccountTo is not null 
                    ? ClearStringIfNoMatchWithList(AccountTo, ListAccountTo) 
                    : string.Empty;
                break;
        }
    }
    private void LinkAccountsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        IsAccountsLinked = !IsAccountsLinked;
        if (IsAccountsLinked)
        {
            //TODO what should be the list 
            
            AccountTo = AccountFrom;
        }
        else
        {
            AccountTo = string.Empty;
        }
    }
    #endregion ASBoxAccountFrom Events

    #region TBoxQtyA
    private void QtyOrPrice_pressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is RegExTextBox tbox) { tbox.SelectAll(); }
    }
    #endregion TBoxQtyA

    #region TBoxPriceA Events
    private void TBoxPriceA_TextChanged(object sender, EventArgs e)
    {
        if (sender is RegExTextBox tbox && tbox.Text == string.Empty)
        {
            PriceA = ActualPriceA;
            OnPropertyChanged(nameof(PriceA));
        }
    }
    #endregion TBoxPriceA Events

    #region PrimaryButton events
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
    #endregion PrimaryButton events

    #region Dialog Events
    private void CloseButton_Cancel(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Validator.Stop();
    }
    #endregion Dialog Events

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _preferencesService.GetAppTheme())
        {
            sender.RequestedTheme = _preferencesService.GetAppTheme();
        }
    }

    private void EarningsCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        PriceA = 0;
    }

    private void TBoxQtyA_TextChanged(object sender, EventArgs e)
    {

    }

    private void TBoxQtyA_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tbox)
        {
            if (Convert.ToDouble(tbox.Text) > MaxQtyA  )
            {
                tbox.Text = MaxQtyA.ToString();
            }
        }
    }

    //public event PropertyChangedEventHandler? PropertyChanged;

    //protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    //{
    //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //}


}

