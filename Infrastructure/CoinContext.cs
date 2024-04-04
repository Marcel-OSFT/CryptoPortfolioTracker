using CryptoPortfolioTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoPortfolioTracker.Infrastructure
{
    public class CoinContext : DbContext
    {
        public CoinContext(DbContextOptions<CoinContext> connection) : base(connection) { }

        //public CoinContext() : base()
        //{
        //}

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlite("Data Source=C:\\Users\\marce\\source\\repos\\CryptoPortfolioTracker\\sqlCPT.db");
        //}

        public DbSet<Coin> Coins { get; set; }

    }
}
