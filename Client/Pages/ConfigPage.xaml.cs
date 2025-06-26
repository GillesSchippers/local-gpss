namespace GPSS_Client
{
    using GPSS_Client.Config;
    using GPSS_Client.Services;
    using System.ComponentModel;
    using System.Reflection;

    /// <summary>
    /// Defines the <see cref="ConfigPage" />.
    /// </summary>
    public partial class ConfigPage : ContentPage
    {
        /// <summary>
        /// Defines the _configHolder.
        /// </summary>
        private readonly ConfigHolder _configHolder;

        /// <summary>
        /// Defines the _config.
        /// </summary>
        private readonly ClientConfig _config;

        /// <summary>
        /// Defines the _serviceProvider.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Defines the _propertyInputs.
        /// </summary>
        private readonly Dictionary<string, View> _propertyInputs = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigPage"/> class.
        /// </summary>
        /// <param name="configHolder">The configHolder<see cref="ConfigHolder"/>.</param>
        /// <param name="serviceProvider">The serviceProvider<see cref="IServiceProvider"/>.</param>
        public ConfigPage(ConfigHolder configHolder, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _configHolder = configHolder;
            _config = _configHolder.Config;
            _serviceProvider = serviceProvider;

            RenderConfigOptions();
        }

        /// <summary>
        /// The RenderConfigOptions.
        /// </summary>
        private void RenderConfigOptions()
        {
            DynamicConfigStack.Children.Clear();
            var props = typeof(ClientConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                // Get DisplayName attribute if present
                var displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
                var labelText = displayNameAttr?.DisplayName ?? prop.Name;

                var label = new Label { Text = labelText, FontAttributes = FontAttributes.Bold };
                View input;

                if (prop.PropertyType == typeof(string))
                {
                    var entry = new Entry { Text = prop.GetValue(_config)?.ToString() ?? "" };
                    input = entry;
                }
                else if (prop.PropertyType == typeof(int))
                {
                    var entry = new Entry { Text = prop.GetValue(_config)?.ToString() ?? "", Keyboard = Keyboard.Numeric };
                    input = entry;
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    var sw = new Switch { IsToggled = (bool)(prop.GetValue(_config) ?? false) };
                    input = sw;
                }
                else
                {
                    // Fallback: show as string
                    var entry = new Entry { Text = prop.GetValue(_config)?.ToString() ?? "" };
                    input = entry;
                }

                _propertyInputs[prop.Name] = input;
                DynamicConfigStack.Children.Add(label);
                DynamicConfigStack.Children.Add(input);
            }
        }

        /// <summary>
        /// The OnSaveClicked.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private async void OnSaveClicked(object sender, EventArgs e)
        {
            var props = typeof(ClientConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                if (_propertyInputs.TryGetValue(prop.Name, out var input))
                {
                    try
                    {
                        if (prop.PropertyType == typeof(string) && input is Entry entry)
                            prop.SetValue(_config, entry.Text);
                        else if (prop.PropertyType == typeof(int) && input is Entry intEntry && int.TryParse(intEntry.Text, out var intVal))
                            prop.SetValue(_config, intVal);
                        else if (prop.PropertyType == typeof(bool) && input is Switch sw)
                            prop.SetValue(_config, sw.IsToggled);
                    }
                    catch { /* Optionally handle conversion errors */ }
                }
            }

            ConfigService.Save(_config);

            // Reload and replace the config in the holder
            var newConfig = ConfigService.Load();
            var configHolder = _serviceProvider.GetService<ConfigHolder>();
            if (configHolder != null)
                configHolder.Config = newConfig;

            await DisplayAlert("Config", "Configuration saved and applied.", "OK");
        }
    }
}
