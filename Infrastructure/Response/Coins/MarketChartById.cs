
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;


namespace CryptoPortfolioTracker.Infrastructure.Response.Coins;

[Serializable]
public class MarketChartById
{

    [JsonProperty("prices")]
    public decimal?[][] Prices { get; set; }

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
            await using FileStream openStream = File.OpenRead(fileName);
            Prices = await System.Text.Json.JsonSerializer.DeserializeAsync<decimal?[][]>(openStream);
        }

    }
}
