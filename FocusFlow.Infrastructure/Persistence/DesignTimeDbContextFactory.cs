using System.Collections.Generic;
using System.IO;
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
            //  Lees appsettings van API (zodat migrations dezelfde DB gebruiken)
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "FocusFlow.Api"))
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Local.json", optional: true)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Encryption:Key", "DesignTimeDummyKeyForMigrationsOnly1234567890" }
                })
                .Build();

            var cs = configuration.GetConnectionString("FocusFlow")
                     ?? "Data Source=../FocusFlow.Infrastructure/focusflow.db";

            cs = NormalizeSqliteConnectionString(cs);

            var optionsBuilder = new DbContextOptionsBuilder<FocusFlowDbContext>();
            optionsBuilder.UseSqlite(cs);

            var encryptionService = new EncryptionService(configuration);

            return new FocusFlowDbContext(optionsBuilder.Options, encryptionService);
        }

        private static string NormalizeSqliteConnectionString(string cs)
        {
            const string prefix = "Data Source=";
            if (!cs.TrimStart().StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return cs;

            var pathPart = cs.Trim().Substring(prefix.Length).Trim().Trim('"');
            if (Path.IsPathRooted(pathPart))
                return cs;

            var absPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), pathPart));
            return $"{prefix}{absPath}";
        }
    }
}
