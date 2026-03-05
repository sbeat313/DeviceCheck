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
    /// 外部設備檢查 API 的基底網址，例如 https://host/api/device。
    /// 實際呼叫時會在後面附上 /{uid}。
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
    public int RequestTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// 需要列管的設備 UID 清單。
    /// </summary>
    public List<int> Uids { get; set; } = [];
}
