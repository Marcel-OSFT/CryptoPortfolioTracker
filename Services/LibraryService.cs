using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CoinGeckoClient = CoinGeckoFluentApi.Client.CoinGeckoClient;
//using PollyDemos.OutputHelpers;
//using CoinGecko.Interfaces;
using Exception = System.Exception;

namespace CryptoPortfolioTracker.Services
{
    public class LibraryService : ILibraryService
    {
        private readonly PortfolioContext context;

        public LibraryService(PortfolioContext portfolioContext)
        {
            context = portfolioContext;
        }

        public async Task<Result<bool>> CreateCoin(Coin newCoin)
        {
            bool _result = false;

            if (newCoin == null) { return _result; }
            try
            {
                context.Coins.Add(newCoin);
                _result = await context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                return new Result<bool>(ex);
            }
            return _result;
        }

        public async Task<Result<Coin>> GetCoin(string coinId)
        {
            Coin coin = null;
            if (coinId == null || coinId == "") { return new Coin(); }
            try
            {
                coin = await context.Coins.Where(x => x.ApiId.ToLower() == coinId.ToLower()).SingleAsync();
            }
            catch (Exception ex)
            {
                return new Result<Coin>(ex);
            }
            return coin;
        }

        public async Task<Result<List<Coin>>> GetCoinsOrderedByRank()
        {
            List<Coin> coinList = null;
            try
            {
                coinList = await context.Coins.OrderBy(x => x.Rank).ToListAsync();
            }
            catch (Exception ex)
            {
                return new Result<List<Coin>>(ex);
            }
            return coinList != null ? coinList : new List<Coin>();
        }

        public async Task<Result<bool>> RemoveCoin(string coinId)
        {
            bool _result;
            if (coinId == null || coinId == "") { return false; }
            try
            {
                var coin = await context.Coins.Where(x => x.ApiId == coinId).SingleAsync();
                context.Coins.Remove(coin);
                _result = await context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                return new Result<bool>(ex);
            }
            return _result;
        }

        public async Task<Result<List<CoinList>>> GetCoinListFromGecko()
        {
            int Retries = 0;

            CancellationTokenSource tokenSource2 = new CancellationTokenSource();
            CancellationToken cancellationToken = tokenSource2.Token;

            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 5,
                Delay = System.TimeSpan.FromSeconds(30), // Wait between each try
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception!;
                    Retries++;
                    return default;
                }
            }).Build();

            List<CoinList> coinList = null;

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

            CoinGeckoClient coinsClient = new CoinGeckoClient(httpClient, App.CoinGeckoApiKey, App.ApiPath, serializerSettings);
            //bool isValidResult;

            Exception error = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await strategy.ExecuteAsync(async token =>
                    {
                        coinList = await coinsClient.Coins.List.GetAsync<List<CoinList>>();

                    }, cancellationToken);
                }
                catch (System.Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    tokenSource2.Cancel();
                    tokenSource2.Dispose();
                }
            }
            if (error != null) return new Result<List<CoinList>>(error);
            return coinList;
        }

        public async Task<Result<CoinFullDataById>> GetCoinDetails(string coinId)
        {
            int Retries = 0;
            if (coinId == null || coinId == "") { return new Result<CoinFullDataById>(); }

            CancellationTokenSource tokenSource2 = new CancellationTokenSource();
            CancellationToken cancellationToken = tokenSource2.Token;

            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 5,
                Delay = System.TimeSpan.FromSeconds(30), // Wait between each try
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception!;
                    Retries++;
                    if (Retries > 0)
                    {
                        if (AddCoinDialog.Current != null) AddCoinDialog.Current.ShowBePatienceNotice();
                    }
                    return default;
                }
            }).Build();

            CoinFullDataById coinFullDataById = null;

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

            CoinGeckoClient coinsClient = new CoinGeckoClient(httpClient, App.CoinGeckoApiKey, App.ApiPath, serializerSettings);
            //bool isValidResult;

            Exception error = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {

                    await strategy.ExecuteAsync(async token =>
                    {
                        coinFullDataById = await coinsClient.Coins[coinId].GetAsync<CoinFullDataById>();
                    }, cancellationToken);
                }
                catch (System.Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    tokenSource2.Cancel();
                    tokenSource2.Dispose();
                }
            }
            //CoinLibraryViewModel.Current.dialog.bePatientText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

            if (error != null) return new Result<CoinFullDataById>(error);
            return coinFullDataById;
        }

        public async Task<Result<bool>> UpdateNote(Coin coin, string note)
        {
            bool result;
            if (coin.Note == note) { return false; }
            try
            {
                coin.Note = note;
                context.Coins.Update(coin);
                result = await context.SaveChangesAsync() < 1;
            }
            catch (Exception ex)
            {
                RejectChanges();
                return new Result<bool>(ex);
            }
            return result;
        }

        public void RejectChanges()
        {
            foreach (var entry in context.ChangeTracker.Entries())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified; //Revert changes made to deleted entity.
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                }
            }
        }


    }


}
