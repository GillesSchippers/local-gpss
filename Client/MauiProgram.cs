namespace GPSS_Client
{
    using CommunityToolkit.Maui;
    using GPSS_Client.Config;
    using GPSS_Client.Services;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Defines the <see cref="MauiProgram" />.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// The CreateMauiApp.
        /// </summary>
        /// <returns>The <see cref="MauiApp"/>.</returns>
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder.Services.AddSingleton<ConfigHolder>(sp => new ConfigHolder(ConfigService.Load()));
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
