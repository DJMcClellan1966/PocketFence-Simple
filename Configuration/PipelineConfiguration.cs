using PocketFence_Simple.Hubs;
using PocketFence.Middleware;
using Microsoft.AspNetCore.RateLimiting;

namespace PocketFence_Simple.Configuration;

/// <summary>
/// Application pipeline and middleware configuration
/// </summary>
public static class PipelineConfiguration
{
    /// <summary>
    /// Configure the application request pipeline
    /// </summary>
    public static async Task<WebApplication> ConfigureApplicationAsync(this WebApplication app)
    {
        // Development vs Production pipeline
        ConfigureEnvironmentSpecificPipeline(app);
        
        // Security and middleware
        ConfigureSecurityPipeline(app);
        
        // Static files and routing
        ConfigureStaticFilesAndRouting(app);
        
        // API endpoints
        ConfigureApiEndpoints(app);
        
        // Initialize services
        await InitializeServicesAsync(app);
        
        return app;
    }

    private static void ConfigureEnvironmentSpecificPipeline(WebApplication app)
    {
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
                c.DisplayRequestDuration();
            });
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
            // Force HTTPS in production
            app.UseHttpsRedirection();
        }
    }

    private static void ConfigureSecurityPipeline(WebApplication app)
    {
        // Custom security middleware
        app.UseMiddleware<SecurityHardeningMiddleware>();
        
        // Rate limiting
        app.UseRateLimiter();
        
        // CORS
        app.UseCors();
        
        // Authentication and authorization
        app.UseAuthentication();
        app.UseAuthorization();
    }

    private static void ConfigureStaticFilesAndRouting(WebApplication app)
    {
        // Static files with caching for performance
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                // Cache static files for 1 hour
                ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
                // Add security headers for static files
                ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            }
        });

        // Health checks endpoint
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new
                    {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        description = x.Value.Description,
                        duration = x.Value.Duration
                    }),
                    duration = report.TotalDuration
                };
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            }
        });

        app.UseRouting();
    }

    private static void ConfigureApiEndpoints(WebApplication app)
    {
        // API controllers with rate limiting
        app.MapControllers().RequireRateLimiting("GlobalLimit");
        
        // SignalR hub with optimized settings
        app.MapHub<DashboardHub>("/hub/dashboard", options =>
        {
            options.TransportMaxBufferSize = 1024 * 1024;
            options.ApplicationMaxBufferSize = 1024 * 1024;
            options.LongPolling.PollTimeout = TimeSpan.FromSeconds(30);
            options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(5);
        });

        // API versioning and documentation endpoints
        app.MapGet("/api/version", () => new { 
            version = "2.0", 
            name = "PocketFence-Simple", 
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName
        })
        .WithName("GetVersion")
        .WithOpenApi();

        // Root redirect to dashboard
        app.MapGet("/", () => Results.Redirect("/dashboard"))
            .ExcludeFromDescription();

        // SPA fallback for client-side routing
        app.MapFallbackToFile("index.html");
    }

    private static async Task InitializeServicesAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            // Initialize AI services
            var aiService = scope.ServiceProvider.GetRequiredService<UnifiedAIService>();
            await aiService.InitializeAsync();
            
            // Initialize network services
            var networkService = scope.ServiceProvider.GetRequiredService<NetworkTrafficService>();
            await networkService.InitializeAsync();
            
            logger.LogInformation("✅ All services initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to initialize services");
            throw;
        }
    }
}