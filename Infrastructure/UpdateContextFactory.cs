
using Microsoft.EntityFrameworkCore;

namespace CryptoPortfolioTracker.Infrastructure
{
    public interface IUpdateContextFactory
    {
        UpdateContext Create(string connectionString);
    }

    public class UpdateContextFactory : IUpdateContextFactory
    {
        private readonly DbContextOptions<UpdateContext> _options;

        public UpdateContextFactory(DbContextOptions<UpdateContext> options)
        {
            _options = options;
        }

        public UpdateContext Create(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UpdateContext>(_options)
                .UseSqlite($"{connectionString};Pooling=False");

            return new UpdateContext(optionsBuilder.Options);
        }
    }





}
