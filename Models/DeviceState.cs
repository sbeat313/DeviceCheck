namespace DeviceCheck.Models;

/// <summary>
/// 單一設備的即時狀態。
/// </summary>
public sealed class DeviceState
{
    /// <summary>設備 UID。</summary>
    public int Uid { get; init; }

    /// <summary>目前健康狀態。</summary>
    public DeviceHealthStatus Status { get; set; } = DeviceHealthStatus.Unknown;

    /// <summary>最後一次確認存活時間（UTC）。</summary>
    public DateTimeOffset LastSeenUtc { get; set; }

    /// <summary>最後一次主動檢查時間（UTC）。</summary>
    public DateTimeOffset LastCheckedUtc { get; set; }

    /// <summary>下一次預計檢查時間（UTC）。</summary>
    public DateTimeOffset NextCheckUtc { get; set; }

    /// <summary>最後一次檢查結果訊息。</summary>
    public string LastResult { get; set; } = "not checked";
}
