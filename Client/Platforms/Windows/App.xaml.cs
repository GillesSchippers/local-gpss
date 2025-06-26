// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GPSS_Client.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// The CreateMauiApp.
        /// </summary>
        /// <returns>The <see cref="MauiApp"/>.</returns>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
