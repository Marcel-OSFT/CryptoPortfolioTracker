using CryptoPortfolioTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPortfolioTracker.Infrastructure.EntityConfigurations;
class NarrativeEntityTypeConfiguration : IEntityTypeConfiguration<Narrative>
{
    public void Configure(EntityTypeBuilder<Narrative> configuration)
    {
        configuration.ToTable("Narratives");

        configuration.HasKey(x => x.Id);

        configuration
            .Property("Id")
            .ValueGeneratedOnAdd()
            .HasColumnName("Id")
            .HasColumnType("INTEGER");

        configuration
            .Property("Name")
            .HasColumnName("Name")
            .HasColumnType("TEXT");

        configuration
            .Property("About")
            .HasColumnName("About")
            .HasColumnType("TEXT");

        configuration.Navigation("Coins");

        configuration
            .Ignore("IsHoldingCoins");

        configuration
            .Ignore("TotalValue");
        configuration
            .Ignore("CostBase");
        configuration
            .Ignore("ProfitLoss");
        configuration
            .Ignore("ProfitLossPerc");

    }
}