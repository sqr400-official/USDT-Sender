using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using USDT_Sender.Controls;
using USDT_Sender.Models;
using USDT_Sender.Services;

namespace USDT_Sender.Views
{
    // FIX CS0103: Added the missing enum definition

    public partial class BillingView : UserControl
    {
        // ── State ──────────────────────────────────────────
        private bool _isLoaded;
        private string _generatedOtp = "";
        private decimal _amountDue = 248.00m;

        private bool _amountOk;
        private bool _walletOk;
        private bool _otpOk;

        private readonly DispatcherTimer _otpTimer;
        private int _otpCountdown;

        private static readonly SolidColorBrush BrAccent = new SolidColorBrush(
            Color.FromRgb(0x7B, 0x6E, 0xF6)
        );
        private static readonly SolidColorBrush BrDanger = new SolidColorBrush(
            Color.FromRgb(0xFF, 0x5C, 0x6E)
        );
        private static readonly SolidColorBrush BrGreen = new SolidColorBrush(
            Color.FromRgb(0x2D, 0xD4, 0xBF)
        );

        public BillingView()
        {
            InitializeComponent();
            _isLoaded = true;

            _otpTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _otpTimer.Tick += OtpTimer_Tick;

            GenerateOtp();
            ValidateAmount();
        }

        public void LoadPayment(decimal amount, string invoiceRef = "INV-0001")
        {
            _amountDue = amount;
            TxtAmountToSend.Text = amount.ToString("F2", CultureInfo.InvariantCulture);
            TxtHeroAmount.Text = $"${amount:N2}";
            TxtHeroRef.Text = $"REF: {invoiceRef}";
            ValidateAmount();
        }

        private void TxtAmountToSend_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var isDecimal = e.Text == decimalSeparator;

            // Allow numbers and decimal separators
            e.Handled =
                !double.TryParse(e.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                && !isDecimal;
        }

        private void TxtAmountToSend_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = e.DataObject.GetData(typeof(string)) as string;
                // FIX CS8604: Passed empty string if text is null
                if (!IsValidAmountString(text ?? ""))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private bool IsValidAmountString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;
            var clean = Regex.Replace(input, @"[^\d.,]", "");
            return decimal.TryParse(
                    clean,
                    NumberStyles.Currency,
                    CultureInfo.InvariantCulture,
                    out var result
                )
                && result > 0;
        }

