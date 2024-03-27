

using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using PollyDemos.OutputHelpers;
using Polly.Retry;
using Polly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using Microsoft.UI;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services
{
    public interface ILibraryService
    {
        public Task<Result<bool>> CreateCoin(Models.Coin newCoin);
        public Task<Result<Coin>> GetCoin(string coinId);
        public Task<Result<List<Coin>>> GetCoinsOrderedByRank();
        public Task<Result<CoinFullDataById>> GetCoinDetails(string coinId);
        public Task<Result<bool>> RemoveCoin(string coinId);
        public Task<Result<List<CoinList>>> GetCoinListFromGecko();
        public Task<Result<bool>> UpdateNote(Coin coin, string note);
    }
}
