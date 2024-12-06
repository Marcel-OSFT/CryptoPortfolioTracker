
using CryptoPortfolioTracker.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CryptoPortfolioTracker.Infrastructure.EntityConfigurations
{
    class PriceLevelEntityTypeConfiguration : IEntityTypeConfiguration<PriceLevel>
    {
        public void Configure(EntityTypeBuilder<PriceLevel> configuration)
        {
            configuration.ToTable("PriceLevels");
            configuration.HasKey(x => x.Id);
            configuration
                .Property("Id")
                .ValueGeneratedOnAdd()
                .HasColumnName("Id");

            configuration
                .Property("Type")
                .HasColumnName("Type");
            
            configuration
                .Property("Value")
                .HasColumnName("Value");

            configuration
                .Property("Note")
                .HasColumnName("Note");

            configuration
                .Property("Status")
                .HasColumnName("Status");

            configuration
                .Property("CoinId")
                .HasColumnName("CoinId");

            configuration.HasIndex("CoinId");

            configuration.HasOne("CryptoPortfolioTracker.Models.Coin", "Coin")
                .WithMany("PriceLevels")
                .HasForeignKey("CoinId");

            configuration.Navigation("Coin");
        }
    }
}
