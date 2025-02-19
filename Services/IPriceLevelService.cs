﻿using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Migrations;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;
using LiveChartsCore.Defaults;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services;

public interface IPriceLevelService
{
    public void SortList(SortingOrder sortingOrder, Func<Coin, object> sortFunc);


    public bool IsCoinsListEmpty();


    public Task<ObservableCollection<Coin>> PopulateCoinsList();

    public Task<ObservableCollection<Coin>> PopulateCoinsList(SortingOrder sortingOrder, Func<Coin, object> sortFunc);


    public Task<Result<List<Coin>>> GetCoinsFromContext();

    public Task<Result<bool>> ResetPriceLevels(Coin coin);

    public Task<Result<bool>> UpdatePriceLevels(Coin coin, ICollection<PriceLevel> priceLevels);
    Task<ObservableCollection<HeatMapPoint>> GetHeatMapPoints(int selectedHeatMapIndex);
    void UpdateHeatMap();
    void SortListString(SortingOrder sortingOrder, Func<Coin, object> sortFunc);
    Portfolio GetPortfolio();
}
