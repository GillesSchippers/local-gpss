namespace GPSS_Client.Config
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
        private ClientConfig Config { get; set; }

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
        /// <returns>The <see cref="ClientConfig"/>.</returns>
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

        /// <summary>
        /// The Save.
        /// </summary>
        /// <param name="config">The config<see cref="ClientConfig"/>.</param>
        private static void Save(ClientConfig config)
        {
        }

        /// <summary>
        /// The Get.
        /// </summary>
        /// <typeparam name="TProp">.</typeparam>
        /// <param name="propertySelector">The propertySelector<see cref="Expression{Func{ClientConfig, TProp}}"/>.</param>
        /// <returns>The <see cref="TProp"/>.</returns>
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

        /// <summary>
        /// The Set.
        /// </summary>
        /// <typeparam name="TProp">.</typeparam>
        /// <param name="propertySelector">The propertySelector<see cref="Expression{Func{ClientConfig, TProp}}"/>.</param>
        /// <param name="value">The value<see cref="TProp"/>.</param>
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

    /// <summary>
    /// Defines the <see cref="ConfigChangedEventArgs" />.
    /// </summary>
    public class ConfigChangedEventArgs(ClientConfig config) : EventArgs
    {
        /// <summary>
        /// Gets the Config.
        /// </summary>
        public ClientConfig Config { get; } = config;
    }
}
