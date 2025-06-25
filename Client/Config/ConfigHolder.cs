namespace GPSS_Client.Config
{
    public class ConfigHolder(ClientConfig config)
    {
        public event EventHandler? ConfigChanged;

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