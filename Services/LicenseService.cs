using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace USDT_Sender.Services
{
    public enum LicenseStatus
    {
        Granted,
        InvalidKey,
        InactiveKey,
        NetworkError,
    }

    public record LicenseResult(LicenseStatus Status, string? Plan = null);

    /// <summary>
    /// Fetches the remote license list from GitHub and validates keys against it.
    /// This is the single source of truth — local storage is only a 24h cache.
    /// </summary>
    public static class LicenseService
    {
        // ⚠️ Replace with your actual GitHub raw URL after uploading licenses.json
        private static readonly string RemoteUrl =
            "https://raw.githubusercontent.com/sqr400-official/APIs/refs/heads/main/licenses.json";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

        private record RemoteKey(string Key, string Plan, bool Active);

        private record RemoteFile(List<RemoteKey> Keys);

        /// <summary>
        /// Full validation: fetch remote list and check if key exists and is active.
        /// </summary>
        public static async Task<LicenseResult> ValidateAsync(string key)
        {
            List<RemoteKey> remoteKeys;
            try
            {
                remoteKeys = await FetchRemoteKeysAsync();
            }
            catch
            {
                return new LicenseResult(LicenseStatus.NetworkError);
            }

            var match = remoteKeys.Find(k =>
                string.Equals(k.Key, key.Trim(), StringComparison.OrdinalIgnoreCase)
            );

            if (match is null)
                return new LicenseResult(LicenseStatus.InvalidKey);
            if (!match.Active)
                return new LicenseResult(LicenseStatus.InactiveKey);

            LicenseStorage.Save(key.Trim());
            return new LicenseResult(LicenseStatus.Granted, match.Plan);
        }

        /// <summary>
        /// Silent startup check: compares locally cached key against remote.
        /// Returns Granted only if the cached key is still valid remotely AND not expired locally.
        /// </summary>
        public static async Task<LicenseResult> StartupCheckAsync()
        {
            var (cachedKey, isExpired) = LicenseStorage.Load();

            // No local key → go straight to activation
            if (cachedKey is null)
                return new LicenseResult(LicenseStatus.InvalidKey);

            // 24h window expired → must re-validate remotely
            // (we always re-validate remotely regardless, but if expired we force activation on network error)
            List<RemoteKey> remoteKeys;
            try
            {
                remoteKeys = await FetchRemoteKeysAsync();
            }
            catch
            {
                // No offline access per requirements
                LicenseStorage.TryDelete();
                return new LicenseResult(LicenseStatus.NetworkError);
            }

            var match = remoteKeys.Find(k =>
                string.Equals(k.Key, cachedKey, StringComparison.OrdinalIgnoreCase)
            );

            if (match is null || !match.Active)
            {
                LicenseStorage.TryDelete();
                return new LicenseResult(LicenseStatus.InvalidKey);
            }

            // Key still valid remotely — refresh the 24h window
            LicenseStorage.Save(cachedKey);
            return new LicenseResult(LicenseStatus.Granted, match.Plan);
        }

        private static async Task<List<RemoteKey>> FetchRemoteKeysAsync()
        {
            var json = await Http.GetStringAsync(RemoteUrl);
            var file = JsonSerializer.Deserialize<RemoteFile>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            return file?.Keys ?? new List<RemoteKey>();
        }
    }
}
