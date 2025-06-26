namespace GPSS_Client
{
    /// <summary>
    /// Defines the <see cref="App" />.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Defines the _serviceProvider.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        /// <param name="serviceProvider">The serviceProvider<see cref="IServiceProvider"/>.</param>
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// The CreateWindow.
        /// </summary>
        /// <param name="activationState">The activationState<see cref="IActivationState?"/>.</param>
        /// <returns>The <see cref="Window"/>.</returns>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Use DI to resolve AppShell
            return new Window(_serviceProvider.GetRequiredService<AppShell>());
        }
    }
}
