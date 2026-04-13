using System;
using System.Collections.Generic;

namespace USDT_Sender.Models
{
    public class LicenseEntry
    {
        public string Key       { get; set; } = "";
        public string Plan      { get; set; } = "";
        public bool   Active    { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class LicenseManifest
    {
        public List<LicenseEntry> Keys { get; set; } = new List<LicenseEntry>();
    }

    public class LocalLicense
    {
        public string   Key        { get; set; } = "";
        public string   Plan       { get; set; } = "";
        public string   DeviceId   { get; set; } = "";
        public DateTime GrantedAt  { get; set; }
        public DateTime ExpiresAt  { get; set; }
    }

    public class LicenseResult
    {
        public bool   Success { get; set; }
        public string Reason  { get; set; } = "";
        public string Plan    { get; set; } = "";
    }
}