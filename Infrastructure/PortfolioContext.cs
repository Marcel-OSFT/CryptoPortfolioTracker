using CryptoPortfolioTracker.Infrastructure.EntityConfigurations;
using CryptoPortfolioTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoPortfolioTracker.Infrastructure
{
    public class PortfolioContext : DbContext
    {

        
        public PortfolioContext(DbContextOptions<PortfolioContext> connection) : base(connection) { }

        //public PortfolioContext() : base()
        //{
        //}

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlite("Data Source=C:\\Users\\marce\\AppData\\Local\\CryptoPortfolioTracker\\sqlCPT.db");
        //}


        public DbSet<Coin> Coins { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Mutation> Mutations { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new TransactionEntityTypeConfiguration());
            builder.ApplyConfiguration(new AccountEntityTypeConfiguration());
            builder.ApplyConfiguration(new AssetEntityTypeConfiguration());
            builder.ApplyConfiguration(new CoinEntityTypeConfiguration());
            builder.ApplyConfiguration(new MutationEntityTypeConfiguration());
        }

    }
}
