namespace GPSS_Client.Config
{
    /// <summary>
    /// Defines the <see cref="ConfigHolder" />.
    /// </summary>
    public class ConfigHolder(ClientConfig config)
    {
        /// <summary>
        /// Defines the ConfigChanged.
        /// </summary>
        public event EventHandler? ConfigChanged;

        /// <summary>
        /// Gets or sets the Config.
        /// </summary>
        public ClientConfig Config
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
