namespace GPSS_Client.Config
{
    public class ConfigHolder
    {
        private ClientConfig _config;
        public event EventHandler? ConfigChanged;

        public ConfigHolder(ClientConfig config)
        {
            _config = config;
        }

        public ClientConfig Config
        {
            get => _config;
            set
            {
                if (_config != value)
                {
                    _config = value;
                    ConfigChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}