﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services;

public interface ILibraryService
{
    ObservableCollection<Coin> ListCoins { get; set; }

    public Task<Result<bool>> CreateCoin(Coin? newCoin);
    public Task<Result<bool>> MergeCoin(Coin prelistingCoin, Coin? newCoin);

    public Task<Result<Coin>> GetCoin(string coinId);
    public Task<Result<List<Coin>>> GetCoinsFromContext();
    public Task<Result<CoinFullDataById>> GetCoinDetails(string coinId);
    public Task<Result<bool>> RemoveCoin(Coin coin);
    public Task<Result<List<CoinList>>> GetCoinListFromGecko();
    public Task<Result<bool>> UpdateNote(Coin coin, string note);

    public bool IsNotAsset(Coin coin);

    
    /// <summary>
    /// this function without parameters will sort the list using the last used settings
    /// </summary>
    /// <param name="sortingOrder">ascending or descending</param>
    /// <param name="sortFunc">function describing where to sort on</param>
    /// <returns>
    /// Nothing
    /// </returns>
    void SortList(SortingOrder sortingOrder, Func<Coin, object> sortFunc);
    
    
    /// <summary>
    /// This Task will populate the ListCoins property of the LibraryService by 
    /// retrieving Coin items from the context/DB 
    /// </summary>
    /// <returns> 
    /// an ObservableCollection of type 'Coin'
    /// </returns>
    bool IsCoinsListEmpty();
    void ClearCoinsList();
    Task RemoveFromCoinsList(Coin coin);
    Task AddToCoinsList(Coin coin);
    Task UpdateCoinsList(Coin coin);
    Task<ObservableCollection<Coin>> PopulateCoinsList(SortingOrder sortingOrder, Func<Coin, object> sortFunc);
    Portfolio? GetPortfolio();
}
