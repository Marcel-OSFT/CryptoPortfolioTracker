using CryptoPortfolioTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public static class CoinMockService
    {
        public static readonly Coin MockCoin1 = new()
        {
            ApiId = "bitcoin",
            Name = "Bitcoin",
            Symbol = "BTC",
            ImageUri = "https://assets.coingecko.com/coins/images/1/small/bitcoin.png?1696501400",
            About = "This is about Bitcoin....",
            Ath = 65000,
            Change1Month = 1,
            Change24Hr = 2,
            Change52Week = 3,
            IsAsset = false,
            MarketCap = 10009,
            Price = 41002,
            Rank = 1
        };
        public static readonly Coin MockCoin2 = new()
        {
            ApiId = "ethereum",
            Name = "Ethereum",
            Symbol = "ETH",
            ImageUri = "https://assets.coingecko.com/coins/images/279/small/ethereum.png?1696501628",
            About = "This is about Ethereum....",
            Ath = 4050,
            Change1Month = 1,
            Change24Hr = 2,
            Change52Week = 3,
            IsAsset = false,
            MarketCap = 20009,
            Price = 2300,
            Rank = 2
        };



    }
}
