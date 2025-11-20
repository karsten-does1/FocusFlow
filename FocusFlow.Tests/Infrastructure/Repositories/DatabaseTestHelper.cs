using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Infrastructure.Persistence;
using FocusFlow.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FocusFlow.Tests.Infrastructure.Repositories;

public static class DatabaseTestHelper
{
    public static FocusFlowDbContext CreateInMemoryDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<FocusFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Encryption:Key", "TestDummyKeyForUnitTestsOnly1234567890" }
            })
            .Build();

        var encryptionService = new EncryptionService(configuration);

        var context = new FocusFlowDbContext(options, encryptionService);
        context.Database.EnsureCreated();
        return context;
    }
}

