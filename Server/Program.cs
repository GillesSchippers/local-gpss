using GPSS_Server.Config;
using GPSS_Server.Datastore;
using GPSS_Server.Datastore.Checks;
using GPSS_Server.Utils;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

#if !DEBUG
using MySqlConnector;
using System.Net;
#endif

/// <summary>
/// Defines the <see cref="Program" />.
/// </summary>
internal class Program
{
    /// <summary>
    /// Defines the Config.
    /// </summary>
    private static readonly ConfigHolder Config = new();

    /// <summary>
    /// The Main.
    /// </summary>
    /// <param name="args">The args<see cref="string[]"/>.</param>
    private static void Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton(Config);
            builder.Services.AddMemoryCache(options =>
            {
                options.SizeLimit = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 3;
            });

#if DEBUG
            builder.Services.AddDbContext<GpssDbContext>((sp, options) =>
                options.UseInMemoryDatabase("MockGpssDb"));
#else
            builder.Services.AddDbContext<GpssDbContext>((sp, options) =>
            {
                var config = sp.GetRequiredService<ConfigHolder>();
                var builder = new MySqlConnectionStringBuilder
                {
                    Server = config.Get(c => c.MySqlHost),
                    Port = (uint)config.Get(c => c.MySqlPort),
                    UserID = config.Get(c => c.MySqlUser),
                    Password = config.Get(c => c.MySqlPassword),
                    Database = config.Get(c => c.MySqlDatabase),
                    // You can set additional options here if needed
                };
                options.UseMySql(builder.ConnectionString, ServerVersion.AutoDetect(builder.ConnectionString));
            });
#endif

            builder.Services.AddScoped<Database>();
            builder.Services.AddControllers();
            builder.Services.AddHostedService<IntegrityChecker>();

            builder.WebHost.ConfigureKestrel(options =>
            {
#if DEBUG
                options.ListenLocalhost(8080, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1;
                });
#else
                if (Config.Get(config => config.GpssHttp) && Config.Get(config => config.GpssHttps))
                {
                    Console.WriteLine("Error: Both HTTP and HTTPS are enabled. Please enable only one.");
                    Environment.Exit(3);
                }
                else if (!Config.Get(config => config.GpssHttp) && !Config.Get(config => config.GpssHttps))
                {
                    Console.WriteLine("Error: No HTTP or HTTPS endpoints are enabled. Please enable at least one.");
                    Environment.Exit(3);
                }

                IPAddress? address = Helpers.GetAddressFromString(Config.Get(config => config.GpssHost));
                if (address == null)
                {
                    Console.WriteLine($"Error: Invalid Hostname or IP address. Please configure a valid host.");
                    Environment.Exit(3);
                }

                if (Config.Get(config => config.GpssHttp))
                {
                    options.Listen(address, Config.Get(config => config.GpssPort), listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1;
                    });
                }

                if (Config.Get(config => config.GpssHttps))
                {
                    if (string.IsNullOrWhiteSpace(Config.Get(config => config.GpssHttpsCert)) || string.IsNullOrWhiteSpace(Config.Get(config => config.GpssHttpsKey)))
                    {
                        Console.WriteLine("Error: HTTPS is enabled but certificate or key path is missing in config.");
                        Environment.Exit(3);
                    }

                    options.Listen(address, Config.Get(config => config.GpssPort), listenOptions =>
                    {
                        listenOptions.UseHttps(Config.Get(config => config.GpssHttpsCert), Config.Get(config => config.GpssHttpsKey));
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });
                }
#endif
            });

            Helpers.Init();
            var app = builder.Build();

            app.UseRouting();
            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GpssDbContext>();
                if (db.Database.IsRelational())
                {
                    db.Database.Migrate();
                }
            }
#if DEBUG
            app.UseDeveloperExceptionPage();
#endif
            if (Config.Get(config => config.GpssHttps))
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: Failed to start Local GPSS due to an unexpected exception.");
            Console.WriteLine(e);
            Environment.Exit(1);
        }
    }
}
