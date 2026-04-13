using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace USDT_Sender.Controls
{
    public partial class UniversalLoader : UserControl
    {
        // ── Auto-dismiss timer (used when Duration > 0) ───
        private readonly DispatcherTimer _dismissTimer = new DispatcherTimer();
        private readonly DispatcherTimer _elapsedTimer = new DispatcherTimer();
        private int _elapsedSeconds;

        // ── Storyboard refs ───────────────────────────────
        private Storyboard SpinAnim     => (Storyboard)Resources["SpinAnim"];
        private Storyboard PulseAnim    => (Storyboard)Resources["PulseAnim"];
        private Storyboard ProgressAnim => (Storyboard)Resources["ProgressAnim"];
        private Storyboard FadeInAnim   => (Storyboard)Resources["FadeIn"];
        private Storyboard FadeOutAnim  => (Storyboard)Resources["FadeOut"];

        public UniversalLoader()
        {
            InitializeComponent();

            _dismissTimer.Tick += (_, _) => Hide();

            _elapsedTimer.Interval = TimeSpan.FromSeconds(1);
            _elapsedTimer.Tick += (_, _) =>
            {
                _elapsedSeconds++;
                TxtElapsed.Text = $"{_elapsedSeconds}s elapsed";
            };
        }

        // ══════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════

        /// <summary>
        /// Show the loader.
        /// </summary>
        /// <param name="message">Primary loading message.</param>
        /// <param name="subMessage">Optional subtle sub-message below.</param>
        /// <param name="duration">
        ///     Optional duration in seconds. If > 0 a progress bar is shown
        ///     and the loader auto-dismisses when complete.
        ///     If 0 (default) the loader runs indefinitely until Hide() is called.
        /// </param>
        public void Show(
            string message    = "Please wait…",
            string subMessage = "This won't take long.",
            int    duration   = 0)
        {
            TxtLoaderMessage.Text = message;
            TxtLoaderSub.Text     = subMessage;

            // Progress bar — only when a duration is given
            if (duration > 0)
            {
                ProgressContainer.Visibility = Visibility.Visible;
                TxtElapsed.Visibility        = Visibility.Visible;

                // Scale the animation duration to match
                var anim = (DoubleAnimation)ProgressAnim.Children[0];
                anim.Duration = new Duration(TimeSpan.FromSeconds(duration));
                ProgressFill.Width = 0;
                ProgressAnim.Begin();

                _dismissTimer.Interval = TimeSpan.FromSeconds(duration);
                _dismissTimer.Start();
            }
            else
            {
                ProgressContainer.Visibility = Visibility.Collapsed;
                TxtElapsed.Visibility        = Visibility.Collapsed;
            }

            // Elapsed counter
            _elapsedSeconds = 0;
            TxtElapsed.Text = "0s elapsed";
            _elapsedTimer.Start();

            // Make visible then animate in
            Visibility = Visibility.Visible;
            SpinAnim.Begin();
            PulseAnim.Begin();
            FadeInAnim.Begin();
        }

        /// <summary>
        /// Manually dismiss the loader (always safe to call).
        /// </summary>
        public void Hide()
        {
            _dismissTimer.Stop();
            _elapsedTimer.Stop();
            ProgressAnim.Stop();
            SpinAnim.Stop();
            PulseAnim.Stop();
            FadeOutAnim.Begin();
        }

        /// <summary>
        /// Update the message while the loader is already visible.
        /// </summary>
        public void UpdateMessage(string message, string subMessage = "")
        {
            TxtLoaderMessage.Text = message;
            if (!string.IsNullOrWhiteSpace(subMessage))
                TxtLoaderSub.Text = subMessage;
        }

        // ── Collapse after fade-out completes ─────────────
        private void FadeOut_Completed(object sender, EventArgs e)
            => Visibility = Visibility.Collapsed;
    }
}