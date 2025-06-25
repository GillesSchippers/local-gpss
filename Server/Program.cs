using GPSS_Server.Datastore;
using GPSS_Server.Utils;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static void Main(string[] args)
    {
        var config = ConfigLoader.LoadOrCreateConfig();

        var logger = LoggerFactory.Create(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        }).CreateLogger("Startup");

        logger.LogInformation("Starting Local GPSS...");
        Helpers.Init(logger);

        var builder = WebApplication.CreateBuilder(args);

        var connectionString = $"Server={config.MySqlHost};Port={config.MySqlPort};User={config.MySqlUser};Password={config.MySqlPassword};Database={config.MySqlDatabase};";
        builder.Services.AddDbContext<GpssDbContext>(options => options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            )
        );

        builder.Services.AddControllers();
        builder.Services.AddScoped<Database>();
        builder.Services.AddHostedService<IntegrityChecker>();

        var app = builder.Build();

        app.UseRouting();
        app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GpssDbContext>();
            db.Database.Migrate();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        try
        {
            app.Run($"http://{config.Ip}:{config.Port}/");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to start Local GPSS due to an unexpected exception.");
            Environment.Exit(1);
        }
    }
}