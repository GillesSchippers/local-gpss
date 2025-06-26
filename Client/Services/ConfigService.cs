using GPSS_Client.Config;
using System.Text.Json;

namespace GPSS_Client.Services
{
    public class ConfigService
    {
        private static readonly string ConfigFilePath = "Config.json";

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

        public static void Save(ClientConfig config)
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