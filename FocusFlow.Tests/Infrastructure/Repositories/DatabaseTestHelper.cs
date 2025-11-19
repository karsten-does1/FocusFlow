using FocusFlow.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

        var context = new FocusFlowDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}

