using CryptoPortfolioTracker.Models;

namespace CryptoPortfolioTracker.Services
{
    public class AccountMockService
    {
        public Account MockAccount1 = new Account();
        public Account MockAccount2 = new Account();
        public AccountMockService()
        {
            MockAccount1.Name = "Ledger Nano X";
            MockAccount1.About = "Dit is mijn ledger";

            MockAccount2.Name = "Bybit UTA";
            MockAccount2.About = "Dit is mijn Bybit account";

        }

    }
}

