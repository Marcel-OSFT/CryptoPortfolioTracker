using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models;

public class TransactionDetailsBuilder
{
    private TransactionDetailsBuilder()
    {
        
    }

    public TransactionDetailsBuilder(TransactionDetails details)
    {
        _transactionType = details.TransactionType;
        _coinASymbol = details.CoinASymbol;
        _coinAName = details.CoinAName;
        _coinBSymbol = details.CoinBSymbol;
        _coinBName = details.CoinBName;
        _accountFrom = details.AccountFrom;
        _accountTo = details.AccountTo;
        _qtyA = details.QtyA;
        _qtyB = details.QtyB;
        _priceA = details.PriceA;
        _priceB = details.PriceB;
        _feeCoinSymbol = details.FeeCoinSymbol;
        _feeCoinName = details.FeeCoinName;
        _feeQty = details.FeeQty;
        _transactionDirection = details.TransactionDirection;
        _imageUriA = details.ImageUriA;
        _imageUriB = details.ImageUriB;
        _mmageUriFee = details.ImageUriFee;
        _valueA = details.ValueA;
        _valueB = details.ValueB;

    }

    private TransactionKind _transactionType;
    private string _coinASymbol  = string.Empty;
    private string _coinAName = string.Empty;
    private string _coinBSymbol = string.Empty;
    private string _coinBName = string.Empty;
    private string _accountFrom = string.Empty;
    private string _accountTo = string.Empty;
    private double _qtyA;
    private double _qtyB;
    private double _priceA;
    private double _priceB;
    private string _feeCoinSymbol = string.Empty;
    private string _feeCoinName = string.Empty;
    private double _feeQty;
    
    private MutationDirection _transactionDirection = MutationDirection.NotSet;
    private string _imageUriA = string.Empty;
    private string _imageUriB = string.Empty;
    private string _mmageUriFee = string.Empty;
    private double _valueA;
    private double _valueB;

    public static TransactionDetailsBuilder Create() => new TransactionDetailsBuilder();
    

    public TransactionDetailsBuilder OfTransactionType(TransactionKind transactionType)
    {
        _transactionType = transactionType;
        return this;
    }
    public TransactionDetailsBuilder FromCoinSymbol(string coinASymbol)
    {
        _coinASymbol = coinASymbol;
        return this;
    }
    public TransactionDetailsBuilder FromCoinName(string coinAName)
    {
        _coinAName = coinAName;
        return this;
    }
    public TransactionDetailsBuilder ToCoinSymbol(string coinBSymbol)
    {
        _coinBSymbol = coinBSymbol;
        return this;
    }
    public TransactionDetailsBuilder ToCoinName(string coinBName)
    {
        _coinBName = coinBName;
        return this;
    }
    public TransactionDetailsBuilder FromAccount(string accountFrom)
    {
        _accountFrom = accountFrom;
        return this;
    }
    public TransactionDetailsBuilder ToAccount(string accountTo)
    {
        _accountTo = accountTo;
        return this;
    }
    public TransactionDetailsBuilder FromQty(double qtyA)
    {
        _qtyA = qtyA;
        return this;
    }
    public TransactionDetailsBuilder ToQty(double qtyB)
    {
        _qtyB = qtyB;
        return this;
    }
    public TransactionDetailsBuilder FromPrice(double priceA)
    {
        _priceA = priceA;
        return this;
    }
    public TransactionDetailsBuilder ToPrice(double priceB)
    {
        _priceB = priceB;
        return this;
    }
    public TransactionDetailsBuilder FeeCoinSymbol(string feeCoinSymbol)
    {
        _feeCoinSymbol = feeCoinSymbol;
        return this;
    }
    public TransactionDetailsBuilder FeeCoinName(string feeCoinName)
    {
        _feeCoinName = feeCoinName;
        return this;
    }
    public TransactionDetailsBuilder FeeQty(double feeQty)
    {
        _feeQty = feeQty;
        return this;
    }
    public TransactionDetailsBuilder Direction(MutationDirection transactionDirection)
    {
        _transactionDirection = transactionDirection;
        return this;
    }
    public TransactionDetailsBuilder FromImage(string imageUriA)
    {
        _imageUriA = imageUriA;
        return this;
    }
    public TransactionDetailsBuilder ToImage(string imageUriB)
    {
        _imageUriB = imageUriB;
        return this;
    }
    public TransactionDetailsBuilder FeeImage(string imageUriFee)
    {
        _mmageUriFee = imageUriFee;
        return this;
    }
    public TransactionDetailsBuilder FromValue(double valueA)
    {
        _valueA = valueA;
        return this;
    }
    public TransactionDetailsBuilder ToValue(double valueB)
    {
        _valueB = valueB;
        return this;
    }
    public TransactionDetails Build()
    {
        return new TransactionDetails
        {
            TransactionType = _transactionType,
            CoinASymbol = _coinASymbol,
            CoinAName = _coinAName,
            CoinBSymbol = _coinBSymbol,
            CoinBName = _coinBName,
            AccountFrom = _accountFrom,
            AccountTo = _accountTo,
            QtyA = _qtyA,
            QtyB = _qtyB,
            PriceA = _priceA,
            PriceB = _priceB,
            FeeCoinSymbol = _feeCoinSymbol,
            FeeCoinName = _feeCoinName,
            FeeQty = _feeQty,
            TransactionDirection = _transactionDirection,
            ImageUriA = _imageUriA,
            ImageUriB = _imageUriB,
            ImageUriFee = _mmageUriFee,
            ValueA = _valueA,
            ValueB = _valueB
        };
    }

}
