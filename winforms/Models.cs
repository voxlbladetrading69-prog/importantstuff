using System;
using System.Collections.Generic;

public class AccountEventRow
{
    public long DeviceId { get; set; }
    public string HwidHash { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public long AccountId { get; set; }
    public string Username { get; set; } = "";
    public DateTime EventTimeUtc { get; set; }
    public Dictionary<string, object?> Values { get; set; } = new();
}
public class AccessToken
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public DateTime? ExpirationDateUtc { get; set; }
    public bool CanViewFeedback { get; set; }
    public bool HasElevatedFeedbackAccess => ExpirationDateUtc == null || CanViewFeedback;
}

public class FeedbackEntry
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
