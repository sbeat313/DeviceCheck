namespace DeviceCheck.Models;

/// <summary>
/// 設備狀態切換事件（正常/異常）
/// </summary>
public sealed class DeviceStatusTransition
{
    public int Uid { get; init; }

    public string Alias { get; init; } = string.Empty;

    public DeviceHealthStatus FromStatus { get; init; }

    public DeviceHealthStatus ToStatus { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }

    public string Result { get; init; } = string.Empty;
}
