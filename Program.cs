using PocketFence_Simple.Configuration;
using Serilog;
using System.Runtime.InteropServices;

namespace PocketFence_Simple;

/// <summary>
/// PocketFence-Simple Application Entry Point
/// AI-powered network security and parental control system
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure Serilog for structured logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/pocketfence-.txt", 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .Enrich.FromLogContext()
            .CreateLogger();

        try
        {
            Log.Information("🚀 Starting PocketFence-Simple v2.0 with AI enhancements");
            
            var builder = WebApplication.CreateBuilder(args);
            
            // Configure logging
            builder.Host.UseSerilog();
            
            // Add Windows Service support
            ConfigureHostingModel(builder);
            
            // Configure all services
            builder.Services.ConfigureServices(builder.Configuration);
            
            // Build application
            var app = builder.Build();
            
            // Configure pipeline
            await app.ConfigureApplicationAsync();
            
            // Log startup information
            LogStartupInfo(app);
            
            // Run application
            await app.RunAsync();
            
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "❌ Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
    
    /// <summary>
    /// Configure hosting model based on platform
    /// </summary>
    private static void ConfigureHostingModel(WebApplicationBuilder builder)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            builder.Host.UseWindowsService(options =>
            {
                options.ServiceName = "PocketFence-Simple";
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            builder.Host.UseSystemd();
        }
    }
    
    /// <summary>
    /// Log application startup information
    /// </summary>
    private static void LogStartupInfo(WebApplication app)
    {
        var urls = app.Environment.IsDevelopment() 
            ? "https://localhost:5001;http://localhost:5000" 
            : "http://0.0.0.0:8080";
            
        Log.Information("✅ PocketFence-Simple started successfully");
        Log.Information("🌐 Server URLs: {Urls}", urls);
        Log.Information("🏠 Environment: {Environment}", app.Environment.EnvironmentName);
        
        if (app.Environment.IsDevelopment())
        {
            Log.Information("📚 API Documentation: https://localhost:5001/api-docs");
            Log.Information("💖 Health Check: https://localhost:5001/health");
        }
        
        Log.Information("📊 Dashboard available at: {Urls}/dashboard", urls);
        Log.Information("🤖 AI Services: Enhanced threat detection active");
        Log.Information("🛡️  Security: Advanced protection enabled");
    }
}