namespace GPSS_Server.Config
{
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Defines the <see cref="ConfigHolder" />.
    /// </summary>
    public class ConfigHolder(ServerConfig config)
    {
        /// <summary>
        /// Defines the ConfigChanged.
        /// </summary>
        public event EventHandler? ConfigChanged;

        /// <summary>
        /// Gets or sets the Config.
        /// </summary>
        public ServerConfig Config { get; set; } = config;

        /// <summary>
        /// The Get.
        /// </summary>
        /// <typeparam name="TProp">.</typeparam>
        /// <param name="propertySelector">The propertySelector<see cref="Expression{Func{ServerConfig, TProp}}"/>.</param>
        /// <returns>The <see cref="TProp"/>.</returns>
        public TProp Get<TProp>(Expression<Func<ServerConfig, TProp>> propertySelector)
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
                    ConfigChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }
            throw new ArgumentException("Invalid property selector");
        }
    }
}
