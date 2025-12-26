using Microsoft.Extensions.Logging;
using PocketFence_Simple.Services;
using PocketFence_Simple.Interfaces;
using CommunityToolkit.Maui;
#if WINDOWS
using PocketFence_Simple.Platforms.Windows;
#endif

namespace PocketFence_Simple;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register platform-specific services
#if WINDOWS
        builder.Services.AddSingleton<INetworkService, WindowsNetworkService>();
        builder.Services.AddSingleton<ISystemUtilsService, WindowsSystemUtilsService>();
#elif ANDROID
        builder.Services.AddSingleton<INetworkService, AndroidNetworkService>();
        builder.Services.AddSingleton<ISystemUtilsService, AndroidSystemUtilsService>();
#elif IOS
        builder.Services.AddSingleton<INetworkService, iOSNetworkService>();
        builder.Services.AddSingleton<ISystemUtilsService, iOSSystemUtilsService>();
#elif MACCATALYST
        builder.Services.AddSingleton<INetworkService, MacNetworkService>();
        builder.Services.AddSingleton<ISystemUtilsService, MacSystemUtilsService>();
#else
        builder.Services.AddSingleton<INetworkService, GenericNetworkService>();
        builder.Services.AddSingleton<ISystemUtilsService, GenericSystemUtilsService>();
#endif

        // Register shared services
        builder.Services.AddSingleton<ContentFilterService>();
        builder.Services.AddSingleton<NetworkTrafficService>();
        builder.Services.AddTransient<MainPage>();

        builder.Services.AddLogging();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}