using System;
using System.IO;
using System.Text.Json;
using FocusFlow.Core.Application.Contracts.Persistence;
using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Infrastructure.Persistence;
using FocusFlow.Infrastructure.Repositories;
using FocusFlow.Infrastructure.Services;
using FocusFlow.Infrastructure.Services.Gmail;
using FocusFlow.Infrastructure.Services.Outlook;
using FocusFlow.Infrastructure.Services.TokenRefresh;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FocusFlow.Infrastructure.Dependencies
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFocusFlowInfrastructure(
            this IServiceCollection services, IConfiguration cfg)
        {
            var cs = cfg.GetConnectionString("FocusFlow")
                     ?? "Data Source=../FocusFlow.Infrastructure/focusflow.db";

            cs = NormalizeSqliteConnectionString(cs);

            services.AddSingleton<IEncryptionService, EncryptionService>();

            services.AddDbContext<FocusFlowDbContext>((sp, options) =>
            {
                options.UseSqlite(cs);
            });

            services.AddHttpClient("GmailApi", client =>
            {
                client.BaseAddress = new Uri("https://gmail.googleapis.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient("OutlookApi", client =>
            {
                client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IEmailRepository, EmailRepository>();
            services.AddScoped<IEmailAccountRepository, EmailAccountRepository>();
            services.AddScoped<ISummaryRepository, SummaryRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IReminderRepository, ReminderRepository>();

            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailAccountService, EmailAccountService>();
            services.AddScoped<ISummaryService, SummaryService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IReminderService, ReminderService>();

            services.AddScoped<IEmailMessageParser<JsonElement>, GmailMessageParser>();
            services.AddScoped<OutlookMessageParser>();

            services.AddScoped<IGmailSyncService, GmailSyncService>();
            services.AddScoped<IOutlookSyncService, OutlookSyncService>();

            services.AddScoped<GmailTokenRefreshService>();
            services.AddScoped<OutlookTokenRefreshService>();

            //services.AddHostedService<TokenRefreshBackgroundService>();

            services.AddHttpClient<IAiService, PythonAiService>();

            return services;
        }

        private static string NormalizeSqliteConnectionString(string cs)
        {
            const string prefix = "Data Source=";

            if (!cs.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return cs;

            var pathPart = cs.Trim().Substring(prefix.Length).Trim().Trim('"');

            if (Path.IsPathRooted(pathPart))
                return cs;

            var absPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), pathPart));

            return $"{prefix}{absPath}";
        }
    }
}
