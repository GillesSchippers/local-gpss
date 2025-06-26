using GPSS_Server.Config;
using System.Text.Json;

namespace GPSS_Server.Services
{
    public class ConfigService
    {
        private static readonly string ConfigFilePath = "Config.json";

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

        public static void Save(ServerConfig config)
        {
#if DEBUG
            return;
#else
            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(ConfigFilePath, json);
#endif
        }
    }
}