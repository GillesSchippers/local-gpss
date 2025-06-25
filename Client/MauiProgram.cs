using CommunityToolkit.Maui;
using GPSS_Client.Services;
using Microsoft.Extensions.Logging;

namespace GPSS_Client
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder.Services.AddSingleton(ConfigService.Load());
            builder.Services.AddSingleton<ConfigService>();
            builder.Services.AddSingleton<PkhexService>();
            builder.Services.AddSingleton<ApiService>();

            builder.Services.AddTransient<AppShell>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ConfigPage>();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
