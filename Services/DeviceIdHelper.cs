using Microsoft.Win32;
using System;
using System.Security.Cryptography;
using System.Text;

namespace USDT_Sender.Services
{
    /// <summary>
    /// Generates a stable, machine-bound device ID using only built-in APIs.
    /// No System.Management / WMI required.
    /// </summary>
    public static class DeviceIdHelper
    {
        private static string? _cached;

        public static string GetDeviceId()
        {
            if (_cached != null) return _cached;

            var raw = $"{Environment.MachineName}-{GetWindowsProductId()}-{Environment.UserName}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            _cached = Convert.ToHexString(bytes);
            return _cached;
        }

        private static string GetWindowsProductId()
        {
            try
            {
                using var key = Registry.LocalMachine
                    .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                return key?.GetValue("ProductId")?.ToString() ?? "NOPID";
            }
            catch
            {
                return "NOPID";
            }
        }
    }
}