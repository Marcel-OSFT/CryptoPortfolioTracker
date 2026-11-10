using CryptoPortfolioTracker.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoPortfolioTracker.ViewModels
{
    public static class ModelMapper
    {
        // ---- Summary mappers (safe) ----

        public static CoinDisplay MapCoinSummary(Coin source)
        {
            if (source is null) return null;

            return new CoinDisplay
            {
                Id = source.Id,
                ApiId = source.ApiId ?? string.Empty,
                Name = source.Name ?? string.Empty,
                Symbol = source.Symbol ?? string.Empty,
                Rank = source.Rank,
                ImageUri = source.ImageUri ?? string.Empty,
                Price = source.Price,
                Ath = source.Ath,
                Change52Week = source.Change52Week,
                Change1Month = source.Change1Month,
                MarketCap = source.MarketCap,
                About = source.About ?? string.Empty,
                Change24Hr = source.Change24Hr,
                Note = source.Note ?? string.Empty,
                IsAsset = source.IsAsset,
                // intentionally do not map Assets / PriceLevels / Narrative here
            };
        }

        public static AssetDisplay MapAssetSummary(Asset source)
        {
            if (source is null) return null;

            return new AssetDisplay
            {
                Id = source.Id,
                Qty = source.Qty,
                AverageCostPrice = source.AverageCostPrice,
                RealizedPnL = source.RealizedPnL,
                Coin = MapCoinSummary(source.Coin),
                Account = source.Account is null ? null : new AccountDisplay
                {
                    Id = source.Account.Id,
                    Name = source.Account.Name ?? string.Empty,
                    About = source.Account.About ?? string.Empty,
                    IsHoldingAsset = source.Account.IsHoldingAsset,
                    TotalValue = source.Account.TotalValue
                },
                // intentionally do not map Mutations here
            };
        }

        public static AssetTotalsDisplay MapAssetTotalsSummary(AssetTotals source)
        {
            if (source is null) return null;

            return new AssetTotalsDisplay
            {
                Qty = source.Qty,
                CostBase = source.CostBase,
                AverageCostPrice = source.AverageCostPrice,
                ProfitLossPerc = source.ProfitLossPerc,
                NetProfitLossPerc = source.NetProfitLossPerc,
                ProfitLoss = source.ProfitLoss,
                MarketValue = source.MarketValue,
                RealizedPnL = source.RealizedPnL,
                Coin = source.Coin is null ? new CoinDisplay() : MapCoinSummary(source.Coin),
                IsHidden = source.IsHidden,
                NetInvestment = source.NetInvestment
            };
        }
        public static NarrativeDisplay MapNarrativeSummary(Narrative source)
        {
            if (source is null) return null;

            return new NarrativeDisplay
            {
                Id = source.Id,
                Name = source.Name ?? string.Empty,
                About = source.About ?? string.Empty,
                IsHoldingCoins = source.IsHoldingCoins,
                TotalValue = source.TotalValue,
                CostBase = source.CostBase,
                ProfitLoss = source.ProfitLoss,
                ProfitLossPerc = source.ProfitLossPerc
                // intentionally do not map Coins here
            };
        }

        public static PriceLevelDisplay MapPriceLevelSummary(PriceLevel source)
        {
            if (source is null) return null;

            return new PriceLevelDisplay
            {
                Id = source.Id,
                Type = source.Type,
                Value = source.Value,
                Note = source.Note ?? string.Empty,
                Status = source.Status,
                DistanceToValuePerc = source.DistanceToValuePerc,
                Coin = MapCoinSummary(source.Coin)
            };
        }

        public static MutationDisplay MapMutationSummary(Mutation source)
        {
            if (source is null) return null;

            return new MutationDisplay
            {
                Id = source.Id,
                Type = source.Type,
                Qty = source.Qty,
                Price = source.Price,
                Direction = source.Direction,
                Asset = MapAssetSummary(source.Asset),
                // intentionally do not map Transaction here
            };
        }

        public static AccountDisplay MapAccountSummary(Account source)
        {
            if (source is null) return null;

            return new AccountDisplay
            {
                Id = source.Id,
                Name = source.Name ?? string.Empty,
                About = source.About ?? string.Empty,
                IsHoldingAsset = source.IsHoldingAsset,
                TotalValue = source.TotalValue
                // intentionally do not map Assets here
            };
        }

        public static TransactionDisplay MapTransactionSummary(Transaction source)
        {
            if (source is null) return null;

            return new TransactionDisplay
            {
                Id = source.Id,
                TimeStamp = source.TimeStamp,
                Note = source.Note ?? string.Empty,
                RequestedAsset = MapAssetSummary(source.RequestedAsset),
                Details = source.Details,
                // intentionally do not map Mutations here unless you really need them
            };
        }

        // ---- Full but safe display mapping ----
        // Only use these when you explicitly need the child collection and it is not cyclic.

        public static CoinDisplay MapToDisplay(Coin source)
        {
            if (source is null) return null;

            var coin = MapCoinSummary(source);

            coin.Assets = source.Assets?
                .Select(MapAssetSummary)
                .Where(x => x != null)
                .ToList() ?? new List<AssetDisplay>();

            coin.Narrative = MapNarrativeSummary(source.Narrative);
            coin.PriceLevels = source.PriceLevels?
                .Select(MapPriceLevelSummary)
                .Where(x => x != null)
                .ToList() ?? new List<PriceLevelDisplay>();

            return coin;
        }

        public static AssetDisplay MapToDisplay(Asset source)
        {
            if (source is null) return null;

            var asset = MapAssetSummary(source);

            asset.Coin = MapCoinSummary(source.Coin);
            asset.Account = MapAccountSummary(source.Account);
            asset.Mutations = source.Mutations?
                .Select(MapMutationSummary)
                .Where(x => x != null)
                .ToList() ?? new List<MutationDisplay>();

            return asset;
        }

        public static AssetTotalsDisplay MapToDisplay(AssetTotals source)
        {
            if (source is null) return null;

            var result = MapAssetTotalsSummary(source);

            // safe, shallow display mapping
            result.Coin = source.Coin is null ? new CoinDisplay() : MapCoinSummary(source.Coin);

            return result;
        }

        public static NarrativeDisplay MapToDisplay(Narrative source)
        {
            if (source is null) return null;

            var narrative = MapNarrativeSummary(source);

            narrative.Coins = source.Coins?
                .Select(MapCoinSummary)
                .Where(x => x != null)
                .ToList() ?? new List<CoinDisplay>();

            return narrative;
        }

        public static PriceLevelDisplay MapToDisplay(PriceLevel source)
        {
            if (source is null) return null;

            var level = MapPriceLevelSummary(source);
            level.Coin = MapCoinSummary(source.Coin);
            return level;
        }

        public static MutationDisplay MapToDisplay(Mutation source)
        {
            if (source is null) return null;

            var mutation = MapMutationSummary(source);
            mutation.Asset = MapAssetSummary(source.Asset);
            mutation.Transaction = MapTransactionSummary(source.Transaction);
            return mutation;
        }

        public static AccountDisplay MapToDisplay(Account source)
        {
            if (source is null) return null;

            var account = MapAccountSummary(source);

            account.Assets = source.Assets?
                .Select(MapAssetSummary)
                .Where(x => x != null)
                .ToList() ?? new List<AssetDisplay>();

            return account;
        }

        public static TransactionDisplay MapToDisplay(Transaction source)
        {
            if (source is null) return null;

            var tx = MapTransactionSummary(source);

            tx.Mutations = source.Mutations?
                .Select(MapMutationSummary)
                .Where(x => x != null)
                .ToList() ?? new List<MutationDisplay>();

            tx.RequestedAsset = MapAssetSummary(source.RequestedAsset);

            return tx;
        }

        //*** DOMAIN Mappers
        public static Transaction MapToDomain(TransactionDisplay source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return new Transaction
            {
                Id = source.Id,
                TimeStamp = source.TimeStamp,
                Note = source.Note ?? string.Empty,
                RequestedAsset = source.RequestedAsset is null ? null : MapToDomain(source.RequestedAsset),
                Details = source.Details,
                Mutations = source.Mutations?
                    .Select(MapToDomain)
                    .ToList() ?? new List<Mutation>()
            };
        }

        public static Asset MapToDomain(AssetDisplay source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return new Asset
            {
                Id = source.Id,
                Qty = source.Qty,
                AverageCostPrice = source.AverageCostPrice,
                RealizedPnL = source.RealizedPnL,
                Coin = source.Coin is null ? null : MapToDomain(source.Coin),
                Account = source.Account is null ? null : MapToDomain(source.Account),
                Mutations = source.Mutations?
                .Select(MapToDomain)
                .Where(x => x != null)
                .ToList() ?? new List<Mutation>()
            };
        }

        public static Coin MapToDomain(CoinDisplay source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return new Coin
            {
                Id = source.Id,
                ApiId = source.ApiId,
                Name = source.Name,
                Symbol = source.Symbol,
                Rank = source.Rank,
                ImageUri = source.ImageUri,
                Price = source.Price,
                Ath = source.Ath,
                Change52Week = source.Change52Week,
                Change1Month = source.Change1Month,
                MarketCap = source.MarketCap,
                About = source.About,
                Change24Hr = source.Change24Hr,
                Note = source.Note,
                IsAsset = source.IsAsset,
                Assets = source.Assets?
                .Select(MapToDomain)
                .Where(x => x != null)
                .ToList() ?? new List<Asset>(),

                Narrative = MapToDomain(source.Narrative),
                PriceLevels = source.PriceLevels?
                .Select(MapToDomain)
                .Where(x => x != null)
                .ToList() ?? new List<PriceLevel>()
            };
        }

        public static Account MapToDomain(AccountDisplay source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return new Account
            {
                Id = source.Id,
                Name = source.Name,
                About = source.About,
                IsHoldingAsset = source.IsHoldingAsset,
                TotalValue = source.TotalValue,
                Assets = source.Assets?
                .Select(MapToDomain)
                .Where(x => x != null)
                .ToList() ?? new List<Asset>()
            };
        }

        public static Mutation MapToDomain(MutationDisplay source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return new Mutation
            {
                Id = source.Id,
                Type = source.Type,
                Qty = source.Qty,
                Price = source.Price,
                Direction = source.Direction,
                Asset = source.Asset is null ? null : MapToDomain(source.Asset),
                Transaction = source.Transaction is null ? null : MapToDomain(source.Transaction)
            };
        }

        public static AssetTotals MapToDomain(AssetTotalsDisplay source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return new AssetTotals
            {
                Qty = source.Qty,
                CostBase = source.CostBase,
                AverageCostPrice = source.AverageCostPrice,
                ProfitLossPerc = source.ProfitLossPerc,
                NetProfitLossPerc = source.NetProfitLossPerc,
                ProfitLoss = source.ProfitLoss,
                MarketValue = source.MarketValue,
                RealizedPnL = source.RealizedPnL,
                Coin = source.Coin is null ? new Coin() : MapToDomain(source.Coin),
                IsHidden = source.IsHidden,
                NetInvestment = source.NetInvestment
            };
        }

        public static Narrative MapToDomain(NarrativeDisplay source)
        {
            if (source is null) return null;

            return new Narrative
            {
                Id = source.Id,
                Name = source.Name ?? string.Empty,
                About = source.About ?? string.Empty,
                IsHoldingCoins = source.IsHoldingCoins,
                TotalValue = source.TotalValue,
                CostBase = source.CostBase,
                ProfitLoss = source.ProfitLoss,
                ProfitLossPerc = source.ProfitLossPerc,
                Coins = source.Coins?
                    .Select(MapToDomain)
                    .Where(x => x != null)
                    .ToList() ?? new List<Coin>()
            };
        }

        public static PriceLevel MapToDomain(PriceLevelDisplay source)
        {
            if (source is null) return null;

            return new PriceLevel
            {
                Id = source.Id,
                Type = source.Type,
                Value = source.Value,
                Note = source.Note ?? string.Empty,
                Status = source.Status,
                DistanceToValuePerc = source.DistanceToValuePerc,
                Coin = MapToDomain(source.Coin)
            };
        }

    }
}