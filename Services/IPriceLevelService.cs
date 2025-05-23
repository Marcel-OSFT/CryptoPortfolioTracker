using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Migrations;
using CryptoPortfolioTracker.Models;
using LanguageExt;
using LanguageExt.Common;
using LiveChartsCore.Defaults;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services;

public interface IPriceLevelService
{
    ObservableCollection<Coin> ListCoins { get; set; }

    public void SortList(SortingOrder sortingOrder, Func<Coin, object> sortFunc);


    public bool IsCoinsListEmpty();


    //public Task<ObservableCollection<Coin>> PopulateCoinsList();

    //public Task<ObservableCollection<Coin>> PopulateCoinsList(SortingOrder sortingOrder, Func<Coin, object> sortFunc);
    public Task PopulateCoinsList(SortingOrder sortingOrder, Func<Coin, object> sortFunc);


    public Task<Result<List<Coin>>> GetCoinsFromContext();

    public Task<Either<Error,Coin>> ResetPriceLevels(Coin coin);

    public Task<Either<Error,Coin>> UpdatePriceLevels(Coin coin, ICollection<PriceLevel> priceLevels);
    Task<ObservableCollection<HeatMapPoint>> GetHeatMapPoints(int selectedHeatMapIndex);
    void UpdateHeatMap();
    void SortListString(SortingOrder sortingOrder, Func<Coin, object> sortFunc);
    Portfolio GetPortfolio();
    Task UpdateCoinsList(Coin coin);
    Task UpdateCoinsList(Coin coin, Coin updatedCoin);
    void ClearCoinsList();
    bool ListCoinsHasAny();
    Task PopulateCoinsList();
}
