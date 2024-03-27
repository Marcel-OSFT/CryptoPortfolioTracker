using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using CryptoPortfolioTracker.Models;

namespace CryptoPortfolioTracker.Infrastructure.EntityConfigurations;

class CoinEntityTypeConfiguration : IEntityTypeConfiguration<Coin>
{
    public void Configure(EntityTypeBuilder<Coin> coinConfiguration)
    {
        coinConfiguration.ToTable("Coins");

        coinConfiguration.HasKey(x => x.Id);
        coinConfiguration            
            .Property("Id")
            .HasColumnName("CoinId");

        coinConfiguration
            .Property("ApiId")
            .HasColumnName("ApiId");

        coinConfiguration
            .Property("Name")
            .HasColumnName("Name");

        coinConfiguration
            .Property("Symbol")
            .HasColumnName("Symbol");

        coinConfiguration
            .Property("ImageUri")
            .HasColumnName("ImageUri");

        coinConfiguration
             .Property("Price")
             .HasColumnName("Price");

        coinConfiguration
              .Property("Ath")
              .HasColumnName("Ath");

        coinConfiguration
             .Property("Change52Week")
             .HasColumnName("Change52Week");

        coinConfiguration
             .Property("Change1Month")
             .HasColumnName("Change1Month");

        coinConfiguration
             .Property("MarketCap")
             .HasColumnName("MarketCap");

        coinConfiguration
             .Property("About")
             .HasColumnName("About");

        coinConfiguration
             .Property("Rank")
             .HasColumnName("Rank");

        coinConfiguration
             .Property("Change24Hr")
             .HasColumnName("Change24Hr");

        coinConfiguration
             .Property("Price")
             .HasColumnName("Price");

        coinConfiguration
             .Property("IsAsset")
             .HasColumnName("IsAsset");




    }
}
