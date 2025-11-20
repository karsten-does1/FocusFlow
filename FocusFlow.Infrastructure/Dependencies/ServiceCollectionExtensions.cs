using FocusFlow.Core.Application.Contracts.Persistence;
using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Infrastructure.Persistence;
using FocusFlow.Infrastructure.Repositories;
using FocusFlow.Infrastructure.Services;
using FocusFlow.Infrastructure.Services.Gmail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;

namespace FocusFlow.Infrastructure.Dependencies
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFocusFlowInfrastructure(
            this IServiceCollection services, IConfiguration cfg)
        {
            var cs = cfg.GetConnectionString("FocusFlow") ?? "Data Source=../FocusFlow.Infrastructure/focusflow.db";

            
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

            services.AddScoped<IGmailSyncService, GmailSyncService>();


            return services;
        }
    }
}