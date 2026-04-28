using Npgsql;
using Opus;
using System;
using System.Collections.Generic;
using System.Data;
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
    (
      coalesce(e.values_jsonb, '{}'::jsonb)
      ||
      jsonb_build_object(
        'device_info', jsonb_build_object(
          'device_name', d.device_name,
          'hwid_hash', d.hwid_hash,
          'event_time', ds.event_time,
          'uptime_sec', ds.uptime_sec,
          'battery_pct', ds.battery_pct,
          'battery_temp_c', ds.battery_temp_c,
          'ram_used_mb', ds.ram_used_mb,
          'ram_free_mb', ds.ram_free_mb,
          'storage_free_mb', ds.storage_free_mb,
          'ping_ms', ds.ping_ms,
          'charging', ds.charging,
          'network', ds.network
        )
      )
    ) as values_jsonb
from public.account_events e
join public.accounts a on a.id = e.account_id
join public.devices d on d.id = a.device_id
left join lateral (
    select s.*
    from public.device_snapshots s
    where s.device_id = d.id
      and s.event_time <= e.event_time
    order by s.event_time desc, s.id desc
    limit 1
) ds on true
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
    (
      coalesce(e.values_jsonb, '{}'::jsonb)
      ||
      jsonb_build_object(
        'device_info', jsonb_build_object(
          'device_name', d.device_name,
          'hwid_hash', d.hwid_hash,
          'event_time', ds.event_time,
          'uptime_sec', ds.uptime_sec,
          'battery_pct', ds.battery_pct,
          'battery_temp_c', ds.battery_temp_c,
          'ram_used_mb', ds.ram_used_mb,
          'ram_free_mb', ds.ram_free_mb,
          'storage_free_mb', ds.storage_free_mb,
          'ping_ms', ds.ping_ms,
          'charging', ds.charging,
          'network', ds.network
        )
      )
    ) as values_jsonb
