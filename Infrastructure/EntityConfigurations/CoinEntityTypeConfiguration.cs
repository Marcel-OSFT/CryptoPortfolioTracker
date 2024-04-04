using CryptoPortfolioTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

class CoinEntityTypeConfiguration : IEntityTypeConfiguration<Coin>
{
    public void Configure(EntityTypeBuilder<Coin> configuration)
    {
        configuration.ToTable("Coins");

        configuration.HasKey(x => x.Id);

        configuration
            .Property("Id")
            .ValueGeneratedOnAdd()
            .HasColumnName("Id");
   
        
        configuration
           .Property("ApiId")
           .HasColumnName("ApiId");
        
        configuration
            .Property("Name")
            .HasColumnName("Name");

        configuration
             .Property("Symbol")
             .HasColumnName("Symbol");
        
        configuration
            .Property("Rank")
            .HasColumnName("Rank");
        
        configuration
            .Property("ImageUri")
            .HasColumnName("ImageUri");
        
        configuration
            .Property("Price")
            .HasColumnName("Price");

        configuration
            .Property("Ath")
            .HasColumnName("Ath");
        
        configuration
            .Property("Change52Week")
            .HasColumnName("Change52Week");
        
        configuration
            .Property("MarketCap")
            .HasColumnName("MarketCap");

        configuration
            .Property("Change1Month")
            .HasColumnName("Change1Month");
        
        configuration
            .Property("About")
            .HasColumnName("About");
        
        configuration
            .Property("Change24Hr")
            .HasColumnName("Change24Hr");
        
        configuration
            .Property("IsAsset")
            .HasColumnName("IsAsset");
        
        configuration
            .Property("Note")
            .HasColumnName("Note");

        configuration
            .Ignore("TotalValue");
        configuration
            .Ignore("IsHoldingAsset");

        configuration.Navigation("Assets");

    }
}
