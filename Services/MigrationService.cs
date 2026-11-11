using CryptoPortfolioTracker.Models.Legacy;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json;

namespace CryptoPortfolioTracker.Services
{
    public interface IMigrationService
    {
        Task AddPriceLevelsEmaToDatabase(PortfolioContext context);
        Task ApplyDatabaseMigrations(PortfolioContext context);
        Task CheckAvailableStablecoins(PortfolioContext context);
        Task MigrateGraphIfNeeded(string signature);
        Task RenameNarrative(PortfolioContext context, string oldName, string newName);
        Task RepairCoinIsAssetStatus(PortfolioContext context);
    }

    public class MigrationService : IMigrationService
    {
        private readonly IExchangeRateService _exchangeRateService;

        private static ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(MigrationService).Name.PadRight(22));

        public MigrationService(IExchangeRateService exchangeRateService)
        {
            _exchangeRateService = exchangeRateService;
        }



        public async Task MigrateGraphIfNeeded(string signature)
        {
            string fileName = string.Empty;
            try
            {
                //*** check/read only Version to see if migration is needed.
                fileName = System.IO.Path.Combine(AppConstants.PortfoliosPath, signature, "graph.json");
                var json = await File.ReadAllTextAsync(fileName);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("Version", out var version))
                {
                    switch (version.GetInt32())
                    {
                        case < 2:
                            await MigrateGraphV1ToV2(fileName, signature);
                            break;

                        case 2:
                            // Already up to date
                            break;
                    }
                }
                else //*** Version field not found at all
                {
                    await MigrateGraphV1ToV2(fileName, signature);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to migrate Graph data to version 2");
            }
        }

        
        private async Task MigrateGraphV1ToV2(string fileName, string signature)
        {
            var graphV1 = new GraphV1();

            await LoadGraphDataFromJson(fileName, async stream =>
            {
                graphV1 = await System.Text.Json.JsonSerializer.DeserializeAsync<GraphV1>(stream);
                Logger.Information("Graph data de-serialized successfully ({0} data points)", graphV1.DataPointsPortfolio.Count);
            });


            if (graphV1 != null)
            {
                var graphV2 = await MigrateGraphDataToV2(graphV1, signature);
                SaveGraphDataToJson(graphV2,fileName);
                Logger.Information("Graph data successfully migrated to version)");
            }
        }



