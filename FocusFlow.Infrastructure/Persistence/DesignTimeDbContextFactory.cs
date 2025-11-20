using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FocusFlow.Infrastructure.Persistence
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FocusFlowDbContext>
    {
        public FocusFlowDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FocusFlowDbContext>();
            optionsBuilder.UseSqlite("Data Source=focusflow.db");

            // Create a dummy configuration for design-time (migrations, etc.)
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Encryption:Key", "DesignTimeDummyKeyForMigrationsOnly1234567890" }
                })
                .Build();

            var encryptionService = new EncryptionService(configuration);

            return new FocusFlowDbContext(optionsBuilder.Options, encryptionService);
        }
    }
}

