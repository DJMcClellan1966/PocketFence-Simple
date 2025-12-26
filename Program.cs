using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using PocketFence_Simple.Services;
using PocketFence_Simple.Hubs;
using PocketFence_Simple.Interfaces;
using System.Runtime.InteropServices;

namespace PocketFence_Simple;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure services for cross-platform operation
        ConfigureServices(builder.Services, builder.Configuration);

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // Add Windows Service support if on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            builder.Host.UseWindowsService();
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline
        ConfigureApp(app);

        // Get the server URLs
        var urls = app.Configuration["urls"] ?? "http://0.0.0.0:8080;https://0.0.0.0:8443";
        
        Console.WriteLine("🚀 PocketFence Dashboard Starting...");
        Console.WriteLine($"📊 Dashboard URL: {GetDashboardUrl(urls)}");
        Console.WriteLine("🌐 Accessible from any device on your network");
        Console.WriteLine("📱 Mobile-friendly responsive design");
        Console.WriteLine();

        await app.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add ASP.NET Core services
        services.AddControllers();
        services.AddSignalR(); // Real-time updates
        services.AddHttpClient();
        
        // Add CORS for cross-platform access
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        // Register application services
        services.AddSingleton<ContentFilterService>();
        services.AddSingleton<HotspotService>();
        services.AddSingleton<NetworkTrafficService>();
        
        // Platform-specific network service  
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<INetworkService, Platforms.Windows.WindowsNetworkService>();
        }
    }

    private static void ConfigureApp(WebApplication app)
    {
        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseCors();
        
        // Serve static files (our web dashboard)
        app.UseStaticFiles();

        // API endpoints
        app.MapControllers();
        app.MapHub<DashboardHub>("/dashboardHub");

        // Default route to dashboard
        app.MapFallbackToFile("index.html");
    }

    private static string GetDashboardUrl(string urls)
    {
        var urlList = urls.Split(';');
        var httpUrl = urlList.FirstOrDefault(u => u.StartsWith("http://")) ?? urlList.First();
        
        // Replace 0.0.0.0 with local IP for display
        if (httpUrl.Contains("0.0.0.0"))
        {
            var localIp = GetLocalIPAddress();
            httpUrl = httpUrl.Replace("0.0.0.0", localIp);
        }
        
        return httpUrl;
    }

    private static string GetLocalIPAddress()
    {
        try
        {
            using var socket = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.InterNetwork, 
                System.Net.Sockets.SocketType.Dgram, 0);
            
            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as System.Net.IPEndPoint;
            return endPoint?.Address.ToString() ?? "localhost";
        }
        catch
        {
            return "localhost";
        }
    }
}
