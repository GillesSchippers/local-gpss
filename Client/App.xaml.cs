namespace GPSS_Client
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Use DI to resolve AppShell
            return new Window(_serviceProvider.GetRequiredService<AppShell>());
        }
    }
}