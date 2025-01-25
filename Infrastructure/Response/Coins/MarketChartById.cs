
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using CryptoPortfolioTracker.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System.Diagnostics;


namespace CryptoPortfolioTracker.Infrastructure.Response.Coins;

[Serializable]
public class MarketChartById
{

    [JsonProperty("prices")]
    public decimal?[][] Prices { get; set; } = Array.Empty<decimal?[]>();


    public List<DataPoint> GetPriceList()
    {
        if (Prices == null || Prices.Length == 0) return new List<DataPoint>();

        var list = new List<DataPoint>();

        for (int i = 0; i < Prices.Length; i++)
        {
            if (Prices[i][0].HasValue && Prices[i][1].HasValue)
            {
                var date = DateTime.UnixEpoch.AddMilliseconds((double)Prices[i][0]!.Value).Date;
                var dataPoint = new DataPoint { Date = DateOnly.FromDateTime(date), Value = (double)Prices[i][1]!.Value };
                list.Add(dataPoint);
            }
        }

        //remove faulty duplicate enties
        var cleanList = list.GroupBy(x => x.Date).Select(g => g.First()).OrderBy(t => t.Date).ToList();

        return cleanList;
    }

    public decimal?[][] FillPricesArray(List<DataPoint> list)
    {
        var sortedList = list.OrderBy(x => x.Date.ToDateTime(TimeOnly.Parse("01:00 AM"))).ToList();

        Prices = Array.Empty<decimal?[]>();

        foreach (var dataPoint in sortedList)
        {
            var date = dataPoint.Date.ToDateTime(TimeOnly.Parse("01:00 AM")).Date;
            var price = dataPoint.Value;
            Prices = Prices.Append(new decimal?[] { (decimal)date.Subtract(DateTime.UnixEpoch).TotalMilliseconds, (decimal)price }).ToArray();
        }
        return Prices;
    }


    public DateOnly StartDate()
    {
        if (Prices == null || Prices.Length == 0 || !Prices[0][0].HasValue) return DateOnly.MinValue;
        var start = DateTime.UnixEpoch.AddMilliseconds((double)Prices[0][0]!.Value).Date;
        return DateOnly.FromDateTime(start);
    }

    public DateOnly EndDate()
    {
        if (Prices == null || Prices.Length == 0 || !Prices[Prices.Length - 1][0].HasValue) return DateOnly.MinValue;
        var end = DateTime.UnixEpoch.AddMilliseconds((double)Prices[Prices.Length - 1][0]!.Value).Date;
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

        var mSecLast = Prices[Prices.Length - 1][0];
        var mSecPrev = Prices[Prices.Length - 2][0];

        if (mSecLast.HasValue && mSecPrev.HasValue)
        {
            var lastDate = DateTime.UnixEpoch.AddMilliseconds((double)mSecLast.Value).Date;
            var prevDate = DateTime.UnixEpoch.AddMilliseconds((double)mSecPrev.Value).Date;

            if (lastDate == prevDate)
            {
                Prices = Prices.SkipLast(1).ToArray();
            }
        }
    }


    public async Task SaveMarketChartJson(string coinId)
    {
        if (Prices is not null)
        {
            RemoveDuplicateLastDate();

            var fileName = App.ChartsFolder + "\\MarketChart_" + coinId + ".json";
            await using FileStream createStream = File.Create(fileName);
            await System.Text.Json.JsonSerializer.SerializeAsync(createStream, Prices);
        }
    }

    public async Task LoadMarketChartJson(string coinId)
    {
        var fileName = App.ChartsFolder + "\\MarketChart_" + coinId + ".json";

        if (File.Exists(fileName))
        {
            try
            {
                await using FileStream openStream = File.OpenRead(fileName);
                Prices = await System.Text.Json.JsonSerializer.DeserializeAsync<decimal?[][]>(openStream);

                CheckAndFixPrices(this);

            }
            catch(Exception ex )
            {
                Logger.LogMessage("Error loading MarketChart for {0}: " + ex.Message, coinId);
            }
            
        }

    }


    private decimal?[][] CheckAndFixPrices(MarketChartById Chart)
    {
        var priceList = this.GetPriceList();
        var checkedChart = new MarketChartById();

        var startDate = this.StartDate().ToDateTime(TimeOnly.Parse("01:00 AM")).Date;
        var endDate = this.EndDate().ToDateTime(TimeOnly.Parse("01:00 AM")).Date;

        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dataPointToCheck = priceList.Where(x => x.Date.ToDateTime(TimeOnly.Parse("01:00 AM")).Date == date.Date).FirstOrDefault();

            if (dataPointToCheck is not null)
            {
                continue; //and check next
            }
            else
            {
                var value = 0.0;
                var adjacent = priceList.Where(x => x.Date.ToDateTime(TimeOnly.Parse("01:00 AM")).Date == date.AddDays(-1).Date).FirstOrDefault();

                if (adjacent is not null)
                {
                    value = adjacent.Value;
                }
                
                var dataPoint = new DataPoint { Date = DateOnly.FromDateTime(date), Value = value };
                priceList.Add(dataPoint);
            }
        }

        checkedChart.FillPricesArray(priceList);

        return checkedChart.Prices;

    }




}
