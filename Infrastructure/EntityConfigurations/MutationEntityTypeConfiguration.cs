using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using CryptoPortfolioTracker.Models;

namespace CryptoPortfolioTracker.Infrastructure.EntityConfigurations;

class MutationEntityTypeConfiguration : IEntityTypeConfiguration<Mutation>
{
    public void Configure(EntityTypeBuilder<Mutation> mutationConfiguration)
    {
        mutationConfiguration.ToTable("Mutations");

        mutationConfiguration.HasKey(x => x.Id);

        mutationConfiguration.HasOne<Transaction>(s => s.Transaction)
                                .WithMany(g => g.Mutations)
                                .HasForeignKey(s => s.Id);

        mutationConfiguration
            .Property("Id")
            .HasColumnName("MutationId");

        mutationConfiguration
            .Property("Type")
            .HasColumnName("Type");

        mutationConfiguration
            .Property("Direction")
            .HasColumnName("Direction");

        mutationConfiguration
            .Property("Qty")
            .HasColumnName("Qty");
    
        mutationConfiguration
            .Property("Price")
            .HasColumnName("Price");

    }
}

