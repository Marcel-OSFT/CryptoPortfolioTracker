

using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;
//using PollyDemos.OutputHelpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public interface ILibraryService
    {
        public Task<Result<bool>> CreateCoin(Models.Coin newCoin);
        public Task<Result<Coin>> GetCoin(string coinId);
        public Task<Result<List<Coin>>> GetCoinsOrderedByRank();
        public Task<Result<CoinFullDataById>> GetCoinDetails(string coinId);
        public Task<Result<bool>> RemoveCoin(Coin coin);
        public Task<Result<List<CoinList>>> GetCoinListFromGecko();
        public Task<Result<bool>> UpdateNote(Coin coin, string note);
    }
}
