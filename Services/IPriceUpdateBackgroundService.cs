using CryptoPortfolioTracker.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public interface IPriceUpdateBackgroundService
    {
        public void Start();
        public void Stop();
        
    }
}
