using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;

using Microsoft.UI.Xaml.Controls;

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

//using CoinGecko.Clients;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
//using CoinGecko.Interfaces;
//using CoinGecko.Parameters;
using System.Diagnostics;
using Newtonsoft.Json;


using System.Net.Http;
using Microsoft.UI.Dispatching;
using Windows.Networking.Connectivity;
using System.Collections.ObjectModel;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Documents;
using Windows.Graphics.Imaging;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Windows.Storage.Streams;
using Windows.Storage;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Infrastructure;
using Windows.UI.Popups;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Views;
using CryptoPortfolioTracker.Services;
using Windows.Security.Cryptography.Core;
using SQLitePCL;
using CryptoPortfolioTracker.Enums;
using static System.Reflection.Metadata.BlobBuilder;
using System.Security.Principal;
using System.Collections;
using Microsoft.UI.Input;
using System.ComponentModel;
using System.Xml.Linq;
using Microsoft.UI.Xaml.Media.Animation;
using CommunityToolkit.WinUI.Converters;
using CommunityToolkit.Common;
using System.Reflection;
using System.Linq.Expressions;
using ABI.Microsoft.UI.Xaml;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NuGet.Frameworks;
using Microsoft.Extensions.DependencyInjection;
using LanguageExt.Common;
using LanguageExt;
using System.Globalization;
using CryptoPortfolioTracker.Extensions;
using Microsoft.Windows.ApplicationModel.Resources;

namespace CryptoPortfolioTracker.Dialogs;

public partial class TransactionDialog : ContentDialog, INotifyPropertyChanged, IDisposable
{
    #region fields
    private readonly DialogAction dialogAction;
    public Transaction transactionToEdit;
    public Transaction transactionNew;
    private readonly ITransactionService _transactionService;
    private TransactionKind transactionType;
    private readonly Brush defaultBrush;
    private readonly Brush notValidBrush = new SolidColorBrush(Colors.Red);
    private List<string> listCoinA;
    private List<string> listCoinB;
    private List<string> listAccountFrom;
    private List<string> listAccountTo;
    private List<string> listFeeCoin;
    private string coinA;
    private string coinB;
    private string accountFrom;
    private string accountTo;
    private bool isAccountsLinked;
    private bool isValidTest = false;
    private string transactionText;
    private string headerAccountFrom;
    private string headerAccountTo;
    private string headerCoinA;
    private string headerCoinB;
    private double maxQtyA;
    private double qtyA;
    private double qtyB;
    private double actualPriceA;
    private double priceA;
    private double priceB;
    private string feeCoin;
    private double feeQty;
    private string note;
    private DateTimeOffset timeStamp;
    private Validator validator;

    #endregion fields

    #region Public Properties

    public Exception Exception { get; private set; }

