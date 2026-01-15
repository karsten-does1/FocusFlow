using FocusFlow.Infrastructure.Persistence;

namespace FocusFlow.Tests.Infrastructure.Repositories;

public abstract class RepositoryTestBase : IDisposable
{
    protected readonly FocusFlowDbContext DbContext;

    protected RepositoryTestBase()
    {
        DbContext = DatabaseTestHelper.CreateInMemoryDbContext();
    }

    public void Dispose()
    {
        DbContext.Dispose();
    }

    protected async Task<T> AddToDatabaseAsync<T>(T entity) where T : class
    {
        DbContext.Add(entity);
        await DbContext.SaveChangesAsync();
        return entity;
    }

    protected async Task<IEnumerable<T>> AddRangeToDatabaseAsync<T>(IEnumerable<T> entities) where T : class
    {
        DbContext.AddRange(entities);
        await DbContext.SaveChangesAsync();
        return entities;
    }
}

