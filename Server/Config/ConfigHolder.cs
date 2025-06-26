namespace GPSS_Server.Config
{
    public class ConfigHolder(ServerConfig config)
    {
        public event EventHandler? ConfigChanged;

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