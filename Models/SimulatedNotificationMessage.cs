namespace DeviceCheck.Models;

/// <summary>
/// 模擬通知接收端收到的訊息內容。
/// </summary>
public sealed class SimulatedNotificationMessage
{
    /// <summary>流水號。</summary>
    public required long Id { get; init; }

    /// <summary>通知收件者。</summary>
    public required string Recipient { get; init; }

    /// <summary>設備 UID。</summary>
    public required int Uid { get; init; }

    /// <summary>轉換前狀態。</summary>
    public required DeviceHealthStatus From { get; init; }

    /// <summary>轉換後狀態。</summary>
    public required DeviceHealthStatus To { get; init; }

    /// <summary>分類（正常→異常 / 異常→正常）。</summary>
    public required string Category { get; init; }

    /// <summary>來源（probe / heartbeat / api）。</summary>
    public required string Trigger { get; init; }

    /// <summary>結果描述。</summary>
    public required string Result { get; init; }

    /// <summary>接收時間（UTC）。</summary>
    public required DateTimeOffset ReceivedUtc { get; init; }
}
