namespace DeviceCheck.Options;

/// <summary>
/// DeviceCheck 設定模型，對應 appsettings.json 的 DeviceCheck 區段。
/// </summary>
public sealed class DeviceCheckOptions
{
    public const string SectionName = "DeviceCheck";

    /// <summary>外部設備服務主機網址。</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>一般檢查週期（秒）。</summary>
    public int CheckIntervalSeconds { get; set; } = 10;

    /// <summary>回傳 busy 時的重試延遲（秒）。</summary>
    public int BusyRetryDelaySeconds { get; set; } = 3;

    /// <summary>呼叫外部 API 的逾時秒數。</summary>
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// 連續判定次數。
    /// 未達次數前狀態維持 Unknown。
    /// </summary>
    public int DecisionThresholdCount { get; set; } = 3;

    /// <summary>需要列管的設備 UID 清單。</summary>
    public List<int> Uids { get; set; } = [];

    /// <summary>
    /// 通知接收端 Endpoint URL 清單。
    /// 每次最終狀態跨越正常/異常邊界時，會 POST 轉換事件到每個 URL。
    /// </summary>
    public List<string> NotificationEndpoints { get; set; } = [];
}
