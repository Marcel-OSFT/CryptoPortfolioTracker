using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using CryptoPortfolioTracker.Models;

namespace CryptoPortfolioTracker.Infrastructure.EntityConfigurations;

class AssetEntityTypeConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> assetConfiguration)
    {
        assetConfiguration.ToTable("Assets");

        assetConfiguration.HasKey(x => x.Id);
        assetConfiguration
            .Property("Id")
            .HasColumnName("AssetId");

        assetConfiguration
            .Property("Qty")
            .HasColumnName("Qty");

        assetConfiguration
            .Property("MarketValue")
            .HasColumnName("Marketvalue");

        assetConfiguration
            .Property("CostBase")
            .HasColumnName("CostBase");

        assetConfiguration
            .Property("ProfitLossPerc")
            .HasColumnName("ImageUri");

        //assetConfiguration
        //     .Property("CoinId")
        //     .HasColumnName("CoinId");

        

    }
}

