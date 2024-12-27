
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using CryptoPortfolioTracker.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;


namespace CryptoPortfolioTracker.Infrastructure.Response.Coins;

[Serializable]
public class MarketChartById
{

    [JsonProperty("prices")]
    public decimal?[][] Prices { get; set; }

   



    public List<DataPoint> GetPriceList()
    {
        var list = new List<DataPoint>();

        for (int i = 0; i < Prices.Length; i++)
        {
            var date = DateTime.UnixEpoch.AddMilliseconds((double)Prices[i][0]).Date;
            var dataPoint = new DataPoint { Date = DateOnly.FromDateTime(date), Value = (double)Prices[i][1] };
            list.Add(dataPoint);
        }
        return list;
    }

    public decimal?[][] FillPricesArray(List<DataPoint> list)
    {
        var sortedList = list.OrderBy(x => x.Date.ToDateTime(TimeOnly.Parse("01:00 AM"))).ToList();

        Prices = null;

        foreach (var dataPoint in sortedList)
        {
            var date = dataPoint.Date.ToDateTime(TimeOnly.Parse("01:00 AM")).Date;
            var price = dataPoint.Value;
            Prices = Prices is not null 
                ? Prices.Append(new decimal?[] { (decimal)date.Subtract(DateTime.UnixEpoch).TotalMilliseconds, (decimal)price }).ToArray() 
                : new decimal?[][] { new decimal?[] { (decimal)date.Subtract(DateTime.UnixEpoch).TotalMilliseconds, (decimal)price } };
        }
        return Prices;
    }


    public DateOnly StartDate()
    {
        if (Prices == null || Prices.Length == 0) return DateOnly.MinValue;
        var start = DateTime.UnixEpoch.AddMilliseconds((double)Prices[0][0]).Date; 
        return  DateOnly.FromDateTime(start);
    }

    public DateOnly EndDate()
    {
        if (Prices == null || Prices.Length == 0) return DateOnly.MinValue;

        var end = DateTime.UnixEpoch.AddMilliseconds((double)Prices[Prices.Length-1][0]).Date;
        return DateOnly.FromDateTime(end);
    }

    public void AddPrices(decimal?[][] prices)
    {
        Prices = Prices is not null ? Prices.Append(prices).ToArray() : prices;
    }


    public void RemoveDuplicateLastDate()
    {
        if (Prices == null || Prices.Length < 2) return;

        //check if both last datapoints have duplicate dates
        //if so then remove the last one

        var mSecLast = (double)Prices[Prices.Length - 1][0];
        var mSecPrev = (double)Prices[Prices.Length - 2][0];
        var lastDate = DateTime.UnixEpoch.AddMilliseconds((double)Prices[Prices.Length - 1][0]).Date;
        var prevDate = DateTime.UnixEpoch.AddMilliseconds((double)Prices[Prices.Length - 2][0]).Date;

        if (lastDate == prevDate)
        {
            Prices = Prices.SkipLast(1).ToArray();
        }
    }


    public async Task SaveMarketChartJson(string coinId)
    {
        if (Prices is not null)
        {
            RemoveDuplicateLastDate();

            var fileName = App.appDataPath + "\\" + App.ChartsFolder + "\\MarketChart_" + coinId + ".json";
            await using FileStream createStream = File.Create(fileName);
            await System.Text.Json.JsonSerializer.SerializeAsync(createStream, Prices);
        }
    }

    public async Task LoadMarketChartJson(string coinId)
    {
        var fileName = App.appDataPath + "\\" + App.ChartsFolder + "\\MarketChart_" + coinId + ".json";

        if (File.Exists(fileName))
        {
            try
            {
                await using FileStream openStream = File.OpenRead(fileName);
                Prices = await System.Text.Json.JsonSerializer.DeserializeAsync<decimal?[][]>(openStream);
            }
            catch(Exception ex )
            {
                Logger.LogMessage("Error loading MarketChart for {0}: " + ex.Message, coinId);
            }
            
        }

    }
}
