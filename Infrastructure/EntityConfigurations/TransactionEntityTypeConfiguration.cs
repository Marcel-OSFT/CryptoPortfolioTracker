using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using CryptoPortfolioTracker.Models;
using System;

namespace CryptoPortfolioTracker.Infrastructure.EntityConfigurations;

class TransactionEntityTypeConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> TransactionConfiguration)
    {
        TransactionConfiguration.ToTable("Transactions");

        TransactionConfiguration.HasKey(x => x.Id);

        TransactionConfiguration
            .Property("Id")
            .HasColumnName("TransactionId");

        TransactionConfiguration
            .Property("TimeStamp")
            .HasColumnName("TimeStamp");

        TransactionConfiguration
            .Property("Note")
            .HasColumnName("Note");
    }
}