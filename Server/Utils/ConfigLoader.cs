using Models;
using System.Text.Json;

namespace Utils
{
    public static class ConfigLoader
    {
        public static Config LoadOrCreateConfig()
        {
            const string configPath = "./Config.json";

            if (!File.Exists(configPath))
            {
                var defaultConfig = new Config
                {
                    Ip = "127.0.0.1",
                    Port = 8080,
                    MySqlHost = "localhost",
                    MySqlPort = 3306,
                    MySqlUser = "root",
                    MySqlPassword = "password",
                    MySqlDatabase = "gpss"
                };
                var json = JsonSerializer.Serialize(defaultConfig);
                File.WriteAllText(configPath, json);
                Console.WriteLine("Error: Configuration file 'local-gpss.json' not found. A default configuration has been created. Please review and update the file before restarting the application.");
                Environment.Exit(3);
            }

            Config? configNullable;
            try
            {
                string configText = File.ReadAllText(configPath);
                configNullable = JsonSerializer.Deserialize<Config>(configText);

                if (!configNullable.HasValue)
                {
                    throw new JsonException("Configuration file is empty or invalid.");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error:  Failed to load or parse 'local-gpss.json'. Please ensure the file is valid JSON and all required fields are present.");
                Environment.Exit(3);
                throw; // This line is unreachable but included to satisfy the compiler.
            }

            var config = configNullable.Value;

            var localIps = Helpers.GetLocalIPs();
            if (!localIps.Contains(config.Ip))
            {
                Console.WriteLine("Error: The configured IP address is not available on this system. Please update 'local-gpss.json' with a valid local IP address.");
                Environment.Exit(3);
            }

            if (!Helpers.CanBindToPort(config.Port))
            {
                Console.WriteLine("Error: The configured port is unavailable or cannot be bound. Please choose a different port in 'local-gpss.json'.");
                Environment.Exit(3);
            }

            return config;
        }
    }
}