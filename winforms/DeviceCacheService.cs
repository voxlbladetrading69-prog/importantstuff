using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Opus.Cachers;

namespace Opus
{
    public class DeviceCacheService
    {
        private readonly DbService _db;
        private readonly Dictionary<string, DeviceState> _devicesById = new(StringComparer.OrdinalIgnoreCase);
        private readonly ReaderWriterLockSlim _lock = new();
        private DateTime _lastEventUtc = DateTime.MinValue;

        public DeviceCacheService(string connString)
        {
            _db = new DbService(connString);
        }

        public async Task InitializeAsync(int initialLimit = 5000)
        {
            var rows = await _db.GetAllEventsAsync(initialLimit);
            rows.Reverse(); // oldest -> newest so each newer row overwrites previous state cleanly
            ApplyRows(rows);
        }

        public async Task<int> RefreshAsync()
        {
            var rows = await _db.GetEventsSinceAsync(_lastEventUtc);
            ApplyRows(rows);
            return rows.Count;
        }

        public List<Device> BuildDashboardDevices(int accountCap = 5)
        {
            _lock.EnterReadLock();
            try
            {
                return _devicesById.Values
                    .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(d =>
                    {
                        var isOnline = d.IsOnline();
                        var active = d.ActiveAccounts;
                        var max = Math.Min(accountCap, Math.Max(active, d.MaxAccounts));

                        var device = new Device(
                            name: string.IsNullOrWhiteSpace(d.Name) ? "Unknown Device" : d.Name,
                            activeAccounts: active,
                            maxAccounts: max,
                            lastSyncText: ToAgoText(d.LastSeenUtc),
                            statusColor: isOnline ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128),
                            deviceId: d.DeviceId
                        );

                        device.LastSyncUtc = d.LastSeenUtc;
                        device.FirstSeenUtc = DateTime.MinValue;
                        return device;
                    })
                    .ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void ApplyRows(List<AccountEventRow> rows)
        {
            if (rows.Count == 0) return;

            _lock.EnterWriteLock();
            try
            {
                foreach (var row in rows)
                {
                    var key = row.DeviceId.ToString();
                    if (!_devicesById.TryGetValue(key, out var device))
                    {
                        device = new DeviceState
                        {
                            DeviceId = key,
                            HwidHash = row.HwidHash,
                            Name = string.IsNullOrWhiteSpace(row.DeviceName) ? row.HwidHash : row.DeviceName
                        };
                        _devicesById[key] = device;
                    }

                    device.LastSeenUtc = MaxUtc(device.LastSeenUtc, row.EventTimeUtc);
                    UpsertAccount(device, row);
                    UpdateDerivedAccountCounts(device);
                    HydrateDeviceInfo(device, row.Values);

                    if (row.EventTimeUtc > _lastEventUtc)
                    {
                        _lastEventUtc = row.EventTimeUtc;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private static void UpsertAccount(DeviceState device, AccountEventRow row)
        {
            if (!device.AccountsByUsername.TryGetValue(row.Username, out var account))
            {
                account = new AccountState
                {
                    AccountId = row.AccountId,
                    Username = row.Username
                };
                device.AccountsByUsername[row.Username] = account;
            }

            account.LastEventUtc = MaxUtc(account.LastEventUtc, row.EventTimeUtc);
            account.Values = row.Values;
            account.PackageName = GetString(row.Values, "package_name");
        }

        private static void UpdateDerivedAccountCounts(DeviceState device)
        {
            var pkgAll = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pkgActive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var account in device.AccountsByUsername.Values)
            {
                var pkg = string.IsNullOrWhiteSpace(account.PackageName) ? account.Username : account.PackageName;
                pkgAll.Add(pkg);
                if (account.IsActive())
                {
                    pkgActive.Add(pkg);
                }
            }

            device.MaxAccounts = pkgAll.Count;
            device.ActiveAccounts = pkgActive.Count;
        }

        private static void HydrateDeviceInfo(DeviceState device, Dictionary<string, object?> values)
        {
            device.UptimeSec = GetInt64(values, "uptime_sec", device.UptimeSec);
            device.BatteryPct = GetInt32(values, "battery_pct", device.BatteryPct);
            device.BatteryTempC = GetDecimal(values, "battery_temp_c", device.BatteryTempC);
            device.RamUsedMb = GetInt32(values, "ram_used_mb", device.RamUsedMb);
            device.RamFreeMb = GetInt32(values, "ram_free_mb", device.RamFreeMb);
            device.StorageFreeMb = GetInt32(values, "storage_free_mb", device.StorageFreeMb);
            device.PingMs = GetInt32(values, "ping_ms", device.PingMs);

            var charging = GetString(values, "charging");
            if (!string.IsNullOrWhiteSpace(charging)) device.Charging = charging;

            var network = GetString(values, "network");
            if (!string.IsNullOrWhiteSpace(network)) device.Network = network;

            var deviceName = GetString(values, "device_name");
            if (!string.IsNullOrWhiteSpace(deviceName)) device.Name = deviceName;
        }

        private static DateTime MaxUtc(DateTime a, DateTime b)
            => a >= b ? a : b;

        private static string ToAgoText(DateTime utc)
        {
            if (utc == DateTime.MinValue) return "Never";
            var span = DateTime.UtcNow - utc;
            if (span.TotalSeconds < 60) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
            return $"{(int)span.TotalDays} day(s) ago";
        }

        private static string GetString(Dictionary<string, object?> values, string key)
            => values.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

        private static int GetInt32(Dictionary<string, object?> values, string key, int fallback)
        {
            if (!values.TryGetValue(key, out var v) || v is null) return fallback;
            if (v is int i) return i;
            if (v is long l) return (int)l;
            if (v is decimal d) return (int)d;
            if (int.TryParse(v.ToString(), out var parsed)) return parsed;
            return fallback;
        }

        private static long GetInt64(Dictionary<string, object?> values, string key, long fallback)
        {
            if (!values.TryGetValue(key, out var v) || v is null) return fallback;
            if (v is long l) return l;
            if (v is int i) return i;
            if (v is decimal d) return (long)d;
            if (long.TryParse(v.ToString(), out var parsed)) return parsed;
            return fallback;
        }

        private static decimal GetDecimal(Dictionary<string, object?> values, string key, decimal fallback)
        {
            if (!values.TryGetValue(key, out var v) || v is null) return fallback;
            if (v is decimal d) return d;
            if (v is double db) return (decimal)db;
            if (v is float f) return (decimal)f;
            if (decimal.TryParse(v.ToString(), out var parsed)) return parsed;
            return fallback;
        }
    }
}
