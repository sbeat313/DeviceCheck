namespace DeviceCheck.Models;

/// <summary>
/// 設備狀態跨越「正常/異常」邊界時的轉換事件。
/// </summary>
public sealed class DeviceStatusTransition
{
    /// <summary>設備 UID。</summary>
    public required int Uid { get; init; }

    /// <summary>轉換前狀態。</summary>
    public required DeviceHealthStatus From { get; init; }

    /// <summary>轉換後狀態。</summary>
    public required DeviceHealthStatus To { get; init; }

    /// <summary>轉換觸發來源（probe 或 heartbeat）。</summary>
    public required string Trigger { get; init; }

    /// <summary>對應的結果文字。</summary>
    public required string Result { get; init; }

    /// <summary>轉換發生時間（UTC）。</summary>
    public required DateTimeOffset OccurredUtc { get; init; }
}
