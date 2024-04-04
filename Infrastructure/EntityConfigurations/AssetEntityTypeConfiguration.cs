using CryptoPortfolioTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace CryptoPortfolioTracker.Infrastructure.EntityConfigurations;

class AssetEntityTypeConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> configuration)
    {
        configuration.ToTable("Assets");

        configuration.HasKey(x => x.Id);

        configuration
            .Property("Id")
            .ValueGeneratedOnAdd()
            .HasColumnName("Id");

        configuration
            .Property("Qty")
            .HasColumnName("Qty");

        configuration
            .Property("AverageCostPrice")
            .HasColumnName("AverageCostPrice");

        configuration
            .Property("RealizedPnL")
            .HasColumnName("RealizedPnL");

        configuration
            .Property("CoinId")
            .HasColumnName("CoinId"); 
        
        configuration
            .Property("AccountId")
            .HasColumnName("AccountId");

       configuration.HasIndex("AccountId");
       configuration.HasIndex("CoinId");

        configuration.HasOne("CryptoPortfolioTracker.Models.Account", "Account")
            .WithMany("Assets")
            .HasForeignKey("AccountId");

        configuration.HasOne("CryptoPortfolioTracker.Models.Coin", "Coin")
            .WithMany("Assets")
            .HasForeignKey("CoinId");

        configuration.Navigation("Account");

        configuration.Navigation("Coin");
        
        configuration.Navigation("Mutations");



    }
}
