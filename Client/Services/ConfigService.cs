namespace GPSS_Client.Services
{
    using GPSS_Client.Config;
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
        /// <returns>The <see cref="ClientConfig"/>.</returns>
        public static ClientConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<ClientConfig>(json);
                    if (config != null)
                        return config;
                }
            }
            catch { /* Ignore and use default */ }

            var defaultConfig = new ClientConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        /// <summary>
        /// The Save.
        /// </summary>
        /// <param name="config">The config<see cref="ClientConfig"/>.</param>
        public static void Save(ClientConfig config)
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
