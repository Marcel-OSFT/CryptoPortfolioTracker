using CryptoPortfolioTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public interface IDashboardService
    {
        public Task StorePriceLevelsToContext(Coin coin);
        public Coin GetPriceLevelsFromContext(Coin coin);
        
    }
}