    public Validator Validator
    {
        get => validator;
        set
        {
            if (value != validator)
            {
                validator = value;
                OnPropertyChanged(nameof(Validator));
            }
        }
    }
    public bool IsValidTest
    {
        get => isValidTest;
        set
        {
            if (value != isValidTest)
            {
                isValidTest = value;
                OnPropertyChanged(nameof(IsValidTest));
            }
        }
    }
    public List<string> ListCoinA
    {
        get => listCoinA;
        set
        {
            if (value != listCoinA)
            {
                listCoinA = value;
                OnPropertyChanged(nameof(ListCoinA));
            }
        }
    }
    public List<string> ListCoinB
    {
        get => listCoinB;
        set
        {
            if (value != listCoinB)
            {
                listCoinB = value;
                OnPropertyChanged(nameof(ListCoinB));
            }
        }
    }
    public List<string> ListAccountFrom
    {
        get => listAccountFrom;
        set
        {
            if (value != listAccountFrom)
            {
                listAccountFrom = value;
                OnPropertyChanged(nameof(ListAccountFrom));
            }
        }
    }
    public List<string> ListAccountTo
    {
        get => listAccountTo;
        set
        {
            if (value != listAccountTo)
            {
                listAccountTo = value;
                OnPropertyChanged(nameof(ListAccountTo));
            }
        }
    }
    public List<string> ListFeeCoin
    {
        get => listFeeCoin;
        set
        {
            if (value != listFeeCoin)
            {
                listFeeCoin = value;
                PresetFeeCoinToETH();
                OnPropertyChanged(nameof(ListFeeCoin));
            }
        }
    }
    public string CoinA
    {
        get => coinA;
        set
        {
            if (value != coinA)
            {
                coinA = value;
                if (AccountFrom != null && AccountFrom != "")
                {
                    GetMaxQtyAndPrice();
                }
                OnPropertyChanged(nameof(CoinA));
            }
        }
    }
    public string CoinB
    {
        get => coinB;
        set
        {
            if (value != coinB)
            {
                coinB = value;
                OnPropertyChanged(nameof(CoinB));
            }
        }
    }
    public string FeeCoin
    {
        get => feeCoin;
        set
        {
            if (value != feeCoin)
            {
                feeCoin = value;
                OnPropertyChanged(nameof(FeeCoin));
            }
        }
    }
    public string AccountFrom
    {
        get => accountFrom;
        set
        {
            if (value != accountFrom)
            {
                accountFrom = value;
                if (CoinA != null && CoinA != "")
                {
                    GetMaxQtyAndPrice();
                }
                OnPropertyChanged(nameof(AccountFrom));
            }
        }
    }
    public string AccountTo
    {
        get => accountTo;
        set
        {
            if (value != accountTo)
            {
                accountTo = value;
                OnPropertyChanged(nameof(AccountTo));
            }
        }
    }
    public bool IsAccountsLinked
    {
        get => isAccountsLinked;
        set
        {
            if (value != isAccountsLinked)
            {
                isAccountsLinked = value;
                OnPropertyChanged(nameof(IsAccountsLinked));
            }
        }
    }
    public string TransactionText
    {
        get => transactionText;
        set
        {
            if (value != transactionText)
            {
                transactionText = value;
                OnPropertyChanged(nameof(TransactionText));
            }
        }
    }
    public string HeaderCoinA
    {
        get => headerCoinA;
        set
        {
            if (value != headerCoinA)
            {
                headerCoinA = value;
                OnPropertyChanged(nameof(HeaderCoinA));
            }
        }
    }
    public string HeaderCoinB
    {
        get => headerCoinB;
        set
        {
            if (value != headerCoinB)
            {
                headerCoinB = value;
                OnPropertyChanged(nameof(HeaderCoinB));
            }
        }
    }
    public string HeaderAccountFrom
    {
        get => headerAccountFrom;
        set
        {
            if (value != headerAccountFrom)
            {
                headerAccountFrom = value;
                OnPropertyChanged(nameof(HeaderAccountFrom));
            }
        }
    }
    public string HeaderAccountTo
    {
        get => headerAccountTo;
        set
        {
            if (value != headerAccountTo)
            {
                headerAccountTo = value;
                OnPropertyChanged(nameof(HeaderAccountTo));
            }
        }
    }
    public double QtyA
    {
        get => qtyA;
        set
        {
            if (value != qtyA)
            {
                if (maxQtyA >= 0 && value > MaxQtyA) value = MaxQtyA;
                qtyA = value;
                CalculatePriceB();
                OnPropertyChanged(nameof(QtyA));
            }
        }
    }
    public double QtyB
    {
        get => qtyB;
        set
        {
            if (value != qtyB)
            {
                qtyB = value;
                CalculatePriceB();
                OnPropertyChanged(nameof(QtyB));
            }
        }
    }
    public double MaxQtyA
    {
        get => maxQtyA;
        set
        {
            if (value != maxQtyA)
            {
                maxQtyA = value;
                OnPropertyChanged(nameof(MaxQtyA));
            }
        }
    }
    public double ActualPriceA
    {
        get => actualPriceA;
        set
        {
            if (value != actualPriceA)
            {
                actualPriceA = value;
                OnPropertyChanged(nameof(ActualPriceA));
            }
        }
    }
    public double PriceA
    {
        get => priceA;
        set
        {
            if (value != priceA)
            {
                priceA = value;
                CalculatePriceB();
                OnPropertyChanged(nameof(PriceA));
            }
        }
    }
    public double PriceB
    {
        get => priceB;
        set
        {
            if (value != priceB)
            {
                priceB = value;
                OnPropertyChanged(nameof(PriceB));
            }
        }
    }
    public double FeeQty
    {
        get => feeQty;
        set
        {
            if (value != feeQty)
            {
                feeQty = value;
                OnPropertyChanged(nameof(FeeQty));
            }
        }
    }
    public string Note
    {
        get => note;
        set
        {
            if (value != note)
            {
                note = value;
                OnPropertyChanged(nameof(Note));
            }
        }
    }
    public DateTimeOffset TimeStamp
    {
        get => DateTimeOffset.Parse(timeStamp.ToString(ci));
        set
        {
            if (value != timeStamp)
            {
                timeStamp = DateTimeOffset.Parse(value.ToString(ci)); ;
                OnPropertyChanged(nameof(TimeStamp));
            }
        }
    }

