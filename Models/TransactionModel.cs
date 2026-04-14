using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace USDT_Sender.Models
{
    /// <summary>
    /// Represents a single USDT / crypto transaction created from the Billing view
    /// and displayed in the Reports view.
    /// </summary>
    public class TransactionRecord : INotifyPropertyChanged
    {
        private string _status = "Completed";
        private bool _canRetry;

        public DateTime Date { get; set; }
        public string Type { get; set; } = "Send";
        public decimal Amount { get; set; }
        public string Crypto { get; set; } = "USDT (TRC-20)";

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

        public string Txid { get; set; } = string.Empty;
        public string WalletAddress { get; set; } = string.Empty;

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
