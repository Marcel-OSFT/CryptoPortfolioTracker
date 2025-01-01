using CryptoPortfolioTracker.Infrastructure.EntityConfigurations;
using CryptoPortfolioTracker.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace CryptoPortfolioTracker.Infrastructure
{
  
    public class PortfolioContext : DbContext
    {

        /// <summary>
        /// /for design-time migration
        /// </summary>


        //public PortfolioContext() : base()
        //{
        //}

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlite("Data Source=" + App.appDataPath + "\\" + App.DbName);
        //}


        public PortfolioContext(DbContextOptions<PortfolioContext> connection) : base(connection) { }



        public DbSet<Coin> Coins  { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Narrative> Narratives { get; set; }
        public DbSet<Mutation> Mutations { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<PriceLevel> PriceLevels { get; set; }




        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new TransactionEntityTypeConfiguration());
            builder.ApplyConfiguration(new AccountEntityTypeConfiguration());
            builder.ApplyConfiguration(new NarrativeEntityTypeConfiguration());
            builder.ApplyConfiguration(new AssetEntityTypeConfiguration());
            builder.ApplyConfiguration(new CoinEntityTypeConfiguration());
            builder.ApplyConfiguration(new MutationEntityTypeConfiguration());
            builder.ApplyConfiguration(new PriceLevelEntityTypeConfiguration());

        }

    }
}
