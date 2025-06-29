namespace GPSS_Server.Config
{
    using System;
    using System.IO;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text.Json;

    /// <summary>
    /// Defines the <see cref="ConfigHolder" />.
    /// </summary>
    public class ConfigHolder
    {
        /// <summary>
        /// Defines the ConfigFilePath.
        /// </summary>
        private const string ConfigFilePath = "Config.json";

        /// <summary>
        /// Defines the _instance.
        /// </summary>
        private static ConfigHolder? _instance;

        /// <summary>
        /// Defines the ConfigChanged.
        /// </summary>
        public event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

        /// <summary>
        /// Gets or sets the Config.
        /// </summary>
        private ServerConfig Config { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigHolder"/> class.
        /// </summary>
        public ConfigHolder()
        {
            if (_instance != null)
                throw new InvalidOperationException("Only one instance of ConfigHolder is allowed.");
            _instance = this;
            Config = Load();
        }

        /// <summary>
        /// The Load.
        /// </summary>
        /// <returns>The <see cref="ServerConfig"/>.</returns>
        private static ServerConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    if (JsonSerializer.Deserialize<ServerConfig>(json) is { } config)
                        return config;
                }
            }
            catch { /* Ignore */ }

            var defaultConfig = new ServerConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        /// <summary>
        /// The Save.
        /// </summary>
        /// <param name="config">The config<see cref="ServerConfig"/>.</param>
        private static void Save(ServerConfig config)
        {
            try
            {
#if !DEBUG
                File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config));
#endif
            }
            catch { /* Ignore */ }
        }

        /// <summary>
        /// The Get.
        /// </summary>
        /// <typeparam name="TProp">.</typeparam>
        /// <param name="propertySelector">The propertySelector<see cref="Expression{Func{ServerConfig, TProp}}"/>.</param>
        /// <returns>The <see cref="TProp"/>.</returns>
        public TProp Get<TProp>(Expression<Func<ServerConfig, TProp>> propertySelector) =>
            propertySelector.Body is MemberExpression { Member: PropertyInfo propInfo }
                ? (TProp)propInfo.GetValue(Config)!
                : throw new ArgumentException("Invalid property selector");

        /// <summary>
        /// The Set.
        /// </summary>
        /// <typeparam name="TProp">.</typeparam>
        /// <param name="propertySelector">The propertySelector<see cref="Expression{Func{ServerConfig, TProp}}"/>.</param>
        /// <param name="value">The value<see cref="TProp"/>.</param>
        public void Set<TProp>(Expression<Func<ServerConfig, TProp>> propertySelector, TProp value)
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

    /// <summary>
    /// Defines the <see cref="ConfigChangedEventArgs" />.
    /// </summary>
    public class ConfigChangedEventArgs(ServerConfig config) : EventArgs
    {
        /// <summary>
        /// Gets the Config.
        /// </summary>
        public ServerConfig Config { get; } = config;
    }
}
