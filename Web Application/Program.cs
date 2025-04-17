using PlayHT.Services;
using Microsoft.AspNetCore.Mvc;

namespace PlayHT
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddRazorPages();
            builder.Services.AddAntiforgery(options => {
                options.HeaderName = "RequestVerificationToken";
            });

            // Add configuration
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddEnvironmentVariables();

            // Configure PlayHt API credentials
            builder.Services.AddHttpClient<IPlayHtService, PlayHtService>();

            // Add health checks
            builder.Services.AddHealthChecks().AddCheck<PlayHtHealthCheck>("PlayHT API");

            // Add distributed memory cache and session support
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add scoped services
            builder.Services.AddScoped<IStateManagementService, StateManagementService>();
            builder.Services.AddScoped<IAgentMonitoringService, AgentMonitoringService>();

            var app = builder.Build();

            app.MapHealthChecks("/health");

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Add session middleware before routing
            app.UseSession();

            app.UseRouting();
            app.UseAuthorization();
            app.MapRazorPages();

            // Add this line to redirect root to the VoiceAgent/Create page
            app.MapGet("/", context => {
                context.Response.Redirect("/VoiceAgent/Create");
                return Task.CompletedTask;
            });

            app.Run();
        }
    }
}