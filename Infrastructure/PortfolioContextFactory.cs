
using Microsoft.EntityFrameworkCore;

namespace CryptoPortfolioTracker.Infrastructure
{
    public interface IPortfolioContextFactory
    {
        PortfolioContext Create(string connectionString);
    }

    public class PortfolioContextFactory : IPortfolioContextFactory
    {
        private readonly DbContextOptions<PortfolioContext> _options;

        public PortfolioContextFactory(DbContextOptions<PortfolioContext> options)
        {
            _options = options;
        }

        public PortfolioContext Create(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PortfolioContext>(_options)
                .UseSqlite($"{connectionString};Pooling=False");

            return new PortfolioContext(optionsBuilder.Options);
        }
    }





}
