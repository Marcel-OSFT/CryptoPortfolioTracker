using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Data;
using System.Text;
using WinUI3Localizer;

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
        ILocalizer loc = Localizer.Get();


        switch (transactionKind)
        {
            case TransactionKind.Deposit: return loc.GetLocalizedString("TransactionKind_Deposit"); 
            case TransactionKind.Withdraw: return loc.GetLocalizedString("TransactionKind_Withdraw"); 
            case TransactionKind.Transfer: return loc.GetLocalizedString("TransactionKind_Transfer"); 
            case TransactionKind.Convert: return loc.GetLocalizedString("TransactionKind_Convert"); 
            case TransactionKind.Buy: return loc.GetLocalizedString("TransactionKind_Buy"); 
            case TransactionKind.Sell: return loc.GetLocalizedString("TransactionKind_Sell");

        }
        return string.Empty;
    }
}