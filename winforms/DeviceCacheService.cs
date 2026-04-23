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
        private bool _initialized;

        public DeviceCacheService(string connString)
        {
            _db = new DbService(connString);
        }

        public async Task InitializeAsync(int initialLimit = 5000)
        {
            var rows = await _db.GetAllEventsAsync(initialLimit);
            rows.Reverse(); // oldest -> newest so each newer row overwrites previous state cleanly
            ApplyRows(rows);
            _initialized = true;
        }

        public async Task<int> RefreshAsync()
        {
            if (!_initialized) return 0;

            // subtract a tiny epsilon to avoid missing rows that share the same timestamp
            // as the latest already-processed event.
            var since = _lastEventUtc == DateTime.MinValue
                ? DateTime.MinValue
                : _lastEventUtc.AddMilliseconds(-1);

            var rows = await _db.GetEventsSinceAsync(since);
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
        public DeviceState? GetDeviceSnapshot(string deviceId)
        {
            _lock.EnterReadLock();
            try
            {
                if (!_devicesById.TryGetValue(deviceId, out var source)) return null;

                var clone = new DeviceState
                {
                    DeviceId = source.DeviceId,
                    HwidHash = source.HwidHash,
                    Name = source.Name,
                    LastSeenUtc = source.LastSeenUtc,
                    UptimeSec = source.UptimeSec,
                    BatteryPct = source.BatteryPct,
                    BatteryTempC = source.BatteryTempC,
                    RamUsedMb = source.RamUsedMb,
                    RamFreeMb = source.RamFreeMb,
                    StorageFreeMb = source.StorageFreeMb,
                    PingMs = source.PingMs,
                    Charging = source.Charging,
                    Network = source.Network,
                    ActiveAccounts = source.ActiveAccounts,
                    MaxAccounts = source.MaxAccounts
                };

                foreach (var pair in source.AccountsByUsername)
                {
                    clone.AccountsByUsername[pair.Key] = CloneAccount(pair.Value);
                }
                return clone;
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
                    else if (!string.IsNullOrWhiteSpace(row.DeviceName))
                    {
                        device.Name = row.DeviceName;
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
            AppendMetricPoint(account, row);
        }

        private static void AppendMetricPoint(AccountState account, AccountEventRow row)
        {
            var honey = GetDecimalOrZero(row.Values, "honey");
            var hive = GetDecimalOrZero(row.Values, "hive_size", "hive", "hiveSize");

            if (account.MetricTimeline.Count > 0 && row.EventTimeUtc <= account.MetricTimeline[^1].EventTimeUtc)
            {
                var existing = account.MetricTimeline.FindIndex(p => p.EventTimeUtc == row.EventTimeUtc);
                if (existing >= 0)
                {
                    account.MetricTimeline[existing].Honey = honey;
                    account.MetricTimeline[existing].HiveSize = hive;
                }
                return;
            }

            account.MetricTimeline.Add(new AccountMetricPoint
            {
                EventTimeUtc = row.EventTimeUtc,
                Honey = honey,
                HiveSize = hive
            });

            const int maxTimelinePoints = 600;
            if (account.MetricTimeline.Count > maxTimelinePoints)
            {
                account.MetricTimeline.RemoveAt(0);
            }
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
            var source = TryGetObject(values, "device_info") ?? values;

            device.UptimeSec = GetInt64(source, device.UptimeSec, "uptime_sec", "uptime", "uptime_seconds");
            device.BatteryPct = GetInt32(source, device.BatteryPct, "battery_pct", "battery", "battery_level");
            device.BatteryTempC = GetDecimal(source, device.BatteryTempC, "battery_temp_c", "battery_temp", "battery_temperature_c");
            device.RamUsedMb = GetInt32(source, device.RamUsedMb, "ram_used_mb", "ram_used", "memory_used_mb");
            device.RamFreeMb = GetInt32(source, device.RamFreeMb, "ram_free_mb", "ram_free", "memory_free_mb");
            device.StorageFreeMb = GetInt32(source, device.StorageFreeMb, "storage_free_mb", "storage_free", "disk_free_mb");
            device.PingMs = GetInt32(source, device.PingMs, "ping_ms", "ping");

            var charging = GetString(source, "charging", "is_charging");
            if (!string.IsNullOrWhiteSpace(charging)) device.Charging = charging;

            var network = GetString(source, "network", "network_type");
            if (!string.IsNullOrWhiteSpace(network)) device.Network = network;

            var deviceName = GetString(source, "device_name");
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

        private static Dictionary<string, object?>? TryGetObject(Dictionary<string, object?> values, string key)
        {
            if (!values.TryGetValue(key, out var nested) || nested is null) return null;
            return nested as Dictionary<string, object?>;
        }

        private static string GetString(Dictionary<string, object?> values, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!values.TryGetValue(key, out var v) || v is null) continue;
                var text = v.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            return "";
        }

        private static int GetInt32(Dictionary<string, object?> values, int fallback, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!values.TryGetValue(key, out var v) || v is null) continue;
                if (v is int i) return i;
                if (v is long l) return (int)l;
                if (v is decimal d) return (int)d;
                if (int.TryParse(v.ToString(), out var parsed)) return parsed;
            }
            return fallback;
        }

        private static long GetInt64(Dictionary<string, object?> values, long fallback, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!values.TryGetValue(key, out var v) || v is null) continue;
                if (v is long l) return l;
                if (v is int i) return i;
                if (v is decimal d) return (long)d;
                if (long.TryParse(v.ToString(), out var parsed)) return parsed;
            }
            return fallback;
        }

        private static decimal GetDecimal(Dictionary<string, object?> values, decimal fallback, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!values.TryGetValue(key, out var v) || v is null) continue;
                if (v is decimal d) return d;
                if (v is double db) return (decimal)db;
                if (v is float f) return (decimal)f;
                if (decimal.TryParse(v.ToString(), out var parsed)) return parsed;
            }
            return fallback;
        }
        private static decimal GetDecimalOrZero(Dictionary<string, object?> values, params string[] keys)
        {
            foreach (var key in keys)
            {
                var val = GetDecimal(values, decimal.MinValue, key);
                if (val != decimal.MinValue) return val;
            }

            return 0m;
        }

        private static AccountState CloneAccount(AccountState source)
            => new()
            {
                AccountId = source.AccountId,
                Username = source.Username,
                PackageName = source.PackageName,
                LastEventUtc = source.LastEventUtc,
                Values = new Dictionary<string, object?>(source.Values, StringComparer.OrdinalIgnoreCase),
                MetricTimeline = source.MetricTimeline
                    .Select(p => new AccountMetricPoint
                    {
                        EventTimeUtc = p.EventTimeUtc,
                        Honey = p.Honey,
                        HiveSize = p.HiveSize
                    })
                    .ToList()
            };
    }
}
