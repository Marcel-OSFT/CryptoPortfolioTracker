using CryptoPortfolioTracker.Infrastructure.EntityConfigurations;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace CryptoPortfolioTracker.Infrastructure
{
    public class PortfolioContext : DbContext
    {
        public PortfolioContext(DbContextOptions<PortfolioContext> connection) : base(connection) { }

        public DbSet<Coin> Coins { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Mutation> Mutations { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new TransactionEntityTypeConfiguration());
        }

    }
}
