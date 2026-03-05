namespace DeviceCheck.Options;

/// <summary>
/// 通知設定。
/// </summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notification";

    /// <summary>
    /// 是否啟用通知。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 通知接收服務 API URL。
    /// </summary>
    public string EndpointUrl { get; set; } = "http://localhost:5058/api/notifications";

    /// <summary>
    /// 通知對象（收件人）。
    /// </summary>
    public List<string> Recipients { get; set; } = [];
}
