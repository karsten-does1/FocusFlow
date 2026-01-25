using System;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.App.Services;
using FocusFlow.App.ViewModels;
using FocusFlow.App.ViewModels.Emails;
using FocusFlow.App.ViewModels.Tasks;
using FocusFlow.App.ViewModels.Settings;

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

                _ = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                // Regular API clients
                services.AddHttpClient<IEmailService, EmailApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<IEmailAccountService, EmailAccountApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<ISummaryService, SummaryApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<ITaskService, TaskApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<IReminderService, ReminderApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<EmailSyncApi>(c => c.BaseAddress = new Uri(apiBase));

                // AI client
                services.AddHttpClient<IAiService, AiApi>(c => c.BaseAddress = new Uri(apiBase));

                // Dialog service (MVVM-friendly)
                services.AddSingleton<IDialogService, DialogService>();

                // ViewModels
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<TasksViewModel>();
                services.AddTransient<EmailsViewModel>();
                services.AddTransient<RemindersViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<MainViewModel>();
            });

            HostApp = builder.Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await HostApp.StartAsync();
            base.OnStartup(e);

            var mainViewModel = HostApp.Services.GetRequiredService<MainViewModel>();
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
