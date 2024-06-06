using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CryptoPortfolioTracker.Infrastructure.Response.Coins;

public class HistoricalDataById
{
    public HistoricalDataById()
    {
        Id = string.Empty;
     
        Dates = new List<DateOnly>();
        Quantities = new List<double>();
        Prices = new List<double>();
    
    }

    public string Id
    {
        get; set; 
    }

    public List<DateOnly> Dates
    {
        get; set;
    }

    public List<double> Quantities
    {
        get; set;
    }

    public List<double> Prices
    {
        get; set;
    }


}