from public.account_events e
join public.accounts a on a.id = e.account_id
join public.devices d on d.id = a.device_id
left join lateral (
    select s.*
    from public.device_snapshots s
    where s.device_id = d.id
      and s.event_time <= e.event_time
    order by s.event_time desc, s.id desc
    limit 1
) ds on true
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
    public async Task EnsureAccessTokensTableAsync()
    {
        const string sql = @"
create table if not exists public.access_tokens
(
    id bigint generated by default as identity primary key,
    token text not null unique,
    username text not null,
    expiration_date timestamptz null,
    can_view_feedback boolean not null default false,
    created_at timestamptz not null default now()
);
create index if not exists ix_access_tokens_token
    on public.access_tokens(token);";

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SeedAccessTokensAsync(IEnumerable<AccessToken> tokens)
    {
        const string sql = @"
insert into public.access_tokens (token, username, expiration_date)
values (@token, @username, @expiration_date)
on conflict (token) do update
set username = excluded.username,
    expiration_date = excluded.expiration_date;";

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        foreach (var token in tokens)
        {
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("token", token.Token);
            cmd.Parameters.AddWithValue("username", token.Username);
            cmd.Parameters.AddWithValue("expiration_date", (object?)token.ExpirationDateUtc ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
    }
    public async Task<AccessToken?> GetValidAccessTokenAsync(string token)
    {
        const string sql = @"
select token, username, expiration_date, can_view_feedback
from public.access_tokens
where token = @token
  and (expiration_date is null or expiration_date > now())
limit 1;";

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("token", token);

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);

        if (!await reader.ReadAsync())
            return null;

        DateTime? expiration = null;

        if (reader["expiration_date"] != DBNull.Value)
            expiration = ((DateTime)reader["expiration_date"]).ToUniversalTime();

        return new AccessToken
        {
            Token = reader["token"]?.ToString() ?? "",
            Username = reader["username"]?.ToString() ?? "",
            ExpirationDateUtc = expiration,
            CanViewFeedback = reader["can_view_feedback"] != DBNull.Value && (bool)reader["can_view_feedback"]
        };
    }

    public async Task EnsureAccessTokenPermissionsAsync()
    {
        const string sql = @"
alter table public.access_tokens
    add column if not exists can_view_feedback boolean not null default false;

update public.access_tokens
set can_view_feedback = true
where expiration_date is null;";

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EnsureFeedbackTableAsync()
    {
        const string sql = @"
create table if not exists public.feedback_entries
(
    id bigint generated by default as identity primary key,
    username text not null,
    content text not null,
    created_at timestamptz not null default now(),
    is_favorite boolean not null default false
);
alter table public.feedback_entries
    add column if not exists is_favorite boolean not null default false;

create index if not exists ix_feedback_entries_created_at
    on public.feedback_entries(created_at desc);";

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SubmitFeedbackAsync(string username, string content)
    {
        const string sql = @"
insert into public.feedback_entries (username, content)
values (@username, @content);";

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("username", username.Trim());
        cmd.Parameters.AddWithValue("content", content.Trim());
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<FeedbackEntry>> GetFeedbackEntriesAsync(int limit = 200)
    {
        const string sql = @"
select id, username, content, created_at, is_favorite
from public.feedback_entries
order by is_favorite desc, created_at desc
limit @limit;";

        var rows = new List<FeedbackEntry>();

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            rows.Add(new FeedbackEntry
            {
                Id = (long)reader["id"],
                Username = reader["username"]?.ToString() ?? "",
                Content = reader["content"]?.ToString() ?? "",
                CreatedAtUtc = ((DateTime)reader["created_at"]).ToUniversalTime(),
                IsFavorite = reader["is_favorite"] != DBNull.Value && (bool)reader["is_favorite"]
            });
        }

        return rows;
    }

    public async Task<List<FeedbackEntry>> GetFeedbackEntriesByUsernameAsync(string username, int limit = 100)
    {
        const string sql = @"
select id, username, content, created_at, is_favorite
from public.feedback_entries
where username = @username
order by is_favorite desc, created_at desc
limit @limit;";

        var rows = new List<FeedbackEntry>();

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("username", username);
        cmd.Parameters.AddWithValue("limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            rows.Add(new FeedbackEntry
            {
                Id = (long)reader["id"],
                Username = reader["username"]?.ToString() ?? "",
                Content = reader["content"]?.ToString() ?? "",
                CreatedAtUtc = ((DateTime)reader["created_at"]).ToUniversalTime(),
                IsFavorite = reader["is_favorite"] != DBNull.Value && (bool)reader["is_favorite"]
            });
        }

        return rows;
    }

    public async Task SetFeedbackFavoriteAsync(long feedbackId, bool isFavorite)
    {
        const string sql = @"
update public.feedback_entries
set is_favorite = @is_favorite
where id = @id;";

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", feedbackId);
        cmd.Parameters.AddWithValue("is_favorite", isFavorite);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteFeedbackAsync(long feedbackId)
    {
        const string sql = @"
delete from public.feedback_entries
where id = @id;";

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", feedbackId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EnsureAddedDevicesTableAsync()
    {
        const string sql = @"
create table if not exists public.added_devices
(
    hwid_hash text primary key,
    created_at timestamptz not null default now()
);";

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<string>> GetAddedDeviceHwidsAsync()
    {
        const string sql = @"
select hwid_hash
from public.added_devices;";

        var result = new List<string>();

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var hwid = reader["hwid_hash"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(hwid))
            {
                result.Add(hwid.Trim());
            }
        }

        return result;
    }

    public async Task AddAddedDeviceAsync(string hwidHash)
    {
        const string sql = @"
insert into public.added_devices (hwid_hash)
values (@hwid_hash)
on conflict (hwid_hash) do nothing;";

        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("hwid_hash", hwidHash.Trim());
        await cmd.ExecuteNonQueryAsync();
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
