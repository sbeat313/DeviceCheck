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
    /// 呼叫外部 API 的逾時秒數。
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// 探測回傳 Dead 時的額外重試次數（Busy 不計入此次數）。
    /// 例如設定 2，代表 Dead 情境下單輪最多會呼叫 3 次（首次 + 2 次重試）。
    /// </summary>
    public int ProbeRetryCount { get; set; } = 2;

    /// <summary>
    /// Dead 重試與 Busy 延後重測的等待間隔（秒）。
    /// 同時用於同輪次 Dead 重試等待，以及最終結果為 Busy 時的下次檢查延遲。
    /// </summary>
    public int ProbeRetryDelaySeconds { get; set; } = 1;

    /// <summary>
    /// 連續幾次探測為 dead 才正式判定為 Dead。
    /// 在達到門檻前會先標記為 Unknown。
    /// </summary>
    public int DeadConsecutiveThreshold { get; set; } = 3;

    /// <summary>
    /// 需要列管的設備 UID 清單。
    /// </summary>
    public List<int> Uids { get; set; } = [];

    /// <summary>
    /// 設備別名設定，Key 為 UID，Value 為中文別名。
    /// </summary>
    public Dictionary<int, string> UidAliases { get; set; } = [];
}
