using CryptoPortfolioTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoPortfolioTracker.Infrastructure.EntityConfigurations;

class TransactionEntityTypeConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> configuration)
    {
        configuration.ToTable("Transactions");
        configuration.HasKey(x => x.Id);
        configuration
            .Property("Id")
            .ValueGeneratedOnAdd()
            .HasColumnName("TransactionId");

        configuration
            .Property("TimeStamp")
            .HasColumnName("TimeStamp");

        configuration
            .Property("Note")
            .HasColumnName("Note");

        configuration.Navigation("Mutations");
        configuration
            .Ignore("RequestedAsset");
        configuration
            .Ignore("Details");

    }
}