        private void TxtAmountToSend_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateAmount();
            UpdateProceedButton();
        }

        private void ValidateAmount()
        {
            // Safeguard for early initialization
            if (TxtAmountToSend == null || TxtHeroAmount == null)
                return;

            var text = TxtAmountToSend.Text.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                _amountOk = false;
                if (ErrAmount != null)
                    ErrAmount.Visibility = Visibility.Collapsed;
                return;
            }

            if (
                decimal.TryParse(
                    text,
                    NumberStyles.Currency,
                    CultureInfo.InvariantCulture,
                    out var amount
                )
            )
            {
                _amountOk = amount > 0;
                _amountDue = amount;
                TxtHeroAmount.Text = $"${amount:N2}";

                if (ErrAmount != null)
                    ErrAmount.Visibility = _amountOk ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                _amountOk = false;
                if (ErrAmount != null)
                    ErrAmount.Visibility = Visibility.Visible;
            }
        }

        // ── OTP & Other Logic (Logic remains same as your original) ──
        private void GenerateOtp()
        {
            _generatedOtp = new Random().Next(100000, 999999).ToString();
            TxtOtpHint.Text = $"Code: {_generatedOtp}";
            foreach (var box in new[] { Otp1, Otp2, Otp3, Otp4, Otp5, Otp6 })
                box.Text = "";
            _otpOk = false;
            ErrOtp.Visibility = Visibility.Collapsed;
            OtpBadge.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0x2D, 0xD4, 0xBF));
            TxtOtpStatus.Text = "GENERATED";
            TxtOtpStatus.Foreground = BrGreen;
            _otpCountdown = 60;
            BtnResendOtp.IsEnabled = false;
            BtnResendOtp.Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x6B, 0x88));
            _otpTimer.Start();
            UpdateProceedButton();
        }

        private void OtpTimer_Tick(object? sender, EventArgs e)
        {
            _otpCountdown--;
            TxtOtpHint.Text =
                _otpCountdown > 0 ? $"Code: {_generatedOtp} ({_otpCountdown}s)" : "Code expired.";
            if (_otpCountdown <= 0)
            {
                _otpTimer.Stop();
                BtnResendOtp.IsEnabled = true;
                BtnResendOtp.Foreground = BrAccent;
                TxtOtpStatus.Text = "EXPIRED";
                TxtOtpStatus.Foreground = BrDanger;
            }
        }

        private void BtnResendOtp_Click(object sender, RoutedEventArgs e) => GenerateOtp();

        private void OtpDigit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb)
                return;
            if (!string.IsNullOrEmpty(tb.Text) && !char.IsDigit(tb.Text[0]))
            {
                tb.Text = "";
                return;
            }
            if (tb.Text.Length == 1)
            {
                var next = tb.Tag?.ToString() switch
                {
                    "1" => Otp2,
                    "2" => Otp3,
                    "3" => Otp4,
                    "4" => Otp5,
                    "5" => Otp6,
                    _ => null,
                };
                next?.Focus();
            }
            ValidateOtp();
        }

        private void OtpDigit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Back || sender is not TextBox tb || tb.Text.Length != 0)
                return;
            var prev = tb.Tag?.ToString() switch
            {
                "2" => Otp1,
                "3" => Otp2,
                "4" => Otp3,
                "5" => Otp4,
                "6" => Otp5,
                _ => null,
            };
            prev?.Focus();
        }

        private void ValidateOtp()
        {
            var entered = Otp1.Text + Otp2.Text + Otp3.Text + Otp4.Text + Otp5.Text + Otp6.Text;
            if (entered.Length < 6)
            {
                _otpOk = false;
                ErrOtp.Visibility = Visibility.Collapsed;
            }
            else
            {
                _otpOk = entered == _generatedOtp;
                ErrOtp.Visibility = _otpOk ? Visibility.Collapsed : Visibility.Visible;
                var brush = _otpOk ? BrGreen : BrDanger;
                foreach (var b in new[] { Otp1, Otp2, Otp3, Otp4, Otp5, Otp6 })
                    b.BorderBrush = brush;
            }
            UpdateProceedButton();
        }

        private void TxtWallet_TextChanged(object sender, TextChangedEventArgs e)
        {
            _walletOk = TxtWalletAddress.Text.Trim().Length >= 26;
            IcoWallet.Visibility = _walletOk ? Visibility.Visible : Visibility.Collapsed;
            ErrWallet.Visibility =
                (!_walletOk && TxtWalletAddress.Text.Length > 0)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            UpdateProceedButton();
        }

        private void UpdateProceedButton()
        {
            if (!_isLoaded)
                return;
            BtnProceed.IsEnabled = _amountOk && _walletOk && _otpOk;
        }

        private async void BtnProceed_Click(object sender, RoutedEventArgs e)
        {
            // Use dynamic cast to avoid strict typing errors if MainWindow isn't fully defined yet
            dynamic mainWin = Application.Current.MainWindow;
            var dialog = mainWin?.AppDialog;
            var loader = mainWin?.AppLoader;

            loader?.Show("Connecting to server…");
            await Task.Delay(2000);
            loader?.UpdateMessage("Authenticating…", "Checking credentials.");
            await Task.Delay(2000);
            loader?.UpdateMessage("Processing Payment", "Please wait...");
            await Task.Delay(4000);
            loader?.Hide();

            // ── Build Transaction ID ──────────────────────────────────────
            var txId = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            // ── Resolve selected crypto label ──────────────────────────────
            string cryptoLabel = "USDT (TRC-20)";
            if (CmbCrypto.SelectedItem is ComboBoxItem selectedItem)
            {
                // Strip leading icon character (e.g., "◈  USDT (TRC-20)" → "USDT (TRC-20)")
                var raw = selectedItem.Content?.ToString() ?? "";
                var parts = raw.Split(new[] { "  " }, 2, StringSplitOptions.None);
                cryptoLabel = parts.Length > 1 ? parts[1].Trim() : raw.Trim();
            }

            // ── Persist to local storage ───────────────────────────────────
            var newTransaction = new TransactionRecord
            {
                Date          = DateTime.Now,
                Type          = "Send",
                Amount        = _amountDue,
                Crypto        = cryptoLabel,
                Status        = "Completed",
                Txid          = txId,
                WalletAddress = TxtWalletAddress.Text.Trim()
            };

            TransactionStorageService.AddTransaction(newTransaction);

            // ── Show success dialog ────────────────────────────────────────
            dialog?.Show(
                "Payment Confirmed",
                $"Payment of ${_amountDue:N2} confirmed.\n\n"
                    + $"Transaction ID: {txId}",
                actionLabel: "View Receipt",
                closeLabel: "Done",
                type: DialogType.Success
            );
        }
    }
}
