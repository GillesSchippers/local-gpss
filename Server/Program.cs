using GPSS_Server.Config;
using GPSS_Server.Datastore;
using GPSS_Server.Services;
using GPSS_Server.Utils;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using System.Net;

internal class Program
{
    private static void Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            var config = ConfigService.Load();

            builder.Services.AddSingleton<ConfigHolder>(sp => new ConfigHolder(config));
            builder.Services.AddGpssDatabase(config);
            builder.Services.AddControllers();
            builder.Services.AddHostedService<IntegrityChecker>();

            // --- Kestrel endpoint configuration ---
            builder.WebHost.ConfigureKestrel(options =>
            {
                if (config.GpssHttp && config.GpssHttps)
                {
                    Console.WriteLine("Error: Both HTTP and HTTPS are enabled. Please enable only one.");
                    Environment.Exit(3);
                }
                else if (!config.GpssHttp && !config.GpssHttps)
                {
                    Console.WriteLine("Error: No HTTP or HTTPS endpoints are enabled. Please enable at least one.");
                    Environment.Exit(3);
                }

                IPAddress? address = Helpers.GetAdressFromString(config.GpssHost);
                if (address == null)
                {
                    Console.WriteLine($"Error: Invalid Hostname or IP address. Please configure a valid host.");
                    Environment.Exit(3);
                }

                if (config.GpssHttp)
                {
                    options.Listen(address, config.GpssPort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });
                }

                if (config.GpssHttps)
                {
                    if (string.IsNullOrWhiteSpace(config.GpssHttpsCert) || string.IsNullOrWhiteSpace(config.GpssHttpsKey))
                    {
                        Console.WriteLine("Error: HTTPS is enabled but certificate or key path is missing in config.");
                        Environment.Exit(3);
                    }

                    options.Listen(address, config.GpssPort, listenOptions =>
                    {
                        listenOptions.UseHttps(config.GpssHttpsCert, config.GpssHttpsKey);
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });
                }
            });

            Helpers.Init();
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

            if (config.GpssHttps)
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.Run();
        }
        catch (Exception)
        {
            Console.WriteLine("Error: Failed to start Local GPSS due to an unexpected exception.");
            Environment.Exit(1);
        }
    }
}