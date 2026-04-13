using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace USDT_Sender.Views
{
    public partial class SplashScreen : Window
    {
        // Track animation completion to avoid race conditions
        private readonly TaskCompletionSource<bool> _loaderStartTcs = new();
        private readonly TaskCompletionSource<bool> _logoPulseTcs = new();

        public SplashScreen()
        {
            InitializeComponent();
            Loaded += SplashScreen_Loaded;

            // Set version dynamically from assembly
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString(3) ?? "2.1.0";
            var buildDate = "2026.04.14"; // Can be injected via MSBuild
            TxtVersion.Text = $"v{version} (Build {buildDate})";
        }

        private async void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            // Start background animations
            BeginStoryboard((Storyboard)FindResource("LoaderAnimation"));
            BeginStoryboard((Storyboard)FindResource("LogoPulse"));
            _loaderStartTcs.SetResult(true);
            _logoPulseTcs.SetResult(true);

            // Run startup sequence
            await RunStartupSequence();
        }

        private async Task RunStartupSequence()
        {
            try
            {
                // 1. Smooth fade-in
                await AnimatePropertyAsync(OpacityProperty, 0, 1, 1000);

                // 2. Simulate initialization phases (Replace with real async init calls)
                await UpdatePhase("Loading configuration...", 1, 1400);
                await UpdatePhase("Establishing secure connection...", 2, 1600);
                await UpdatePhase("Verifying transaction engine...", 3, 1500);
                await UpdatePhase("Preparing interface...", 4, 1300);

                // 3. Small pause for visual completion
                await Task.Delay(400);

                // 4. Smooth fade-out & close
                await AnimatePropertyAsync(OpacityProperty, 1, 0, 450);
            }
            catch (Exception ex)
            {
                // Fallback: log error and ensure splash closes gracefully
                System.Diagnostics.Debug.WriteLine($"[Splash] Init error: {ex.Message}");
            }
            finally
            {
                Close();
            }
        }

        private async Task UpdatePhase(string message, int activeDots, int delay)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = message;

                // Loop through all 4 dots
                for (int i = 1; i <= 4; i++)
                {
                    if (FindName($"Dot{i}") is Ellipse dot)
                    {
                        // Active dots get the Accent color, others stay Muted
                        dot.Fill =
                            (i <= activeDots)
                                ? (SolidColorBrush)FindResource("Accent")
                                : (SolidColorBrush)FindResource("TxtMuted");
                    }
                }
            });

            await Task.Delay(delay);
        }

        private Task AnimatePropertyAsync(
            DependencyProperty prop,
            double from,
            double to,
            int milliseconds
        )
        {
            var tcs = new TaskCompletionSource<bool>();
            var animation = new DoubleAnimation(
                from,
                to,
                new Duration(TimeSpan.FromMilliseconds(milliseconds))
            );
            animation.Completed += (s, e) => tcs.SetResult(true);
            BeginAnimation(prop, animation);
            return tcs.Task;
        }
    }
}
