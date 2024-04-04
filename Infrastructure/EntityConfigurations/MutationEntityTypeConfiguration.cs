using CryptoPortfolioTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace CryptoPortfolioTracker.Infrastructure.EntityConfigurations;

class MutationEntityTypeConfiguration : IEntityTypeConfiguration<Mutation>
{
    public void Configure(EntityTypeBuilder<Mutation> configuration)
    {
        configuration.ToTable("Mutations");

        configuration.HasKey(x => x.Id);

        configuration
            .Property("Id")
            .ValueGeneratedOnAdd()
            .HasColumnName("Id");

        configuration
            .Property("Type")
            .HasColumnName("Type");

        configuration
            .Property("Qty")
            .HasColumnName("Qty");

        configuration
            .Property("Price")
            .HasColumnName("Price");

        configuration
           .Property("Direction")
           .HasColumnName("Direction");

        configuration
            .Property("AssetId")
            .HasColumnName("AssetId");

        configuration
            .Property("TransactionId")
            .HasColumnName("TransactionId");

        configuration.HasIndex("AssetId");
        configuration.HasIndex("TransactionId");

        configuration.HasOne("CryptoPortfolioTracker.Models.Asset", "Asset")
                        .WithMany("Mutations")
                        .HasForeignKey("AssetId");

        configuration.HasOne("CryptoPortfolioTracker.Models.Transaction", "Transaction")
            .WithMany("Mutations")
            .HasForeignKey("TransactionId");

        configuration.Navigation("Asset");

        configuration.Navigation("Transaction");

    }
}