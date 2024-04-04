using CryptoPortfolioTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

class AccountEntityTypeConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> configuration)
    {
        configuration.ToTable("Accounts");

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

        configuration
            .Ignore("TotalValue");
        configuration
            .Ignore("IsHoldingAsset");
       
        configuration.Navigation("Assets");

    }
}