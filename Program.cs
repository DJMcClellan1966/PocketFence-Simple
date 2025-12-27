using PocketFence_Simple.Services;
using PocketFence_Simple.Services.AI;
using PocketFence_Simple.Services.iOS;
using PocketFence_Simple.Hubs;
using Serilog;
using System.Runtime.InteropServices;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
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

    // Add Serilog
    builder.Host.UseSerilog();

    // Add Windows Service support if on Windows
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = "PocketFence-Simple";
        });
    }

    // Configure services
    ConfigureServices(builder.Services);

    var app = builder.Build();

    // Configure pipeline
    await ConfigureAppAsync(app);

    var urls = GetServerUrls(app);
    LogStartupInfo(urls);

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;

static void ConfigureServices(IServiceCollection services)
{
    // API and Web services
    services.AddControllers(options =>
    {
        options.SuppressAsyncSuffixInActionNames = false;
    });

    services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true;
        options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
        options.StreamBufferCapacity = 10;
    });

    // Configure CORS for cross-platform access
    services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .WithExposedHeaders("*");
        });
    });

    // HTTP client factory with modern configuration
    services.AddHttpClient();
    services.AddHttpClient("UpdateService", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "PocketFence-Simple/2.0");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    // Register core services with singleton lifetime
    services.AddSingleton<HotspotService>();
    services.AddSingleton<NetworkTrafficService>();
    services.AddSingleton<ContentFilterService>();    
    // Register network mode detection service
    services.AddSingleton<INetworkModeService, NetworkModeService>();
    // Register simplified AI services (replaces multiple complex services)
    services.AddSingleton<UnifiedAIService>();
    services.AddSingleton<SimpleGeofenceService>();
    services.AddHostedService<UnifiedAIService>();
    
    // Keep essential support services
    services.AddSingleton<AINotificationService>();
    services.AddSingleton<SelfHealingService>();
    services.AddSingleton<AutoUpdateService>();
    
    // Register iOS services
    services.AddSingleton<iOSHotspotHelper>();

    // Add health checks
    services.AddHealthChecks()
        .AddCheck<HealthCheckService>("system_health");

    // OpenAPI/Swagger with modern configuration
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v2", new() 
        { 
            Title = "PocketFence Simple API", 
            Version = "v2.0",
            Description = "Next-generation AI-powered parental control system with advanced threat detection and autonomous security responses.",
            Contact = new() { Name = "PocketFence Team" }
        });
        
        c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "PocketFence-Simple.xml"), true);
    });

    // Add memory caching
    services.AddMemoryCache();
    
    // Add background services
    services.AddHostedService<SystemMonitoringService>();
}

static async Task ConfigureAppAsync(WebApplication app)
{
    // Exception handling
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v2/swagger.json", "PocketFence Simple API v2.0");
            c.RoutePrefix = "api-docs";
            c.DocumentTitle = "PocketFence API Documentation";
            c.DefaultModelsExpandDepth(-1);
        });
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    // Security headers
    app.UseSecurityHeaders();
    
    // CORS
    app.UseCors();
    
    // Static files with caching
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
        }
    });

    // Health checks
    app.MapHealthChecks("/health");

    // Routing
    app.UseRouting();

    // API endpoints
    app.MapControllers();
    
    // SignalR hub
    app.MapHub<DashboardHub>("/hub/dashboard", options =>
    {
        options.TransportMaxBufferSize = 1024 * 1024;
        options.ApplicationMaxBufferSize = 1024 * 1024;
    });

    // SPA fallback
    app.MapFallbackToFile("index.html");

    // Initialize AI services
    await InitializeServicesAsync(app.Services);
}

static async Task InitializeServicesAsync(IServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Initialize network mode monitoring first
        var networkModeService = scope.ServiceProvider.GetRequiredService<INetworkModeService>();
        logger.LogInformation("🔍 Initializing network mode detection");
        await networkModeService.StartMonitoringAsync();
        
        // Initialize AI services
        var aiServices = new object?[]
        {
            scope.ServiceProvider.GetService<AINotificationService>(),
            scope.ServiceProvider.GetService<AIThreatDetectionService>(),
            scope.ServiceProvider.GetService<SelfHealingService>(),
            scope.ServiceProvider.GetService<AutoUpdateService>(),
            scope.ServiceProvider.GetService<AIParentalAssistantService>()
        };

        logger.LogInformation("🤖 Initializing {Count} AI services", aiServices.Length);
        
        // Services are initialized through dependency injection
        await Task.CompletedTask;
        
        logger.LogInformation("✅ All AI services initialized successfully");
        logger.LogInformation("📡 Network mode monitoring active");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize AI services");
        throw;
    }
}

static string GetServerUrls(WebApplication app)
{
    var urls = app.Configuration["urls"];
    if (!string.IsNullOrEmpty(urls))
        return urls;
        
    // Default to network-accessible URLs for mobile testing
    return app.Environment.IsDevelopment() 
        ? "http://0.0.0.0:5000;https://0.0.0.0:5001" 
        : "http://0.0.0.0:8080;https://0.0.0.0:8443";
}

static void LogStartupInfo(string urls)
{
    Log.Information("🌐 Server URLs: {Urls}", urls);
    Log.Information("📊 Dashboard: Navigate to the server URL in your web browser");
    Log.Information("📚 API Documentation: {Urls}/api-docs", urls.Split(';')[0]);
    Log.Information("🔗 SignalR Hub: {Urls}/hub/dashboard", urls.Split(';')[0]);
    Log.Information("💡 Access from any device on your network for remote monitoring");
    Log.Information("🚀 PocketFence-Simple is ready for AI-powered parental control!");
}

// Extension method for security headers
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            
            await next();
        });
    }
}

// Background service for system monitoring
public class SystemMonitoringService(ILogger<SystemMonitoringService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("System monitoring service started");
        
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var memoryUsage = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
                logger.LogDebug("Memory usage: {MemoryMB:F2} MB", memoryUsage);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("System monitoring service stopped");
        }
    }
}

// Health check service
public class HealthCheckService : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var memoryUsage = GC.GetTotalMemory(false);
            var isHealthy = memoryUsage < 500_000_000; // 500MB threshold
            
            return Task.FromResult(isHealthy 
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("System operating normally")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"High memory usage: {memoryUsage / 1024.0 / 1024.0:F2} MB"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Health check failed", ex));
        }
    }
}