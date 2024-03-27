using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using CryptoPortfolioTracker.Models;

namespace CryptoPortfolioTracker.Infrastructure.EntityConfigurations;

class AccountEntityTypeConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> accountConfiguration)
    {
        accountConfiguration.ToTable("Accounts");

        accountConfiguration.HasKey(x => x.Id);
        accountConfiguration
            .Property("Id")
            .HasColumnName("AccountId");

        accountConfiguration
            .Property("Name")
            .HasColumnName("Name");

        



    }
}

