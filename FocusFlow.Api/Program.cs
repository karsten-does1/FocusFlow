using FocusFlow.Api.ApiSupport.Options;
using FocusFlow.Infrastructure.Dependencies;
using FocusFlow.Infrastructure.Services.TokenRefresh;

namespace FocusFlow.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile(
                "appsettings.Local.json",
                optional: true,
                reloadOnChange: true);

            // Add services to the container.
            builder.Services.AddFocusFlowInfrastructure(builder.Configuration);

            builder.Services.AddHostedService<TokenRefreshBackgroundService>();


            builder.Services.Configure<GoogleOAuthOptions>(
              builder.Configuration.GetSection("GoogleOAuth"));

            builder.Services.Configure<MicrosoftOAuthOptions>(
             builder.Configuration.GetSection(MicrosoftOAuthOptions.SectionName));

            builder.Services.AddHttpClient();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
