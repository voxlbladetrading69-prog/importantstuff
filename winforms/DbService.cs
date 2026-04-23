using Npgsql;
using Opus;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;

public class DbService
{
    private readonly string _connString;

    public DbService(string connString)
    {
        _connString = connString;
    }
    public async Task<List<AccountEventRow>> GetEventsSinceAsync(DateTime sinceUtc)
    {
        const string sql = @"
        select
            d.id as device_id,
            d.hwid_hash,
            coalesce(d.device_name, '') as device_name,
            a.id as account_id,
            a.username,
            e.event_time,
            e.values_jsonb
        from public.account_events e
        join public.accounts a on a.id = e.account_id
        join public.devices d on d.id = a.device_id
        where e.event_time > @since
        order by e.event_time asc;";

        var rows = new List<AccountEventRow>();

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("since", sinceUtc);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var json = reader["values_jsonb"]?.ToString() ?? "{}";
            rows.Add(new AccountEventRow
            {
                DeviceId = (long)reader["device_id"],
                HwidHash = reader["hwid_hash"]?.ToString() ?? "",
                DeviceName = reader["device_name"]?.ToString() ?? "",
                AccountId = (long)reader["account_id"],
                Username = reader["username"]?.ToString() ?? "",
                EventTimeUtc = ((DateTime)reader["event_time"]).ToUniversalTime(),
                Values = ParseJsonToDictionary(json)
            });
        }

        return rows;
    }
    public async Task<List<AccountEventRow>> GetAllEventsAsync(int limit = 500)
    {
        const string sql = @"
            select
                d.id as device_id,
                d.hwid_hash,
                coalesce(d.device_name, '') as device_name,
                a.id as account_id,
                a.username,
                e.event_time,
                e.values_jsonb
            from public.account_events e
            join public.accounts a on a.id = e.account_id
            join public.devices d on d.id = a.device_id
            order by e.event_time desc
            limit @limit;";

        var rows = new List<AccountEventRow>();

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("limit", limit);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var json = reader["values_jsonb"]?.ToString() ?? "{}";

            rows.Add(new AccountEventRow
            {
                DeviceId = (long)reader["device_id"],
                HwidHash = reader["hwid_hash"]?.ToString() ?? "",
                DeviceName = reader["device_name"]?.ToString() ?? "",
                AccountId = (long)reader["account_id"],
                Username = reader["username"]?.ToString() ?? "",
                EventTimeUtc = ((DateTime)reader["event_time"]).ToUniversalTime(),
                Values = ParseJsonToDictionary(json)
            });
        }

        return rows;
    }

    public async Task<List<(DateTime t, decimal honey)>> GetHoneyOverTimeAsync(string username)
    {
        const string sql = @"
            select
                e.event_time,
                case
                  when (e.values_jsonb->>'honey') ~ '^[0-9]+(\.[0-9]+)?$'
                  then (e.values_jsonb->>'honey')::numeric
                  else 0
                end as honey
            from public.account_events e
            join public.accounts a on a.id = e.account_id
            where a.username = @username
            order by e.event_time;";

        var result = new List<(DateTime t, decimal honey)>();

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("username", username);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var t = ((DateTime)reader["event_time"]).ToUniversalTime();
            var h = (decimal)reader["honey"];
            result.Add((t, h));
        }

        return result;
    }

    private static Dictionary<string, object?> ParseJsonToDictionary(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return ReadObject(doc.RootElement);
    }

    private static Dictionary<string, object?> ReadObject(JsonElement obj)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in obj.EnumerateObject())
        {
            dict[prop.Name] = ReadValue(prop.Value);
        }
        return dict;
    }

    private static object? ReadValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Object => ReadObject(el),
        JsonValueKind.Array => ReadArray(el),
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.TryGetInt64(out var i) ? i :
                                el.TryGetDecimal(out var d) ? d :
                                el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => el.ToString()
    };

    private static List<object?> ReadArray(JsonElement arr)
    {
        var list = new List<object?>();
        foreach (var item in arr.EnumerateArray())
            list.Add(ReadValue(item));
        return list;
    }
}

public class DeviceDbLoader
{
    private readonly string _connString;

    public DeviceDbLoader(string connString)
    {
        _connString = connString;
    }

    public async Task<List<Device>> LoadDevicesForDashboardAsync()
    {
        const string sql = @"
with last_event_per_package as (
    select
        a.device_id,
        coalesce(nullif(e.values_jsonb->>'package_name', ''), d.package_name, 'unknown') as package_name,
        max(e.event_time) as last_event_time
    from public.account_events e
    join public.accounts a on a.id = e.account_id
    join public.devices d on d.id = a.device_id
    group by a.device_id, coalesce(nullif(e.values_jsonb->>'package_name', ''), d.package_name, 'unknown')
),
pkg_counts as (
    select
        lep.device_id,
        count(*)::int as unique_packages,
        count(*) filter (where lep.last_event_time >= now() - interval '10 minutes')::int as active_packages_10m,
        max(lep.last_event_time) as max_event_time
    from last_event_per_package lep
    group by lep.device_id
),
acc_last as (
    select device_id, max(last_seen) as max_account_seen
    from public.accounts
    group by device_id
)
select
    d.id as device_id,
    coalesce(d.device_name, d.hwid_hash, 'Unknown Device') as device_name,
    d.first_seen as first_seen_at,

    least(coalesce(pc.unique_packages, 0), 5) as max_accounts,
    least(coalesce(pc.active_packages_10m, 0), 5) as active_accounts,

    greatest(
        coalesce(d.last_seen, to_timestamp(0)),
        coalesce(al.max_account_seen, to_timestamp(0)),
        coalesce(pc.max_event_time, to_timestamp(0))
    ) as last_sync_at
from public.devices d
left join pkg_counts pc on pc.device_id = d.id
left join acc_last al on al.device_id = d.id
order by d.id;
";

        var devices = new List<Device>();

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var deviceId = reader["device_id"].ToString() ?? "";
            var name = reader["device_name"]?.ToString() ?? "Unknown Device";
            var active = Convert.ToInt32(reader["active_accounts"]);
            var max = Convert.ToInt32(reader["max_accounts"]);
            var lastSyncAt = ((DateTime)reader["last_sync_at"]).ToUniversalTime();
            var firstSeenAt = ((DateTime)reader["first_seen_at"]).ToUniversalTime();

            var mins = (DateTime.UtcNow - lastSyncAt).TotalMinutes;
            var isOnline = mins <= 10;

            var d = new Device(
                name: name,
                activeAccounts: active,
                maxAccounts: max,
                lastSyncText: ToAgoText(lastSyncAt),
                statusColor: isOnline ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128),
                deviceId: deviceId
            );

            d.LastSyncUtc = lastSyncAt.ToUniversalTime();
            d.FirstSeenUtc = firstSeenAt.ToUniversalTime();

            d.SetDeviceName(name);
            d.SetAccounts(active, max);
            d.SetLastSync(ToAgoText(lastSyncAt));
            d.SetStatusColor(isOnline ? Color.FromArgb(128, 255, 128) : Color.FromArgb(255, 128, 128));

            devices.Add(d);
        }

        return devices;
    }

    private static string ToAgoText(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalSeconds < 60) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
        return $"{(int)span.TotalDays} day(s) ago";
    }
}
