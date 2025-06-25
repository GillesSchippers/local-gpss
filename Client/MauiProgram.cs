using Microsoft.Extensions.Logging;
using GPSS_Client.Config;
using GPSS_Client.Services;
using CommunityToolkit.Maui;


namespace GPSS_Client
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // Load config and register as singleton
            var config = ConfigService.Load();
            builder.Services.AddSingleton(config);

            // Register ApiService as singleton, using config
            //builder.Services.AddSingleton<ApiService>();

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
