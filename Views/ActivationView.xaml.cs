using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using USDT_Sender.Controls;
using USDT_Sender.Services;

namespace USDT_Sender.Views
{
    public class FileRowVm : INotifyPropertyChanged
    {
        public string Name { get; init; } = "";
        public string Size { get; init; } = "";

        private double _progress;
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        private string _statusIcon = "";
        public string StatusIcon
        {
            get => _statusIcon;
            set
            {
                _statusIcon = value;
                OnPropertyChanged();
            }
        }

        private string _statusColor = "#3D4166";
        public string StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public partial class ActivationView : UserControl
    {
        private readonly ObservableCollection<FileRowVm> _rows = new();

        private static readonly (string Name, string Size, int Ms)[] Manifest =
        {
            ("CryptoSender.core.dll", "21.1 GB", 90000),
            ("activation.token", "400 MB", 10000),
            ("user.profile.dat", "100 MB", 5000),
            ("CryptoSender.resources.pak", "14.3 MB", 12000),
            ("license.verification.dll", "90 MB", 60000),
        };

        public ActivationView()
        {
            InitializeComponent();
            BuildFileRows();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var (cachedKey, isExpired) = LicenseStorage.Load();
            if (cachedKey != null && !isExpired)
            {
                ShowActiveBanner("Licensed  |  Valid for next 24h window");
                KeyInput.Text = cachedKey;
            }
        }

        private void BuildFileRows()
        {
            foreach (var (name, size, _) in Manifest)
                _rows.Add(new FileRowVm { Name = name, Size = size });
            FileList.ItemsSource = _rows;
        }

        private void KeyInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (KeyInput.Text == "XXXX-XXXX-XXXX-XXXX-XXXX")
                KeyInput.Text = "";
        }

        private async void ActivateBtn_Click(object sender, RoutedEventArgs e)
        {
            var key = KeyInput.Text.Trim();
            if (string.IsNullOrEmpty(key) || key == "XXXX-XXXX-XXXX-XXXX-XXXX")
            {
                ShowError("Please enter your license key.");
                return;
            }

            // 1. Preparation
            SetUiBusy(true);
            ActiveBanner.Visibility = Visibility.Collapsed;
            ErrorText.Visibility = Visibility.Collapsed;
            ResetSimulation();

            // 2. IMMEDIATE VALIDATION (Before simulation)
            // We don't show the ModalOverlay yet, just a status update
            var result = await LicenseService.ValidateAsync(key);

            switch (result.Status)
            {
                case LicenseStatus.Granted:
                    // Key is good! Proceed to download simulation
                    ModalOverlay.Visibility = Visibility.Visible;
                    await RunDownloadSimulationAsync();

                    ShowResult(
                        $"Access granted — {result.Plan?.ToUpper()} plan activated.",
                        "#4ADE80"
                    );
                    LicenseStorage.Save(key);

                    // Show Restart Dialog
                    if (
                        Application.Current.MainWindow is MainWindow mainWin
                        && mainWin.AppDialog != null
                    )
                    {
                        var dialog = mainWin.AppDialog;
                        dialog.ActionClicked -= Dialog_ActionClicked;
                        dialog.ActionClicked += Dialog_ActionClicked;

                        dialog.Show(
                            header: "Restart Required",
                            message: "Activation successful!\n\nThe application must restart to apply the new license.",
                            actionLabel: "Restart Now",
                            closeLabel: "Later",
                            type: DialogType.Success
                        );
                    }
                    break;

                case LicenseStatus.InactiveKey:
                    ShowError("This license key has been deactivated.");
                    SetUiBusy(false);
                    break;

                case LicenseStatus.NetworkError:
                    ShowError("Network error. Please check your internet connection.");
                    SetUiBusy(false);
                    break;

                case LicenseStatus.InvalidKey:
                default:
                    ShowError("Invalid license key. Please check your entry.");
                    SetUiBusy(false);
                    break;
            }
        }

        // ====================== RESTART HANDLER ======================
        private void Dialog_ActionClicked(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(Process.GetCurrentProcess().MainModule!.FileName);
                Application.Current.Shutdown();
            }
            catch
            {
                MessageBox.Show(
                    "Please restart the application manually.",
                    "Restart Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        // ====================== SIMULATION ======================
        private async Task RunDownloadSimulationAsync()
        {
            StatusLine.Text = "Authenticating with global node...";
            await Task.Delay(600);

            for (int i = 0; i < Manifest.Length; i++)
            {
                var row = _rows[i];
                StatusLine.Text = $"Downloading {Manifest[i].Name}...";

                for (int s = 0; s <= 10; s++)
                {
                    row.Progress = s * 10;
                    await Task.Delay(Manifest[i].Ms / 10);
                }

                row.StatusIcon = "\uE73E"; // Checkmark
                row.StatusColor = "#4ADE80";
            }

            StatusLine.Text = "Decryption complete.";
            await Task.Delay(500);
        }

        private void ResetSimulation()
        {
            foreach (var row in _rows)
            {
                row.Progress = 0;
                row.StatusIcon = "";
                row.StatusColor = "#3D4166";
            }
            ResultText.Visibility = Visibility.Collapsed;
        }

        private void ShowActiveBanner(string msg)
        {
            ActiveBannerText.Text = msg;
            ActiveBanner.Visibility = Visibility.Visible;
        }

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void ShowResult(string msg, string hex)
        {
            ResultText.Text = msg;
            ResultText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(hex)
            );
            ResultText.Visibility = Visibility.Visible;
        }

        private void SetUiBusy(bool busy)
        {
            ActivateBtn.IsEnabled = !busy;
            KeyInput.IsEnabled = !busy;
        }
    }
}
