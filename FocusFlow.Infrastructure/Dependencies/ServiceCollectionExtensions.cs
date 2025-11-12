using FocusFlow.Core.Application.Contracts.Repositories;
using FocusFlow.Infrastructure.Persistence;
using FocusFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FocusFlow.Infrastructure.Dependencies
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFocusFlowInfrastructure(
            this IServiceCollection services, IConfiguration cfg)
        {
            var cs = cfg.GetConnectionString("FocusFlow") ?? "Data Source=focusflow.db";
            services.AddDbContext<FocusFlowDbContext>(opt => opt.UseSqlite(cs));

            services.AddScoped<IEmailRepository, EmailRepository>();
            services.AddScoped<ISummaryRepository, SummaryRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IReminderRepository, ReminderRepository>();

            return services;
        }
    }
}