    private readonly CultureInfo ci = new(App.userPreferences.CultureLanguage);
    private readonly DispatcherQueue dispatcherQueue;

    private readonly ResourceLoader rl;


    #endregion Public Properties

    #region Constructor(s)

    public TransactionDialog(ITransactionService transactionService, DialogAction _dialogAction, Transaction transaction = null)
    {
        this.InitializeComponent();
        this.dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        dialogAction = _dialogAction;
        _transactionService = transactionService;
        transactionToEdit = transaction;
        InitializeAllFields() ;
        defaultBrush = ASBoxCoinA.Background;
        TimeStamp = DateTimeOffset.Parse(DateTime.Now.ToString(ci));
        Validator = new Validator(10, true);
        rl = new ResourceLoader();
        SetDialogButtonsAndTitle(dialogAction);
    }

    private void SetDialogButtonsAndTitle(DialogAction dialogAction)
    {
        var index = -1;
        if (dialogAction == DialogAction.Edit)
        {
           
            for (int i = 0; i < TransactionTypeRadioButtons.Items.Count; i++)
            {
                if ((TransactionTypeRadioButtons.Items[i] as RadioButton).Content.ToString() == transactionToEdit.Details.TransactionType.AsDisplayString())
                {
                    index = i;
                    break;
                }
            }
            TransactionTypeRadioButtons.SelectedIndex = index;
            Title = rl.GetString("TransactionDialog_Title_Edit");
        }
        else
        {
            TransactionTypeRadioButtons.SelectedIndex = 0;
            Title = rl.GetString("TransactionDialog_Title_Add");
        }
        PrimaryButtonText = rl.GetString("TransactionDialog_PrimaryButton");
        CloseButtonText = rl.GetString("TransactionDialog_CloseButton");
    }
    #endregion Constructors

    #region Methods  
    public void Dispose()
    {
        Debug.WriteLine("Dialog Disposed");
    }
    private async void GetMaxQtyAndPrice()
    {
        //in case of a 'Deposit' the MaxQtyA doesn't need to be set....
        if (transactionType != TransactionKind.Deposit)
        {
            (await _transactionService.GetMaxQtyAndPrice(CoinA, AccountFrom)).IfSucc(result =>
            {
                MaxQtyA = result[0];
                ActualPriceA = PriceA = result[1];
            });
        }
        else 
        {
            MaxQtyA = -1;
            ActualPriceA = PriceA = (await _transactionService.GetPriceFromLibrary(CoinA)).Match(Succ: price => price , Fail: err => 0);
        }           
    }

