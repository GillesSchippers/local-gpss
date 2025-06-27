namespace GPSS_Client.Config
{
    using System;
    using System.IO;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text.Json;

    public class ConfigHolder
    {
        private const string ConfigFilePath = "Config.json";
        private static ConfigHolder? _instance;

        public event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

        private ClientConfig Config { get; set; }

        public ConfigHolder()
        {
            if (_instance != null)
                throw new InvalidOperationException("Only one instance of ConfigHolder is allowed.");
            _instance = this;
            Config = Load();
        }

        private static ClientConfig Load()
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
            catch { /* Ignore */ }

            var defaultConfig = new ClientConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        private static void Save(ClientConfig config)
        {
#if !DEBUG
            try
            {
                var json = JsonSerializer.Serialize(config);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch { /* Ignore */ }
#endif
        }

        public TProp Get<TProp>(Expression<Func<ClientConfig, TProp>> propertySelector)
        {
            if (propertySelector.Body is MemberExpression memberExpr)
            {
                var propInfo = memberExpr.Member as PropertyInfo;
                if (propInfo != null)
                {
                    return (TProp)propInfo.GetValue(Config)!;
                }
            }
            throw new ArgumentException("Invalid property selector");
        }

        public void Set<TProp>(Expression<Func<ClientConfig, TProp>> propertySelector, TProp value)
        {
            if (propertySelector.Body is MemberExpression memberExpr)
            {
                var propInfo = memberExpr.Member as PropertyInfo;
                if (propInfo != null)
                {
                    propInfo.SetValue(Config, value);
                    Save(Config);
                    ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(Config));
                    return;
                }
            }
            throw new ArgumentException("Invalid property selector");
        }
    }

    public class ConfigChangedEventArgs(ClientConfig config) : EventArgs
    {
        public ClientConfig Config { get; } = config;
    }
}
