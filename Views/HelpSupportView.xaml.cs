using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace USDT_Sender.Views
{
    public partial class HelpSupportView : UserControl
    {
        // Validation flags
        private bool _nameOk, _emailOk, _subjectOk, _messageOk;

        // Brushes for validation feedback
        private static readonly SolidColorBrush BrDanger = new SolidColorBrush(Color.FromRgb(0xFF, 0x5C, 0x6E));
        private static readonly SolidColorBrush BrGreen = new SolidColorBrush(Color.FromRgb(0x2D, 0xD4, 0xBF));

        public HelpSupportView()
        {
            InitializeComponent();
            InitializeValidation();
        }

        private void InitializeValidation()
        {
            // Attach validation events
            TxtContactName.TextChanged += TxtContactName_TextChanged;
            TxtContactEmail.TextChanged += TxtContactEmail_TextChanged;
            TxtContactSubject.TextChanged += TxtContactSubject_TextChanged;
            TxtContactMessage.TextChanged += TxtContactMessage_TextChanged;
        }

        // ══════════════════════════════════════════════════
        //  CONTACT FORM VALIDATION
        // ══════════════════════════════════════════════════

        private void TxtContactName_TextChanged(object sender, TextChangedEventArgs e)
        {
            _nameOk = !string.IsNullOrWhiteSpace(TxtContactName.Text.Trim());
            ErrName.Visibility = _nameOk ? Visibility.Collapsed : Visibility.Visible;
            UpdateSendButton();
        }

        private void TxtContactEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            var email = TxtContactEmail.Text.Trim();
            // Simple but effective email regex for client-side validation
            _emailOk = !string.IsNullOrWhiteSpace(email) && 
                       Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            ErrEmail.Visibility = _emailOk ? Visibility.Collapsed : Visibility.Visible;
            UpdateSendButton();
        }

        private void TxtContactSubject_TextChanged(object sender, TextChangedEventArgs e)
        {
            _subjectOk = !string.IsNullOrWhiteSpace(TxtContactSubject.Text.Trim());
            ErrSubject.Visibility = _subjectOk ? Visibility.Collapsed : Visibility.Visible;
            UpdateSendButton();
        }

        private void TxtContactMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            var msg = TxtContactMessage.Text.Trim();
            _messageOk = msg.Length >= 20;
            ErrMessage.Visibility = _messageOk ? Visibility.Collapsed : Visibility.Visible;
            UpdateSendButton();
        }

        private void UpdateSendButton()
        {
            BtnSendTicket.IsEnabled = _nameOk && _emailOk && _subjectOk && _messageOk;
        }

        // ══════════════════════════════════════════════════
        //  BUTTON CLICK HANDLERS
        // ══════════════════════════════════════════════════

        private async void BtnSendTicket_Click(object sender, RoutedEventArgs e)
        {
            // Disable button to prevent duplicate submissions
            BtnSendTicket.IsEnabled = false;
            BtnSendTicket.Content = "⏳ Sending...";

            // Simulate API call
            await System.Threading.Tasks.Task.Delay(1500);

            // Show success state
            TxtSendSuccess.Visibility = Visibility.Visible;
            BtnSendTicket.Content = "✓ Sent!";
            BtnSendTicket.Background = BrGreen;

            // Optional: Clear form after delay
            await System.Threading.Tasks.Task.Delay(3000);
            ClearContactForm();
        }

        private void ClearContactForm()
        {
            TxtContactName.Text = "";
            TxtContactEmail.Text = "";
            TxtContactSubject.Text = "";
            TxtContactMessage.Text = "";
            TxtSendSuccess.Visibility = Visibility.Collapsed;
            BtnSendTicket.Content = "📤 Send Support Ticket";
            BtnSendTicket.Background = (SolidColorBrush)FindResource("Accent");
            _nameOk = _emailOk = _subjectOk = _messageOk = false;
            UpdateSendButton();
        }

        private void BtnQuickAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string action) return;

            switch (action)
            {
                case "status":
                    // Open ticket status modal or navigate
                    ShowNotification("🎫 Enter your ticket ID to check status", "info");
                    break;
                case "chat":
                    // Launch live chat widget
                    ShowNotification("💬 Connecting to live chat...", "info");
                    break;
                case "guides":
                    // Open user guides
                    OpenUrl("https://usdt-sender.com/guides");
                    break;
            }
        }

        private void BtnWebsite_Click(object sender, RoutedEventArgs e) => 
            OpenUrl("https://usdt-sender.com");

        private void BtnSocial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string platform) return;

            var urls = new System.Collections.Generic.Dictionary<string, string>
            {
                ["twitter"] = "https://twitter.com/usdtsender",
                ["linkedin"] = "https://linkedin.com/company/usdt-sender",
                ["youtube"] = "https://youtube.com/@usdtsender"
            };

            if (urls.TryGetValue(platform, out var url))
                OpenUrl(url);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) =>
            OpenUrl(e.Uri.AbsoluteUri);

        private void OpenUrl(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch
            {
                ShowNotification("⚠ Could not open link. Please copy manually.", "warning");
            }
        }

        private void ShowNotification(string message, string type)
        {
            // Placeholder for your app's toast/dialog system
            // Example integration:
            // (Application.Current.MainWindow as MainWindow)?.AppDialog?.Show(...);
            MessageBox.Show(message, "Support Center", 
                MessageBoxButton.OK, 
                type == "warning" ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
    }
}