    private async Task<Transaction> WrapUpTransactionData()
    {
        Transaction transactionToAdd = new Transaction();
        List<Mutation> newMutations = new List<Mutation>();

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
        var transactionDetails = new TransactionDetails
        {
            AccountFrom = AccountFrom,
            AccountTo = AccountTo,
            FeeQty = FeeQty,
            CoinA = CoinA,
            CoinB = CoinB,
            PriceA = PriceA,
            PriceB = PriceB,
            FeeCoin = FeeCoin,
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
    private async Task<Result<Mutation>> GetMutation(TransactionKind type, MutationDirection direction, string symbol, double qty, double price, string account)
    {
        Mutation mutation = new Mutation();
        try
        {
            // mutation.Asset.Coin.Symbol = symbol;
            mutation.Asset.Coin = (await _transactionService.GetCoinBySymbol(symbol))
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
        PriceB = (QtyA * PriceA) / QtyB;
    }
    private void PresetFeeCoinToETH()
    {
        if (listFeeCoin != null && listFeeCoin.Count > 0)
        {               
            var result = listFeeCoin.Where(x => x.ToString().ToLower() == "eth").FirstOrDefault();
            if (result != null) 
            {
                FeeCoin= (string)result;
            }  
            else { FeeCoin = ""; }
        }
    }
    private void ConfigureDialogFields(string explainType, string assetTextA, string assetTextB, string accountTextA, string accountTextB, bool isAccountLinkEnabled = true)
    {
        TransactionText = explainType;
        HeaderAccountFrom = accountTextA;
        HeaderAccountTo = accountTextB;
        HeaderCoinA = assetTextA;
        HeaderCoinB = assetTextB;

        IsAccountsLinked = isAccountLinkEnabled;

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
            else LinkAccountsButton.Visibility = Visibility.Collapsed;

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
    private string SetStringIfListContainsOneItem(string _string, List<string> list)
    {
        //if (list.Count == 1)
        //{
        //    _string = list.First().ToString();
        //}
        return _string;
    }
    private void InitializeAllFields()
    {
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
            OnPropertyChanged(nameof(ListCoinA));
        }
        else ListCoinA = new List<string>();
        if (ListCoinB != null)
        {
            ListCoinB.Clear();
            OnPropertyChanged(nameof(ListCoinB));
        }
        else ListCoinB = new List<string>();
        if (ListAccountFrom != null)
        {
            ListAccountFrom.Clear();
            OnPropertyChanged(nameof(ListAccountFrom));
        }
        else ListAccountFrom = new List<string>();
        if (ListAccountTo != null)
        {
            ListAccountTo.Clear();
            OnPropertyChanged(nameof(ListAccountTo));
        }
        else ListAccountTo = new List<string>();

        if (ListFeeCoin != null)
        {
            ListFeeCoin.Clear();
            OnPropertyChanged(nameof(ListFeeCoin));
        }
        else ListFeeCoin = new List<string>();

        
    }
    #endregion Methods

    #region TransactionType Events
    private async void TransactionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //if (dialogAction == DialogAction.Edit) TransactionTypeRadioButtons.SelectedItem = transactionToEdit.TransactionType.ToString();
        InitializeAllFields();
        var rl = new ResourceLoader();
        string CoinFromHeader = rl.GetString("TransactionDialog_CoinFromHeader");
        string CoinToHeader = rl.GetString("TransactionDialog_CoinToHeader");
        string CoinHeader = rl.GetString("TransactionDialog_CoinHeader");
        string AccountFromHeader = rl.GetString("TransactionDialog_AccountFromHeader");
        string AccountToHeader = rl.GetString("TransactionDialog_AccountToHeader");

        if (sender is RadioButtons rb)
        {
            
            switch (rb.SelectedIndex)
            {
                case 0: //Deposit                       
                    Validator.NrOfValidEntriesNeeded = 5;
                    transactionType = TransactionKind.Deposit;
                    ConfigureDialogFields(rl.GetString("TransactionDialog_Deposit_Explainer"), CoinHeader, "", AccountToHeader, "");
                    ListCoinA = (await _transactionService.GetCoinSymbolsFromLibrary()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountFrom = (await _transactionService.GetAccountNames()).Match(Succ: list => list, Fail: err => new List<string>()); 
                    ListCoinB = new List<string>();
                    ListAccountTo = new List<string>();
                    ListFeeCoin = new List<string>();
                    break;
                case 1: //Withdraw
                    Validator.NrOfValidEntriesNeeded = 5;
                    transactionType = TransactionKind.Withdraw; 
                    ConfigureDialogFields(rl.GetString("TransactionDialog_Withdraw_Explainer"), CoinHeader, "", AccountFromHeader, "");
                    ListCoinA = (await _transactionService.GetCoinSymbolsFromAssets()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListCoinB = new List<string>();
                    ListAccountFrom = (await _transactionService.GetAccountNames()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountTo = new List<string>();
                    ListFeeCoin = new List<string>();
                    break;
                case 2: //Transfer
                    Validator.NrOfValidEntriesNeeded = 8;
                    transactionType = TransactionKind.Transfer;
                    ConfigureDialogFields(rl.GetString("TransactionDialog_Transfer_Explainer"), CoinHeader, "", AccountFromHeader, AccountToHeader, false);
                    ListCoinA = (await _transactionService.GetCoinSymbolsFromAssets()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListFeeCoin = ListCoinA;
                    ListCoinB = new List<string>();
                    ListAccountFrom = (await _transactionService.GetAccountNames()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountTo = (await _transactionService.GetAccountNames()).Match(Succ: list => list, Fail: err => new List<string>());
                    //ListAccountTo = new List<string>();
                    break;
                case 3: //Convert
                    Validator.NrOfValidEntriesNeeded = 10;
                    transactionType = TransactionKind.Convert;
                    ConfigureDialogFields(rl.GetString("TransactionDialog_Convert_Explainer"), CoinFromHeader, CoinToHeader, AccountFromHeader, AccountToHeader);
                    ListCoinA = (await _transactionService.GetCoinSymbolsFromAssets()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListCoinB = (await _transactionService.GetCoinSymbolsFromLibrary()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountFrom = (await _transactionService.GetAccountNames()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountTo = (await _transactionService.GetAccountNames()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListFeeCoin = ListCoinA;
                    break;
                case 4: //Buy
                    Validator.NrOfValidEntriesNeeded = 10;
                    transactionType = TransactionKind.Buy;
                    ConfigureDialogFields(rl.GetString("TransactionDialog_Buy_Explainer"), CoinFromHeader, CoinToHeader, AccountFromHeader, AccountToHeader);
                    ListCoinA = (await _transactionService.GetUsdtUsdcSymbolsFromAssets()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListCoinB = (await _transactionService.GetCoinSymbolsExcludingUsdtUsdcFromLibrary()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountFrom = (await _transactionService.GetAccountNames()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountTo = (await _transactionService.GetAccountNames()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListFeeCoin = (await _transactionService.GetCoinSymbolsFromAssets()).Match(Succ: list => list, Fail: err => new List<string>());
                    break;
                case 5: //Sell
                    Validator.NrOfValidEntriesNeeded = 10;
                    transactionType = TransactionKind.Sell;
                    ConfigureDialogFields(rl.GetString("TransactionDialog_Sell_Explainer"), CoinFromHeader, CoinToHeader, AccountFromHeader, AccountToHeader);
                    ListCoinA = (await _transactionService.GetCoinSymbolsFromAssets()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListCoinB = (await _transactionService.GetUsdtUsdcSymbolsFromLibrary()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountFrom = (await _transactionService.GetAccountNames()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListAccountTo = (await _transactionService.GetAccountNames()).Match(Succ: list => list, Fail: err => new List<string>());
                    ListFeeCoin = ListCoinA;
                    
                    break;
            }
            if (dialogAction == DialogAction.Add)
            {
                TimeStamp = DateTimeOffset.Parse(DateTime.Now.ToString(ci));
            }
            else if (dialogAction == DialogAction.Edit)
            {
                CoinA = transactionToEdit.Details.CoinA;
                CoinB = transactionToEdit.Details.CoinB;
                AccountFrom = transactionToEdit.Details.AccountFrom;
                AccountTo = transactionToEdit.Details.AccountTo;
                QtyA = transactionToEdit.Details.QtyA;
                QtyB = transactionToEdit.Details.QtyB;
                PriceA = transactionToEdit.Details.PriceA;
                PriceB = transactionToEdit.Details.PriceB;
                FeeCoin = transactionToEdit.Details.FeeCoin;
                FeeQty = transactionToEdit.Details.FeeQty;
                Note = transactionToEdit.Note;
                TimeStamp = DateTimeOffset.Parse(transactionToEdit.TimeStamp.ToString(ci));
                TransactionTypeRadioButtons.IsEnabled = false;
                ASBoxCoinA.IsEnabled = false;
                ASBoxCoinB.IsEnabled = false;
                ASBoxAccountFrom.IsEnabled = false;
                ASBoxAccountTo.IsEnabled = false;
            }
        }
    }
    #endregion TransactionType Events

    #region ASBoxCoinA Events       
    private void ASBoxCoinA_TextChanged(object sender, EventArgs e)
    {
        Debug.WriteLine("Raised - TextChanged");
    }
    private async void ASBoxCoinA_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        CoinA = args.SelectedItem.ToString();
        switch (transactionType.ToString())
        {
            case "Deposit": //->ASBoxCoinA
                //al is allready set at selecting RB -> 'Deposit'
                break;
            case "Withdraw": //->ASBoxCoinA
                ListAccountFrom = (await _transactionService.GetAccountNames(CoinA)).Match(Succ: list => list, Fail: err => new List<string>());
                AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
                AccountFrom = SetStringIfListContainsOneItem(AccountFrom, ListAccountFrom);
                break;
            case "Transfer": //->ASBoxCoinA
                ListAccountFrom = (await _transactionService.GetAccountNames(CoinA)).Match(Succ: list => list, Fail: err => new List<string>());
                AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
                AccountFrom = SetStringIfListContainsOneItem(AccountFrom, ListAccountFrom);
                break;
            case "Convert": //->ASBoxCoinA
                ListAccountFrom = (await _transactionService.GetAccountNames(CoinA)).Match(Succ: list => list, Fail: err => new List<string>());
                AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
                AccountFrom = SetStringIfListContainsOneItem(AccountFrom, ListAccountFrom);
                ListCoinB = (await _transactionService.GetCoinSymbolsFromLibraryExcluding(args.SelectedItem.ToString())).Match(Succ: list => list, Fail: err => new List<string>());
                CoinB = ClearStringIfNoMatchWithList(CoinB, ListCoinB);
                CoinB = SetStringIfListContainsOneItem(CoinB, ListCoinB);
                break;
            case "Buy": //->ASBoxCoinA
                ListAccountFrom = (await _transactionService.GetAccountNames(CoinA)).Match(Succ: list => list, Fail: err => new List<string>());
                AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
                AccountFrom = SetStringIfListContainsOneItem(AccountFrom, ListAccountFrom);
                break;
            case "Sell": //->ASBoxCoinA
                ListAccountFrom = (await _transactionService.GetAccountNames(CoinA)).Match(Succ: list => list, Fail: err => new List<string>());
                AccountFrom = ClearStringIfNoMatchWithList(AccountFrom, ListAccountFrom);
                AccountFrom = SetStringIfListContainsOneItem(AccountFrom, ListAccountFrom);
                break;
        }
    }
    #endregion ASBoxCoinA Events

    #region ASBoxCoinB Events
    private void ASBoxCoinB_TextChanged(object sender, EventArgs e)
    {
        Debug.WriteLine("Raised - TextChanged");
    }
    private void ASBoxCoinB_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs e)
    {
        Debug.WriteLine("Raised - SuggestionChosen");
    }
    #endregion ASBoxCoinB Events

    #region ASBoxAccountFrom Events
    private async void ASBoxAccountFrom_TextChanged(object sender, EventArgs e)
    {
        List<string> _list = new List<string>();
        //string _account = "";

        switch (transactionType.ToString())
        {
            case "Transfer": //->ASBoxAccountFrom
                _list = ListAccountFrom;
                if (AccountFrom == AccountTo)
                {
                    AccountTo = "";
                    ListAccountTo = (await _transactionService.GetAccountNamesExcluding(AccountFrom)).Match(Succ: list => list, Fail: err => new List<string>());
                }
                break;
            //default:
                //if (IsAccountsLinked && ASBoxAccountFrom.Items.Contains(_account))
                //{
                //    AccountTo = AccountFrom;
                //}
                //break;
        }
    }
    private async void ASBoxAccountFrom_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        AccountFrom = args.SelectedItem.ToString();
        if (IsAccountsLinked)
        {
            AccountTo = args.SelectedItem.ToString();
        }
        switch (transactionType.ToString())
        {
            case "Transfer":
                ListAccountTo = (await _transactionService.GetAccountNamesExcluding(AccountFrom)).Match(Succ: list => list, Fail: err => new List<string>());
                AccountTo = ClearStringIfNoMatchWithList(AccountTo, ListAccountTo);
                AccountTo = SetStringIfListContainsOneItem(AccountTo, ListAccountTo);
                break;
        }
    }
    private void LinkAccountsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        IsAccountsLinked = !IsAccountsLinked;
        if (IsAccountsLinked)
        {
            AccountTo = AccountFrom;
        }
    }
    #endregion ASBoxAccountFrom Events

    #region ASBoxFeeCoin Events       
    private void ASBoxFeeCoin_TextChanged(object sender, EventArgs e)
    {
        Debug.WriteLine("Raised - TextChanged");
    }
    private void ASBoxFeeCoin_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs e)
    {
        Debug.WriteLine("Raised - SuggestionChosen");
    }
    #endregion ASBoxFeeCoin Events

    #region TBoxQtyA
    private void QtyOrPrice_pressed(object sender, PointerRoutedEventArgs e)
    {
        (sender as RegExTextBox).SelectAll();
    }
    #endregion TBoxQtyA

    #region TBoxPriceA Events
    private void TBoxPriceA_TextChanged(object sender, EventArgs e)
    {
        if ((sender as RegExTextBox).Text == "")
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
            transactionNew = null;
            Exception = ex;
        }
    }
    #endregion PrimaryButton events

    #region Event Handlers
    //******* EventHandlers
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name)
    {
        //if (MainPage.Current == null) return;
        //MainPage.Current.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        
        
        this.dispatcherQueue.TryEnqueue(() =>
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        },
        exception =>
        {
            throw new Exception(exception.Message);
        });
    }
    #endregion Event Handlers

    #region Dialog Events
    private void CloseButton_Cancel(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Validator.Stop();
    }
    #endregion Dialog Events

    
}

