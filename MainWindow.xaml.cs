using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using USDT_Sender.Controls;
using USDT_Sender.Services;
using USDT_Sender.Views;

namespace USDT_Sender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, UserControl> _viewCache = new();
        private readonly DispatcherTimer _clock;

        public MainWindow()
        {
            InitializeComponent();

            _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clock.Tick += (_, _) => UpdateClock();
            _clock.Start();
            UpdateClock();

            NavigateTo("Billing");
            UpdateLicenseBadge(LicenseBadgeBorder, LicenseDot, LicenseBadgeText);

            // ── FIX: subscribe to Click (not just Checked) on every nav RadioButton.
            // When the user clicks a RadioButton that is ALREADY checked, the Checked
            // event does not fire again — so we use Click to catch that case.
            foreach (var rb in new[] { NavBilling, NavOrders, NavInventory, NavReports, NavHelp })
                rb.Click += NavBtn_RadioClick;
        }

        // Fires on every click of a nav RadioButton, even if already checked.
        private void NavBtn_RadioClick(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
                NavigateTo(tag);
        }

        // Still needed for the initial IsChecked=True trigger on startup.
        private void NavBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
                NavigateTo(tag);
        }

        // ── Icon button click (Settings, Notifications, etc.) ─────────────────
        private void NavBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                // ── FIX: uncheck all radio buttons so none appears active while
                // on a non-radio destination (Settings, etc.). This ensures that
                // when the user later clicks e.g. Billing, IsChecked goes from
                // false → true and the Checked event fires normally.
                foreach (
                    var rb in new[] { NavBilling, NavOrders, NavInventory, NavReports, NavHelp }
                )
                    rb.IsChecked = false;

                NavigateTo(tag);
            }
        }

        private void BtnNotifications_Click(object sender, RoutedEventArgs e)
        {
            AppDialog.Show(
                "No new notifications",
                "You have no new notifications.",
                actionLabel: "Okay",
                closeLabel: "Close",
                DialogType.Info
            );
        }

        private void NavigateTo(string viewTag)
        {
            if (!_viewCache.TryGetValue(viewTag, out var view))
            {
                view = viewTag switch
                {
                    "Billing" => new BillingView(),
                    "Settings" => new SettingsView(),
                    "Activation" => new ActivationView(),
                    "Help" => new HelpSupportView(),
                    "Reports" => new ReportsView(),
                    "Orders" => MakePlaceholder("Orders", "📋", "Order management coming soon."),
                    "Inventory" => MakePlaceholder(
                        "Inventory",
                        "📦",
                        "Inventory management coming soon."
                    ),

                    _ => new UserControl(),
                };
                _viewCache[viewTag] = view;
            }

            MainContent.Content = view;
        }

        /// <summary>
        /// Creates a simple dark placeholder panel for pages not yet implemented.
        /// </summary>
        private static UserControl MakePlaceholder(string title, string icon, string subtitle)
        {
            var uc = new UserControl { Background = System.Windows.Media.Brushes.Transparent };
            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            stack.Children.Add(
                new TextBlock
                {
                    Text = icon,
                    FontSize = 48,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 12),
                }
            );
            stack.Children.Add(
                new TextBlock
                {
                    Text = title,
                    FontSize = 22,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF5)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8),
                }
            );
            stack.Children.Add(
                new TextBlock
                {
                    Text = subtitle,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xA0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            );

            uc.Content = stack;
            return uc;
        }

        private static void UpdateLicenseBadge(Border badgeBorder, Ellipse dot, TextBlock text)
        {
            var (cachedKey, isExpired) = LicenseStorage.Load();
            bool active = cachedKey != null && !isExpired;

            var teal = Color.FromRgb(0x00, 0xD4, 0xAA);
            var red = Color.FromRgb(0xFF, 0x5C, 0x7A);
            var c = active ? teal : red;

            dot.Fill = new SolidColorBrush(c);
            badgeBorder.BorderBrush = new SolidColorBrush(c);
            badgeBorder.Background = new SolidColorBrush(Color.FromArgb(0x22, c.R, c.G, c.B));
            text.Foreground = new SolidColorBrush(c);
            text.Text = active ? "ACTIVATED" : "NOT ACTIVATED";
        }

        private void UpdateClock() =>
            TxtClock.Text = DateTime.Now.ToString("ddd dd MMM   HH:mm:ss");

        protected override void OnClosed(EventArgs e)
        {
            _clock.Stop();
            base.OnClosed(e);
        }
    }
}
