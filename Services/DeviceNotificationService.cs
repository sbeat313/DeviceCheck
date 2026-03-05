using DeviceCheck.Models;
using DeviceCheck.Options;
using Microsoft.Extensions.Options;

namespace DeviceCheck.Services;

/// <summary>
/// 設備狀態轉換通知服務。
/// </summary>
public sealed class DeviceNotificationService(IOptions<DeviceCheckOptions> options, ILogger<DeviceNotificationService> logger)
{
    private readonly IReadOnlyList<string> _recipients = options.Value.NotificationRecipients
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    /// <summary>
    /// 發送狀態轉換通知。
    /// </summary>
    public Task NotifyTransitionAsync(DeviceStatusTransition transition, CancellationToken cancellationToken)
    {
        if (_recipients.Count == 0)
        {
            return Task.CompletedTask;
        }

        string category = transition.To == DeviceHealthStatus.Alive ? "異常→正常" : "正常→異常";

        foreach (string recipient in _recipients)
        {
            logger.LogWarning(
                "[Notify] To={Recipient}, UID={Uid}, Category={Category}, From={From}, To={To}, Trigger={Trigger}, Result={Result}, AtUtc={AtUtc:O}",
                recipient,
                transition.Uid,
                category,
                transition.From,
                transition.To,
                transition.Trigger,
                transition.Result,
                transition.OccurredUtc);
        }

        return Task.CompletedTask;
    }
}
