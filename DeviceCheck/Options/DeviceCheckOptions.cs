namespace DeviceCheck.Options;

/// <summary>
/// DeviceCheck 設定模型，對應 appsettings.json 的 DeviceCheck 區段。
/// </summary>
public sealed class DeviceCheckOptions
{
    /// <summary>
    /// 設定區段名稱。
    /// </summary>
    public const string SectionName = "DeviceCheck";

    /// <summary>
    /// 外部設備服務主機網址，例如 http://192.168.0.58:5099。
    /// 實際呼叫時會組成 /Ctrl/{uid}/RadioCheck。
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 一般檢查週期（秒）。
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// 回傳 busy 時的重試延遲（秒）。
    /// </summary>
    public int BusyRetryDelaySeconds { get; set; } = 3;

    /// <summary>
    /// 呼叫外部 API 的逾時秒數。
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// 連續幾次探測為 dead 才正式判定為 Dead。
    /// 在達到門檻前會先標記為 Unknown。
    /// </summary>
    public int DeadConsecutiveThreshold { get; set; } = 3;

    /// <summary>
    /// 需要列管的設備 UID 清單。
    /// </summary>
    public List<int> Uids { get; set; } = [];
}
