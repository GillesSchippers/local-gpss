namespace GPSS_Server.Services
{
    using GPSS_Server.Config;
    using System.Text.Json;

    /// <summary>
    /// Defines the <see cref="ConfigService" />.
    /// </summary>
    public class ConfigService
    {
        /// <summary>
        /// Defines the ConfigFilePath.
        /// </summary>
        private static readonly string ConfigFilePath = "Config.json";

        /// <summary>
        /// The Load.
        /// </summary>
        /// <returns>The <see cref="ServerConfig"/>.</returns>
        public static ServerConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<ServerConfig>(json);
                    if (config != null)
                        return config;
                }
            }
            catch { /* Ignore and use default */ }

            var defaultConfig = new ServerConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        /// <summary>
        /// The Save.
        /// </summary>
        /// <param name="config">The config<see cref="ServerConfig"/>.</param>
        public static void Save(ServerConfig config)
        {
            try
            {
#if DEBUG
                // In debug mode, we do not save the config to avoid overwriting changes made during development.
                return;
#else
                var json = JsonSerializer.Serialize(config);
                File.WriteAllText(ConfigFilePath, json);
#endif
            }
            catch { /* Ignore */ }
        }
    }
}
