using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace USDT_Sender.Services
{
    /// <summary>
    /// Persists the license key + 24-hour grant window to an encrypted local file.
    /// Encryption uses Windows DPAPI with the device ID as additional entropy,
    /// making the file machine-bound and tamper-proof.
    /// </summary>
    public static class LicenseStorage
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CryptoSender",
            "license.dat"
        );

        private record LicenseCache(string Key, DateTime GrantedAt, DateTime ExpiresAt);

        public static void Save(string key)
        {
            var cache = new LicenseCache(key, DateTime.UtcNow, DateTime.UtcNow.AddHours(24));
            var json = JsonSerializer.Serialize(cache);
            var plainBytes = Encoding.UTF8.GetBytes(json);
            var entropy = Encoding.UTF8.GetBytes(DeviceIdHelper.GetDeviceId());
            var encrypted = ProtectedData.Protect(
                plainBytes,
                entropy,
                DataProtectionScope.CurrentUser
            );

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllBytes(FilePath, encrypted);
        }

        public static (string? Key, bool IsExpired) Load()
        {
            if (!File.Exists(FilePath))
                return (null, true);

            try
            {
                var encrypted = File.ReadAllBytes(FilePath);
                var entropy = Encoding.UTF8.GetBytes(DeviceIdHelper.GetDeviceId());
                var plainBytes = ProtectedData.Unprotect(
                    encrypted,
                    entropy,
                    DataProtectionScope.CurrentUser
                );
                var json = Encoding.UTF8.GetString(plainBytes);
                var cache = JsonSerializer.Deserialize<LicenseCache>(json);

                if (cache is null)
                    return (null, true);

                bool expired = DateTime.UtcNow >= cache.ExpiresAt;
                return (cache.Key, expired);
            }
            catch
            {
                // Corrupt or tampered file — treat as no license
                TryDelete();
                return (null, true);
            }
        }

        public static void TryDelete()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch { }
        }
    }
}
