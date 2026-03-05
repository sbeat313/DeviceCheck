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

        if (_options.Recipients.Count == 0)
        {
            logger.LogWarning("Notification 已啟用但 Recipients 為空，略過 UID {Uid} 的通知", transition.Uid);
            return;
        }

        NotificationRequest request = new()
        {
            Uid = transition.Uid,
            FromStatus = transition.FromStatus.ToString(),
            ToStatus = transition.ToStatus.ToString(),
            OccurredAtUtc = transition.OccurredAtUtc,
            Message = $"設備 {transition.Uid} 狀態由 {transition.FromStatus} 變更為 {transition.ToStatus}，探測結果：{transition.Result}",
            Recipients = _options.Recipients
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
}
