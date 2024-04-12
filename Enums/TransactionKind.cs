using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Data;
using System.Text;

namespace CryptoPortfolioTracker.Enums;

public enum TransactionKind
{
    Deposit = 0,
    Withdraw = 1,
    Buy = 2,
    Sell = 3,
    Convert = 4,
    Transfer = 5,
    Fee = 6
}

public static class TransactionKindExt
{
    public static string AsDisplayString(this TransactionKind transactionKind)
    {
        ResourceLoader rl = new();
        switch (transactionKind)
        {
            case TransactionKind.Deposit: return rl.GetString("TransactionType_Deposit/Content"); ;
            case TransactionKind.Withdraw: return rl.GetString("TransactionType_Withdraw/Content"); ;
            case TransactionKind.Transfer: return rl.GetString("TransactionType_Transfer/Content"); ;
            case TransactionKind.Convert: return rl.GetString("TransactionType_Convert/Content"); ;
            case TransactionKind.Buy: return rl.GetString("TransactionType_Buy/Content"); ;
            case TransactionKind.Sell: return rl.GetString("TransactionType_Sell/Content"); ;

            default: throw new ArgumentOutOfRangeException("role");
        }
    }
}