using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FocusFlow.Infrastructure.Persistence
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FocusFlowDbContext>
    {
        public FocusFlowDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FocusFlowDbContext>();

            optionsBuilder.UseSqlite("Data Source=focusflow.db");

            return new FocusFlowDbContext(optionsBuilder.Options);
        }
    }
}

