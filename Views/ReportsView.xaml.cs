using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace USDT_Sender.Views
{
    // ═══════════════════════════════════════════════════════════
    //  TRANSACTION MODEL - Represents a single transaction
    // ═══════════════════════════════════════════════════════════
    public class Transaction : INotifyPropertyChanged
    {
        private string _status;
        private bool _canRetry;

        public DateTime Date { get; set; }
        public string Type { get; set; } = "Send";
        public decimal Amount { get; set; }
        public string Crypto { get; set; } = "USDT";

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                UpdateCanRetry();
            }
        }

        public string Txid { get; set; }
        public string WalletAddress { get; set; }

        public bool CanRetry
        {
            get => _canRetry;
            private set
            {
                _canRetry = value;
                OnPropertyChanged();
            }
        }

        private void UpdateCanRetry()
        {
            CanRetry = string.Equals(Status, "Failed", StringComparison.OrdinalIgnoreCase);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  VOLUME DATA POINT - For the chart
    // ═══════════════════════════════════════════════════════════
    public class VolumeDataPoint
    {
        public DateTime Day { get; set; }
        public decimal Amount { get; set; }
        public static decimal MaxAmount { get; set; } = 1000;

        // Height percentage for the bar (0-100)
        public double Height
        {
            get
            {
                if (MaxAmount <= 0)
                    return 5;
                double percent = (double)(Amount / MaxAmount * 100);
                return Math.Min(100, Math.Max(5, percent));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  CONVERTERS - Help with data binding
    // ═══════════════════════════════════════════════════════════

    // Converts bool to Visibility
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }

    // Converts percentage to actual pixel height
    public class PercentToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double pct)
                return Math.Max(4, pct * 0.5); // Max 50px height
            return 4.0;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            throw new NotImplementedException();
        }
    }

    // Truncates long strings (like TXID)
    public class TruncateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            string text = value.ToString();
            int length = 8; // Default

            if (parameter != null)
            {
                if (parameter is int i)
                    length = i;
                else if (int.TryParse(parameter.ToString(), out int parsed))
                    length = parsed;
            }

            if (text.Length <= length)
                return text;

            return text.Substring(0, length) + "...";
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            throw new NotImplementedException();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  MAIN REPORTS VIEW
    // ═══════════════════════════════════════════════════════════
    public partial class ReportsView : UserControl, INotifyPropertyChanged
    {
        // Private fields
        private ObservableCollection<Transaction> _transactions;
        private ObservableCollection<VolumeDataPoint> _volumeData;
        private string _searchQuery = "";
        private string _statusFilter = "All Status";

        // Constructor
        public ReportsView()
        {
            InitializeComponent();

            // Set DataContext FIRST
            DataContext = this;

            // Initialize collections
            _transactions = new ObservableCollection<Transaction>();
            _volumeData = new ObservableCollection<VolumeDataPoint>();

            // Load data
            LoadSampleData();
            UpdateSummary();
            ApplyFilters();
        }

        // ═══════════════════════════════════════════════════════════
        //  PUBLIC PROPERTIES (For data binding)
        // ═══════════════════════════════════════════════════════════

        public ObservableCollection<Transaction> Transactions
        {
            get => _transactions;
            set
            {
                _transactions = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<VolumeDataPoint> VolumeData
        {
            get => _volumeData;
            set
            {
                _volumeData = value;
                OnPropertyChanged();
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // ═══════════════════════════════════════════════════════════
        //  SAMPLE DATA - Replace with real API calls
        // ═══════════════════════════════════════════════════════════

        private void LoadSampleData()
        {
            var random = new Random();
            string[] cryptos = { "USDT (TRC-20)", "USDT (ERC-20)", "BTC", "ETH", "SOL" };
            string[] statuses = { "Completed", "Completed", "Completed", "Pending", "Failed" };
            string[] types = { "Send", "Send", "Send", "Send", "Receive" };

            _transactions.Clear();

            // Create 47 sample transactions
            for (int i = 0; i < 10; i++)
            {
                var tx = new Transaction
                {
                    Date = DateTime.Now.AddDays(-random.Next(0, 30)).AddHours(-random.Next(0, 24)),
                    Type = types[random.Next(types.Length)],
                    Amount = Math.Round((decimal)(random.NextDouble() * 500 + 10), 2),
                    Crypto = cryptos[random.Next(cryptos.Length)],
                    Status = statuses[random.Next(statuses.Length)],
                    Txid = GenerateMockTxid(),
                    WalletAddress = GenerateMockWallet(),
                };

                _transactions.Add(tx);
            }

            // Sort by date (newest first)
            var sorted = _transactions.OrderByDescending(t => t.Date).ToList();
            _transactions.Clear();
            foreach (var tx in sorted)
                _transactions.Add(tx);

            // Generate chart data
            GenerateVolumeData();

            // Update UI
            DgTransactions.ItemsSource = _transactions;
            IcVolumeChart.ItemsSource = _volumeData;
        }

        private string GenerateMockTxid()
        {
            var random = new Random();
            char[] chars = "0123456789abcdef".ToCharArray();
            char[] result = new char[64];

            for (int i = 0; i < 64; i++)
                result[i] = chars[random.Next(chars.Length)];

            return new string(result);
        }

        private string GenerateMockWallet()
        {
            var random = new Random();
            char[] chars = "0123456789abcdef".ToCharArray();
            char[] result = new char[40];

            for (int i = 0; i < 40; i++)
                result[i] = chars[random.Next(chars.Length)];

            return "0x" + new string(result);
        }

        private void GenerateVolumeData()
        {
            _volumeData.Clear();
            var today = DateTime.Today;

            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);

                // Sum completed transactions for this day
                var vol = _transactions
                    .Where(t => t.Date.Date == day && t.Status == "Completed")
                    .Sum(t => t.Amount);

                _volumeData.Add(new VolumeDataPoint { Day = day, Amount = vol });
            }

            // Set max amount for scaling the chart
            var maxVol = _volumeData.Max(v => v.Amount);
            VolumeDataPoint.MaxAmount = maxVol > 0 ? maxVol : 1000;
        }

        // ═══════════════════════════════════════════════════════════
        //  FILTERING & SEARCH
        // ═══════════════════════════════════════════════════════════

        private void ApplyFilters()
        {
            if (_transactions == null || _transactions.Count == 0)
                return;

            // Start with all transactions
            var filtered = _transactions.AsEnumerable();

            // Filter by status
            if (!string.IsNullOrEmpty(_statusFilter) && _statusFilter != "All Status")
            {
                filtered = filtered.Where(t => t.Status == _statusFilter);
            }

            // Filter by search query
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                string query = _searchQuery.ToLowerInvariant();
                filtered = filtered.Where(t =>
                    (t.Txid != null && t.Txid.ToLowerInvariant().Contains(query))
                    || (
                        t.WalletAddress != null
                        && t.WalletAddress.ToLowerInvariant().Contains(query)
                    )
                    || t.Amount.ToString("F2").Contains(query)
                    || (t.Crypto != null && t.Crypto.ToLowerInvariant().Contains(query))
                );
            }

            // Convert to list
            var filteredList = filtered.OrderByDescending(t => t.Date).ToList();

            // Update DataGrid
            DgTransactions.ItemsSource = filteredList;

            // Update record count
            TxtRecordCount.Text =
                $"{filteredList.Count} record{(filteredList.Count != 1 ? "s" : "")}";

            // Show/hide empty state
            PnlEmptyState.Visibility =
                filteredList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateSummary()
        {
            if (_transactions == null || _transactions.Count == 0)
                return;

            try
            {
                // Calculate totals
                decimal totalSent = _transactions
                    .Where(t => t.Status == "Completed")
                    .Sum(t => t.Amount);
                int pendingCount = _transactions.Count(t => t.Status == "Pending");
                int completedCount = _transactions.Count(t => t.Status == "Completed");
                int failedCount = _transactions.Count(t => t.Status == "Failed");

                // Update UI
                TxtTotalSent.Text = $"${totalSent:N2}";
                TxtPendingCount.Text = pendingCount.ToString();
                TxtCompletedCount.Text = completedCount.ToString();
                TxtFailedCount.Text = failedCount.ToString();

                // Random trend indicator (just for demo)
                var random = new Random();
                int change = random.Next(-15, 20);
                TxtTotalSentChange.Text = $"{(change >= 0 ? "+" : "")}{change}% vs last month";

                // Use resources for colors
                TxtTotalSentChange.Foreground =
                    change >= 0 ? (Brush)FindResource("Green") : (Brush)FindResource("Danger");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating summary: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = TxtSearch.Text;
            ApplyFilters();
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbStatusFilter.SelectedItem is ComboBoxItem item)
            {
                _statusFilter = item.Content.ToString();
                ApplyFilters();
            }
        }

        private void DgTransactions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Handle row selection
        }

        // ═══════════════════════════════════════════════════════════
        //  ACTION BUTTON HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void BtnCopyTxid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string txid)
            {
                Clipboard.SetText(txid);
                ShowMessage("TXID copied to clipboard", false);
            }
        }

        private void BtnViewExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string txid)
            {
                // Choose explorer based on TXID format
                string explorerUrl = txid.StartsWith("0x")
                    ? $"https://etherscan.io/tx/{txid}"
                    : $"https://tronscan.org/#/transaction/{txid}";

                OpenUrl(explorerUrl);
            }
        }

        private void BtnRetryTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Transaction tx)
            {
                var result = MessageBox.Show(
                    $"Retry transaction of ${tx.Amount:N2}?\n\nThis will create a new transaction with current rates.",
                    "Confirm Retry",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    ShowMessage("Retry initiated - check your wallet for confirmation", false);
                    // TODO: Call your payment service here
                }
            }
        }

        private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            ExportTransactions("csv");
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            ExportTransactions("pdf");
        }

        private void ExportTransactions(string format)
        {
            // Get currently displayed transactions
            var items = DgTransactions.ItemsSource as IEnumerable<Transaction>;

            if (items == null || !items.Any())
            {
                ShowMessage("No transactions to export", true);
                return;
            }

            // Disable buttons during export
            BtnExportCsv.IsEnabled = false;
            BtnExportPdf.IsEnabled = false;

            try
            {
                string filename = $"USDT_Reports_{DateTime.Now:yyyyMMdd_HHmm}.{format}";
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, filename);

                if (format == "csv")
                {
                    // Create CSV content
                    var lines = new List<string> { "Date,Type,Amount,Crypto,Status,TXID,Wallet" };

                    foreach (var tx in items)
                    {
                        lines.Add(
                            $"{tx.Date:yyyy-MM-dd HH:mm},{tx.Type},{tx.Amount:F2},{tx.Crypto},{tx.Status},{tx.Txid},{tx.WalletAddress}"
                        );
                    }

                    File.WriteAllText(filePath, string.Join("\n", lines));
                }
                else // pdf
                {
                    // Simple text file as placeholder (you can add PDF library later)
                    var content = new List<string>
                    {
                        "USDT SENDER - TRANSACTION REPORT",
                        $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}",
                        $"Total Transactions: {items.Count()}",
                        "",
                        "DATE\t\tTYPE\tAMOUNT\tCRYPTO\tSTATUS",
                    };

                    foreach (var tx in items.Take(50)) // Limit to 50 for demo
                    {
                        content.Add(
                            $"{tx.Date:yyyy-MM-dd}\t{tx.Type}\t${tx.Amount:F2}\t{tx.Crypto}\t{tx.Status}"
                        );
                    }

                    File.WriteAllText(filePath, string.Join("\n", content));
                }

                ShowMessage($"Exported to Desktop\\{filename}", false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Export failed: {ex.Message}", true);
            }
            finally
            {
                // Re-enable buttons
                BtnExportCsv.IsEnabled = true;
                BtnExportPdf.IsEnabled = true;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        private void OpenUrl(string url)
        {
            try
            {
                var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                ShowMessage($"Could not open link: {ex.Message}", true);
            }
        }

        private void ShowMessage(string message, bool isError)
        {
            MessageBox.Show(
                message,
                "Reports",
                MessageBoxButton.OK,
                isError ? MessageBoxImage.Warning : MessageBoxImage.Information
            );
        }
    }
}
