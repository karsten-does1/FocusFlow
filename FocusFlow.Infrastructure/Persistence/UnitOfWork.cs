using FocusFlow.Core.Application.Contracts.Persistence;

namespace FocusFlow.Infrastructure.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly FocusFlowDbContext _dbContext;

        public UnitOfWork(FocusFlowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _dbContext.SaveChangesAsync(ct);
        }
    }
}

