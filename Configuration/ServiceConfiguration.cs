using PocketFence_Simple.Services;
using PocketFence_Simple.Services.AI;
using PocketFence_Simple.Services.iOS;
using PocketFence_Simple.Hubs;
using System.Runtime.InteropServices;
using Microsoft.Extensions.ObjectPool;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PocketFence.Security;
using PocketFence.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace PocketFence_Simple.Configuration;

/// <summary>
/// Service configuration and registration for dependency injection
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configure all application services
    /// </summary>
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Core web services
        ConfigureWebServices(services);
        
        // Authentication and security
        ConfigureAuthentication(services, configuration);
        
        // Core application services
        ConfigureApplicationServices(services);
        
        // Background services
        ConfigureBackgroundServices(services);
        
        // External services
        ConfigureExternalServices(services, configuration);
        
        // Health checks and monitoring
        ConfigureHealthChecks(services);
        
        // API documentation
        ConfigureApiDocumentation(services);
    }

    private static void ConfigureWebServices(IServiceCollection services)
    {
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

        // Configure CORS
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

        // Rate limiting
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("GlobalLimit", limiterOptions =>
            {
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.PermitLimit = 100;
                limiterOptions.QueueLimit = 10;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
        });
    }

    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret = configuration["Authentication:JwtSecret"] ?? 
                       Environment.GetEnvironmentVariable("JWT_SECRET") ??
                       throw new InvalidOperationException("JWT Secret not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Authentication:JwtIssuer"],
                    ValidAudience = configuration["Authentication:JwtAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtSecret))
                };
            });

        services.AddAuthorization();
    }

    private static void ConfigureApplicationServices(IServiceCollection services)
    {
        // Core PocketFence services
        services.AddSingleton<HotspotService>();
        services.AddSingleton<NetworkTrafficService>();
        services.AddSingleton<ContentFilterService>();
        services.AddSingleton<INetworkModeService, NetworkModeService>();
        
        // AI services
        services.AddSingleton<UnifiedAIService>();
        services.AddSingleton<SimpleGeofenceService>();
        services.AddSingleton<AINotificationService>();
        
        // iOS services
        services.AddSingleton<iOSHotspotHelper>();
        
        // Caching and memory management
        services.AddMemoryCache();
        services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
    }

    private static void ConfigureBackgroundServices(IServiceCollection services)
    {
        services.AddHostedService<UnifiedAIService>();
        services.AddHostedService<SystemMonitoringService>();
        services.AddHostedService<SelfHealingService>();
        services.AddHostedService<AutoUpdateService>();
    }

    private static void ConfigureExternalServices(IServiceCollection services, IConfiguration configuration)
    {
        // HTTP client factory
        services.AddHttpClient();
        services.AddHttpClient("UpdateService", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "PocketFence-Simple/2.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient("StripeClient", client =>
        {
            client.BaseAddress = new Uri("https://api.stripe.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "PocketFence-Simple/2.0");
        });
    }

    private static void ConfigureHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<HealthCheckService>("system_health")
            .AddCheck("memory", () =>
            {
                var memoryUsage = GC.GetTotalMemory(false);
                var maxMemory = 1024 * 1024 * 1024; // 1GB
                return memoryUsage < maxMemory 
                    ? HealthCheckResult.Healthy($"Memory usage: {memoryUsage / 1024 / 1024} MB")
                    : HealthCheckResult.Unhealthy($"High memory usage: {memoryUsage / 1024 / 1024} MB");
            });
    }

    private static void ConfigureApiDocumentation(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v2", new()
            {
                Title = "PocketFence Simple API",
                Version = "v2.0",
                Description = "AI-powered network security and parental control system with advanced threat detection.",
                Contact = new() 
                { 
                    Name = "PocketFence Team",
                    Email = "support@pocketfence.com"
                },
                License = new()
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            var xmlFile = Path.Combine(AppContext.BaseDirectory, "PocketFence-Simple.xml");
            if (File.Exists(xmlFile))
            {
                c.IncludeXmlComments(xmlFile, true);
            }
        });
    }
}