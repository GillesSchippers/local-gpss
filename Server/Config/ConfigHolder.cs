namespace GPSS_Server.Config
{
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
        public ServerConfig Config
        {
            get => config;
            set
            {
                if (config != value)
                {
                    config = value;
                    ConfigChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
