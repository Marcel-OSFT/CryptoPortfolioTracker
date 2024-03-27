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
