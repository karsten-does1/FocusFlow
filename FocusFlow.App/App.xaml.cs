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
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
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
                var apiBase = ctx.Configuration["Api:BaseUrl"] ?? "https://localhost:7279";

                services.AddHttpClient<IEmailService, EmailApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<ISummaryService, SummaryApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<ITaskService, TaskApi>(c => c.BaseAddress = new Uri(apiBase));
                services.AddHttpClient<IReminderService, ReminderApi>(c => c.BaseAddress = new Uri(apiBase));

               
            });

            HostApp = builder.Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await HostApp.StartAsync();
            base.OnStartup(e);
            // var win = new MainWindow { DataContext = HostApp.Services.GetRequiredService<MainViewModel>() };
            // win.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await HostApp.StopAsync();
            HostApp.Dispose();
            base.OnExit(e);
        }
    }
}
    