namespace GPSS_Server
{
    using GPSS_Server.Config;
    using GPSS_Server.Datastore;
    using GPSS_Server.Datastore.Checks;
    using GPSS_Server.Utils;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.IdentityModel.Tokens;
    using System.Diagnostics;
    using System.Security.Cryptography;
    using System.Text;

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

                using var loggerFactory = LoggerFactory.Create(logging =>
                {
                    logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                });
                var logger = loggerFactory.CreateLogger<Program>();

                logger.LogInformation("Starting GPSS Server initialization...");

                builder.Services.AddSingleton(Config);
                logger.LogInformation("Loaded configuration.");

                builder.Services.AddMemoryCache(options =>
                {
                    options.SizeLimit = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 3;
                });
                logger.LogInformation("Configured Memory cache.");

#if DEBUG
                builder.Services.AddDbContext<GpssDbContext>((sp, options) =>
                    options.UseInMemoryDatabase("MockGpssDb"));
                logger.LogInformation("Using in-memory database.");
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
                logger.LogInformation("Configured database context.");
#endif

                builder.Services.AddScoped<Database>();
                builder.Services.AddControllers();
                builder.Services.AddHostedService<IntegrityChecker>();
                logger.LogInformation("Registered core services and controllers.");

                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        var jwtKey = Config.Get(config => config.GpssJwtKey);
                        if (string.IsNullOrWhiteSpace(jwtKey))
                        {
                            logger.LogWarning("No JWT key configured. Using a randomly generated key.");
                            jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                        }

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = nameof(GPSS_Server),
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtKey))
                        };
                    });
                builder.Services.AddAuthorization();
                logger.LogInformation("Configured authentication and authorization.");

                builder.WebHost.ConfigureKestrel(options =>
                {
#if DEBUG
                    options.ListenLocalhost(8080, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1;
                    });

                    options.ListenLocalhost(8443, listenOptions =>
                    {
                        listenOptions.UseHttps(Helpers.GenerateSelfSignedCertificate());
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });
#else
                    var http = Config.Get(config => config.GpssHttp);
                    var https = Config.Get(config => config.GpssHttps);

                    if (Helpers.GetAddressFromString(Config.Get(config => config.GpssHost)) is not IPAddress address)
                    {
                        logger.LogCritical("Invalid Hostname or IP address. Please configure a valid host.");
                        throw new InvalidOperationException();
                    }

                    switch ((http, https))
                    {
                        case (true, true):
                            logger.LogCritical("Both HTTP and HTTPS are enabled. Please enable only one.");
                            throw new InvalidOperationException();
                        case (false, false):
                            logger.LogCritical("No HTTP or HTTPS endpoints are enabled. Please enable at least one.");
                            throw new InvalidOperationException();
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
                                logger.LogWarning("No HTTPS certificate/key configured. Using a self-signed certificate for HTTPS.");
                                options.Listen(address, Config.Get(config => config.GpssPort), listenOptions =>
                                {
                                    listenOptions.UseHttps(Helpers.GenerateSelfSignedCertificate(address));
                                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                                });
                            }
                            else
                            {
                                options.Listen(address, Config.Get(config => config.GpssPort), listenOptions =>
                                {
                                    listenOptions.UseHttps(cert, key);
                                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                                });
                            }
                            break;
                    }
#endif
                });

                Helpers.Init();
                logger.LogInformation("PKHeX data initialized.");

                var app = builder.Build();
                logger = app.Services.GetRequiredService<ILogger<Program>>();

                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
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
                    logger.LogInformation("HSTS and HTTPS redirection enabled.");
                }

                logger.LogInformation("GPSS Server initialization complete. Server is running.");
                app.Run();
            }
            catch (Exception e)
            {
                Trace.WriteLine($"{e.Message}\n{e.StackTrace}");
                Console.WriteLine("Error: Failed to start GPSS Server due to an unexpected exception.");
                Environment.Exit(1);
            }
        }
    }
}
