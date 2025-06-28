namespace GPSS_Server
{
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
                var http = Config.Get(config => config.GpssHttp);
                var https = Config.Get(config => config.GpssHttps);

                if (Helpers.GetAddressFromString(Config.Get(config => config.GpssHost)) is not IPAddress address)
                {
                    throw new InvalidOperationException("Invalid Hostname or IP address. Please configure a valid host.");
                }

                switch ((http, https))
                {
                    case (true, true):
                        throw new InvalidOperationException("Both HTTP and HTTPS are enabled. Please enable only one.");
                    case (false, false):
                        throw new InvalidOperationException("No HTTP or HTTPS endpoints are enabled. Please enable at least one.");
                    case (true, false):
                        options.Listen(address, Config.Get(config => config.GpssPort), listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });
                        break;
                    case (false, true):
                        var cert = Config.Get(config => config.GpssHttpsCert);
                        var key = Config.Get(config => config.GpssHttpsKey);
                        if (string.IsNullOrWhiteSpace(cert) || string.IsNullOrWhiteSpace(key))
                        {
                            throw new InvalidOperationException("HTTPS is enabled but certificate or key path is missing in config.");
                        }
                        options.Listen(address, Config.Get(config => config.GpssPort), listenOptions =>
                        {
                            listenOptions.UseHttps(cert, key);
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        });
                        break;
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
                Console.WriteLine($"Error: Failed to start GPSS Server due to an unexpected exception.\n{e.Message}\n\n{e.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}