        private static async Task LoadGraphDataFromJson(string fileName, Func<FileStream, Task> processStream)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    using FileStream openStream = File.OpenRead(fileName);
                    await processStream(openStream);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to de-serialize data from {0}", fileName);
                }
            }
        }

        private async Task SaveGraphDataToJson(Graph graph, string fileName)
        {
            try
            {
                //*** make a copy of the original file to V1 file
                if (File.Exists(fileName))
                {
                    File.Copy(fileName, fileName.Replace("graph", "graphV1"), true);
                }

                //*** Serialize new graph class (with new DataPoint definition)
                await using FileStream createStream = File.Create(fileName);
                await System.Text.Json.JsonSerializer.SerializeAsync(createStream, graph);

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to serialize Graph data");
            }
        }

        
        public async Task<Graph> MigrateGraphDataToV2(GraphV1 source, string signature)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));


            DateOnly startDate = source.DataPointsPortfolio.MinBy(x => x.Date)?.Date ?? DateOnly.MinValue;
            await _exchangeRateService.LoadDailyExchangeRates(startDate.ToDateTime(TimeOnly.MinValue));


            var target = new Graph
            {
                Version = 2,
                GraphStatus = source.GraphStatus,
                IsModificationRequested = source.IsModificationRequested,
                ModifyFromDate = source.ModifyFromDate,
                Signature = signature
            };

            target.DataPointsPortfolio = source.DataPointsPortfolio?
                .Select(x => new DataPoint
                {
                    Date = x.Date,
                    ValueUsd = x.Value,
                    ValueEur = x.Value / (_exchangeRateService.TryGetDailyRate(x.Date, out var rate) ? rate : 1)
                })
                .ToList() ?? new List<DataPoint>();

            target.DataPointsInFlow = source.DataPointsInFlow?
                .Select(x => new DataPoint
                {
                    Date = x.Date,
                    ValueUsd = x.Value,
                    ValueEur = x.Value / (_exchangeRateService.TryGetDailyRate(x.Date, out var rate) ? rate : 1)
                })
                .ToList() ?? new List<DataPoint>();

            target.DataPointsOutFlow = source.DataPointsOutFlow?
                .Select(x => new DataPoint
                {
                    Date = x.Date,
                    ValueUsd = x.Value,
                    ValueEur = x.Value / (_exchangeRateService.TryGetDailyRate(x.Date, out var rate) ? rate : 1)
                })
                .ToList() ?? new List<DataPoint>();

            return target;
        }

        public async Task ApplyDatabaseMigrations(PortfolioContext context)
        {
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            var initPriceLevelsEntity = pendingMigrations.Any(m => m.Contains("AddPriceLevelsEntity"));
            var initNarrativesEntity = pendingMigrations.Any(m => m.Contains("AddNarrativesEntity"));

            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync();

                var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                var newlyApplied = appliedMigrations
                    .Where(m => pendingMigrations.Contains(m))
                    .ToList();
                foreach (var migration in newlyApplied)
                {
                    Logger?.Information("Applied Migrations {0}", migration);
                }
            }
            else
            {
                Logger?.Information("Database already up to date. No new migrations applied.");
            }
        }

        public async Task RepairCoinIsAssetStatus(PortfolioContext context)
        {
            //var context = Context;
            if (context == null) return;
            context.ChangeTracker.Clear();

            try
            {
                var coins = await context.Coins.ToListAsync();

                foreach (var coin in coins)
                {
                    var isAsset = context.Assets.Any(a => a.Coin.ApiId == coin.ApiId);
                    if (coin.IsAsset != isAsset)
                    {
                        coin.IsAsset = isAsset;
                        context.Update(coin);
                    }
                }
                await context.SaveChangesAsync();
            }
            finally
            {
                context.ChangeTracker.Clear();
            }

        }


        public async Task CheckAvailableStablecoins(PortfolioContext context)
        {
            var stablecoins = new[]
            {
                new { Name = "Tether", Symbol = "USDT", ApiId = "tether", ImageUri = "https://coin-images.coingecko.com/coins/images/325/small/Tether.png?1696501661",
                    About = "Tether (USDT) is a cryptocurrency with a value meant to mirror the value of the U.S. dollar. The idea was to create a stable cryptocurrency that can be used like digital dollars. Coins that serve this purpose of being a stable dollar substitute are called “stable coins.” Tether is the most popular stable coin and even acts as a dollar replacement on many popular exchanges! According to their site, Tether converts cash into digital currency, to anchor or “tether” the value of the coin to the price of national currencies like the US dollar, the Euro, and the Yen. Like other cryptos it uses blockchain. Unlike other cryptos, it is [according to the official Tether site] “100% backed by USD” (USD is held in reserve). The primary use of Tether is that it offers some stability to the otherwise volatile crypto space and offers liquidity to exchanges who can’t deal in dollars and with banks (for example to the sometimes controversial but leading exchange <a href=\"https://www.coingecko.com/en/exchanges/bitfinex\">Bitfinex</a>).\r\n\r\nThe digital coins are issued by a company called Tether Limited that is governed by the laws of the British Virgin Islands, according to the legal part of its website. It is incorporated in Hong Kong. It has emerged that Jan Ludovicus van der Velde is the CEO of cryptocurrency exchange Bitfinex, which has been accused of being involved in the price manipulation of bitcoin, as well as tether. Many people trading on exchanges, including Bitfinex, will use tether to buy other cryptocurrencies like bitcoin. Tether Limited argues that using this method to buy virtual currencies allows users to move fiat in and out of an exchange more quickly and cheaply. Also, exchanges typically have rocky relationships with banks, and using Tether is a way to circumvent that.\r\n\r\nUSDT is fairly simple to use. Once on exchanges like <a href=\"https://www.coingecko.com/en/exchanges/poloniex\">Poloniex</a> or Bittrex, it can be used to purchase Bitcoin and other cryptocurrencies. It can be easily transferred from an exchange to any Omni Layer enabled wallet. Tether has no transaction fees, although external wallets and exchanges may charge one. In order to convert USDT to USD and vise versa through the Tether.to Platform, users must pay a small fee. Buying and selling Tether for Bitcoin can be done through a variety of exchanges like the ones mentioned previously or through the Tether.to platform, which also allows the conversion between USD to and from your bank account." },
                new { Name = "USD Coin", Symbol = "USDC", ApiId = "usd-coin", ImageUri = "https://coin-images.coingecko.com/coins/images/6319/small/USDC.png?1769615602",
                    About = "USDC is a fully collateralized US dollar stablecoin. USDC is the bridge between dollars and trading on cryptocurrency exchanges. The technology behind CENTRE makes it possible to exchange value between people, businesses and financial institutions just like email between mail services and texts between SMS providers. We believe by removing artificial economic borders, we can create a more inclusive global economy." },
                new { Name = "Euro Coin", Symbol = "EURC", ApiId = "euro-coin", ImageUri = "https://coin-images.coingecko.com/coins/images/26045/small/EURC.png?1769615705" ,
                    About = "Euro Coin is a stablecoin issued by Circle under the same reserve model as USD Coin (USDC). It is designed to be stable and 100% backed by euros held in euro denominated banking accounts. An Euro Coin (EUROC) will be redeemable 1:1 for the euro currency." },
                new { Name = "Euro CoinVertible", Symbol = "EURCV", ApiId = "societe-generale-forge-eurcv", ImageUri = "https://coin-images.coingecko.com/coins/images/33415/small/eurcv_%281%29.png?1701752017",
                    About = "EUR CoinVertible (EURCV) is a euro-pegged stablecoin issued by Société Générale–FORGE, designed to maintain a 1:1 value with the euro. It is a MiCA-compliant e-money token backed by segregated euro-denominated reserves."},
                new { Name = "EURI", Symbol = "EURI", ApiId = "euri", ImageUri = "https://example.com/euri.png",
                    About = "" },
                new { Name = "EURE", Symbol = "EURE", ApiId = "eure", ImageUri = "https://example.com/eure.png",
                    About = "" },
                new { Name = "EUROP", Symbol = "EUROP", ApiId = "europ", ImageUri = "https://example.com/europ.png",
                    About = "" }
            };

            if (context == null) return;

            var scNarrative = await context.Narratives.FirstOrDefaultAsync(n => n.Name == "Stablecoin");

            foreach (var sc in stablecoins)
            {
                var coin = await context.Coins.FirstOrDefaultAsync(c => c.Symbol == sc.Symbol);
                if (coin == null)
                {
                    // Only add required stablecoins
                    if (sc.Symbol == "USDT" || sc.Symbol == "USDC" || sc.Symbol == "EURC" || sc.Symbol == "EURCV")
                    {
                        coin = new Coin
                        {
                            Name = sc.Name,
                            Symbol = sc.Symbol,
                            ApiId = sc.ApiId,
                            IsAsset = false,
                            Narrative = scNarrative,
                            About = sc.About
                        };
                        context.Coins.Add(coin);
                    }
                }
                else
                {
                    if (coin.Narrative != scNarrative)
                    {
                        coin.Narrative = scNarrative;
                        context.Coins.Update(coin);
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        public async Task RenameNarrative(PortfolioContext context, string oldName, string newName)
        {
            
            if (context == null) return;

            var narrative = await context.Narratives.FirstOrDefaultAsync(n => n.Name == oldName);
            if (narrative != null)
            {
                narrative.Name = newName;
                await context.SaveChangesAsync();
            }
        }

        public async Task AddPriceLevelsEmaToDatabase(PortfolioContext context)
        {
            if (context == null) return;
            context.ChangeTracker.Clear();

            try
            {
                var coinsWithoutEma = await context.Coins
                    .Where(coin => !coin.PriceLevels.Any(pl => pl.Type == PriceLevelType.Ema))
                    .ToListAsync();

                if (!coinsWithoutEma.Any()) return;

                foreach (var coin in coinsWithoutEma)
                {
                    var priceLevel = new PriceLevel
                    {
                        Coin = coin,
                        Type = PriceLevelType.Ema,
                        Value = 0,
                        Note = string.Empty,
                        Status = PriceLevelStatus.NotWithinRange
                    };
                    coin.PriceLevels.Add(priceLevel);
                    context.Coins.Update(coin);
                }
                await context.SaveChangesAsync();
            }
            finally
            {
                context.ChangeTracker.Clear();
            }
        }


    }
}
