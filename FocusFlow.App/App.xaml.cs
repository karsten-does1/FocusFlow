using System;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FocusFlow.Core.Application.Contracts.Services;
using System.Net.Http;
using FocusFlow.App.Services;

namespace FocusFlow.App
{
 
    public partial class App : Application
    {
        public static IHost HostApp { get; private set; } = null!;

        public App()
        {
            var builder = Host.CreateDefaultBuilder();

            builder.ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory)
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            });

            builder.ConfigureServices((ctx, services) =>
            {
                var apiBase = ctx.Configuration["Api:BaseUrl"] ?? "https://localhost:7248";

                services.AddHttpClient<IEmailService, EmailApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<IEmailAccountService, EmailAccountApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<ISummaryService, SummaryApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<ITaskService, TaskApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<IReminderService, ReminderApi>(c => c.BaseAddress = new Uri(apiBase));

                
                services.AddTransient<ViewModels.DashboardViewModel>();
                services.AddTransient<ViewModels.TasksViewModel>();
                services.AddTransient<ViewModels.EmailsViewModel>();
                services.AddTransient<ViewModels.RemindersViewModel>();
                services.AddTransient<ViewModels.SettingsViewModel>();
                services.AddTransient<ViewModels.MainViewModel>();
            });

            HostApp = builder.Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await HostApp.StartAsync();
            base.OnStartup(e);

            var mainViewModel = HostApp.Services.GetRequiredService<ViewModels.MainViewModel>();
            var win = new MainWindow(mainViewModel);
            win.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await HostApp.StopAsync();
            HostApp.Dispose();
            base.OnExit(e);
        }
    }
}