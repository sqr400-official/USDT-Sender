using System.Configuration;
using System.Data;
using System.Windows;

namespace USDT_Sender
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // This prevents the app from exiting when the Splash screen closes
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splash = new USDT_Sender.Views.SplashScreen();
            splash.Show();

            // Listen for when the splash is finished
            splash.Closed += (s, args) =>
            {
                // Now that Splash is gone, change mode so it closes when MainWindow closes
                this.Dispatcher.Invoke(() =>
                {
                    this.ShutdownMode = ShutdownMode.OnLastWindowClose;
                    var main = new MainWindow();
                    main.Show();
                });
            };
        }
    }
}
