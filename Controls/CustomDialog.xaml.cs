using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace USDT_Sender.Controls
{
    // ── Dialog type controls the icon + accent colour ──
    public enum DialogType { Info, Success, Warning, Danger }

    public partial class CustomDialog : UserControl
    {
        // ── Events the parent can subscribe to ────────────
        public event EventHandler? ActionClicked;
        public event EventHandler? CloseClicked;

        // ── Brushes keyed to DialogType ───────────────────
        private static readonly (string Icon, string BadgeBg, string IconFg)[] TypeTheme =
        {
            ("ℹ",  "#1A7B6EF6", "#FF7B6EF6"),   // Info
            ("✓",  "#1A2DD4BF", "#FF2DD4BF"),   // Success
            ("⚠",  "#1AFBBF24", "#FFFBBF24"),   // Warning
            ("✕",  "#1AFF5C6E", "#FFFF5C6E"),   // Danger
        };

        public CustomDialog()
        {
            InitializeComponent();
        }

        // ══════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════

        /// <summary>
        /// Show the dialog with full configuration.
        /// </summary>
        public void Show(
            string      header,
            string      message,
            string      actionLabel  = "Confirm",
            string      closeLabel   = "Cancel",
            DialogType  type         = DialogType.Info)
        {
            TxtHeader.Text  = header;
            TxtMessage.Text = message;
            BtnAction.Content = actionLabel;
            BtnClose.Content  = closeLabel;

            ApplyType(type);
            Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hide the dialog programmatically.
        /// </summary>
        public void Hide() => Visibility = Visibility.Collapsed;

        // ══════════════════════════════════════════════════
        //  BUTTON HANDLERS
        // ══════════════════════════════════════════════════

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            ActionClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            CloseClicked?.Invoke(this, EventArgs.Empty);
        }

        // ══════════════════════════════════════════════════
        //  THEMING
        // ══════════════════════════════════════════════════

        private void ApplyType(DialogType type)
        {
            var (icon, badgeBg, iconFg) = TypeTheme[(int)type];

            TxtIcon.Text       = icon;
            TxtIcon.Foreground = BrushFrom(iconFg);
            IconBadge.Background = BrushFrom(badgeBg);

            // Tint the action button to match type
            BtnAction.Background = type switch
            {
                DialogType.Success => BrushFrom("#FF2DD4BF"),
                DialogType.Warning => BrushFrom("#FFFBBF24"),
                DialogType.Danger  => BrushFrom("#FFFF5C6E"),
                _                  => BrushFrom("#FF7B6EF6"),
            };

            // Warning uses dark text for contrast
            BtnAction.Foreground = type == DialogType.Warning
                ? BrushFrom("#FF111118")
                : Brushes.White;
        }

        private static SolidColorBrush BrushFrom(string hex)
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(c);
        }
    }
}