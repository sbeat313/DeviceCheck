using DeviceCheck.Models;
using DeviceCheck.Options;
using Microsoft.Extensions.Options;

namespace DeviceCheck.Services;

/// <summary>
/// 狀態變化通知客戶端。
/// </summary>
public sealed class NotificationClient(HttpClient httpClient, IOptions<NotificationOptions> notificationOptions, ILogger<NotificationClient> logger)
{
    private readonly NotificationOptions _options = notificationOptions.Value;

    public async Task SendStatusTransitionAsync(DeviceStatusTransition transition, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        DeviceHealthStatus fromStatusForNotification = NormalizeBoundaryStatus(transition.FromStatus);
        DeviceHealthStatus toStatusForNotification = NormalizeBoundaryStatus(transition.ToStatus);

        NotificationRequest request = new()
        {
            Uid = transition.Uid,
            Alias = transition.Alias,
            FromStatus = fromStatusForNotification.ToString(),
            ToStatus = toStatusForNotification.ToString(),
            OccurredAtUtc = transition.OccurredAtUtc,
            Message = $"設備 {transition.Uid}（{transition.Alias}）狀態由 {fromStatusForNotification} 變更為 {toStatusForNotification}，探測結果：{transition.Result}"
        };

        try
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(_options.EndpointUrl, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("通知發送失敗，UID {Uid}，HTTP {StatusCode}", transition.Uid, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "通知發送例外，UID {Uid}", transition.Uid);
        }
    }

    private static DeviceHealthStatus NormalizeBoundaryStatus(DeviceHealthStatus status)
    {
        // 通知語意僅聚焦在「是否 Dead」邊界，非 Dead 一律以 Alive 呈現。
        return status == DeviceHealthStatus.Dead ? DeviceHealthStatus.Dead : DeviceHealthStatus.Alive;
    }
}
