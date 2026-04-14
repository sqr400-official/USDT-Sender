using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using USDT_Sender.Models;

namespace USDT_Sender.Services
{
    /// <summary>
    /// Persists completed transactions to a local JSON file so they survive
    /// app restarts and can be displayed in the Reports view.
    /// This is the WPF equivalent of browser "localStorage".
    /// </summary>
    public static class TransactionStorageService
    {
        // ── File path ─────────────────────────────────────────────────────────
        private static readonly string StoragePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CryptoSender",
            "transactions.json"
        );

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a new transaction record to the local store and saves to disk.
        /// </summary>
        public static void AddTransaction(TransactionRecord tx)
        {
            var list = LoadAll();
            list.Insert(0, tx); // newest first
            SaveAll(list);
        }

        /// <summary>
        /// Returns all stored transactions, newest-first.
        /// Returns an empty list if the file does not exist or is corrupt.
        /// </summary>
        public static List<TransactionRecord> LoadAll()
        {
            if (!File.Exists(StoragePath))
                return new List<TransactionRecord>();

            try
            {
                var json = File.ReadAllText(StoragePath);
                return JsonSerializer.Deserialize<List<TransactionRecord>>(json, JsonOptions)
                       ?? new List<TransactionRecord>();
            }
            catch
            {
                // Corrupt file – start fresh
                return new List<TransactionRecord>();
            }
        }

        /// <summary>
        /// Completely clears all stored transactions.
        /// </summary>
        public static void ClearAll()
        {
            if (File.Exists(StoragePath))
                File.Delete(StoragePath);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static void SaveAll(List<TransactionRecord> list)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StoragePath)!);
            var json = JsonSerializer.Serialize(list, JsonOptions);
            File.WriteAllText(StoragePath, json);
        }
    }
}
