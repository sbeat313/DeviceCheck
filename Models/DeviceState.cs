namespace DeviceCheck.Models;

public sealed class DeviceState
{
    public int Uid { get; init; }

    public DeviceHealthStatus Status { get; set; } = DeviceHealthStatus.Unknown;

    public DateTimeOffset LastSeenUtc { get; set; }

    public DateTimeOffset LastCheckedUtc { get; set; }

    public DateTimeOffset NextCheckUtc { get; set; }

    public string LastResult { get; set; } = "not checked";
}
