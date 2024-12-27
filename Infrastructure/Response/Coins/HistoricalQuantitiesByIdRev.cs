using System;
using System.Collections.Generic;
using CryptoPortfolioTracker.Models;
using Newtonsoft.Json;

namespace CryptoPortfolioTracker.Infrastructure.Response.Coins;

public class HistoricalDataByIdRev
{
    public HistoricalDataByIdRev()
    {
        Id = string.Empty;
        //Dates = new List<DateOnly>();
        //Quantities = new List<double>();
        //Prices = new List<double>();
    
        DataPoints = new List<DataPoint>();

    }

    public string Id { get; set; }

    public List<DataPoint> DataPoints { get; set; }

    //public List<DateOnly> Dates { get; set; }

    //public List<double> Quantities { get; set; }

    //public List<double> Prices { get; set; }


}
