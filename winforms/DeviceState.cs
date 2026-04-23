using System;
using System.Collections.Generic;

namespace Opus.Cachers
{
    public class DeviceState
    {
        // Core identity
        public string DeviceId { get; set; } = "";
        public string HwidHash { get; set; } = "";
        public string Name { get; set; } = "";

        // Device snapshot fields (from device_info payload)
        public DateTime LastSeenUtc { get; set; } = DateTime.MinValue;
        public long UptimeSec { get; set; }

        public int BatteryPct { get; set; }
        public decimal BatteryTempC { get; set; }

        public int RamUsedMb { get; set; }
        public int RamFreeMb { get; set; }
        public int StorageFreeMb { get; set; }

        public int PingMs { get; set; }
        public string Charging { get; set; } = ""; // e.g., Charging/Discharging
        public string Network { get; set; } = "";  // e.g., WiFi/Mobile/Unknown

        // Derived values for UI cards
        public int ActiveAccounts { get; set; }    // unique packages active in last 10m
        public int MaxAccounts { get; set; }       // unique packages total (cap in UI if needed)

        // Optional “offline” helper
        public bool IsOnline(int activeWindowMinutes = 10)
            => LastSeenUtc >= DateTime.UtcNow.AddMinutes(-activeWindowMinutes);

        // Account cache per username
        public Dictionary<string, AccountState> AccountsByUsername { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);
    }

    public class AccountState
    {
        public long AccountId { get; set; }
        public string Username { get; set; } = "";
        public string PackageName { get; set; } = "";

        public DateTime LastEventUtc { get; set; } = DateTime.MinValue;

        // Flexible payload fields (honey, hive_size, etc.)
        public Dictionary<string, object?> Values { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);
        // Timeline cache for charting.
        public List<AccountMetricPoint> MetricTimeline { get; set; } = new();
        public bool IsActive(int minutes = 10)
            => LastEventUtc >= DateTime.UtcNow.AddMinutes(-minutes);
    }
    public class AccountMetricPoint
    {
        public DateTime EventTimeUtc { get; set; }
        public decimal Honey { get; set; }
        public decimal HiveSize { get; set; }
    }
}
