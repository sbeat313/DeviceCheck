namespace DeviceCheck.Notifier.Models;

public sealed class NotificationRequest
{
    public int Uid { get; set; }

    public string FromStatus { get; set; } = string.Empty;

    public string ToStatus { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public List<string> Recipients { get; set; } = [];
}
