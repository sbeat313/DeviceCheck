using DeviceCheck.Models;
using DeviceCheck.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace DeviceCheck.Services;

/// <summary>
/// 設備狀態轉換通知服務（以 HTTP POST 發送到外部接收端）。
/// </summary>
public sealed class DeviceNotificationService(HttpClient httpClient, IOptions<DeviceCheckOptions> options, ILogger<DeviceNotificationService> logger)
{
    private readonly IReadOnlyList<string> _endpoints = options.Value.NotificationEndpoints
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public async Task NotifyTransitionAsync(DeviceStatusTransition transition, CancellationToken cancellationToken)
    {
        if (_endpoints.Count == 0)
        {
            return;
        }

        string category = transition.To == DeviceHealthStatus.Alive ? "異常→正常" : "正常→異常";

        foreach (string endpoint in _endpoints)
        {
            try
            {
                using HttpResponseMessage response = await httpClient.PostAsJsonAsync(endpoint, transition, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation(
                        "[Notify] Endpoint={Endpoint}, UID={Uid}, Category={Category}, From={From}, To={To}, Trigger={Trigger}, Result={Result}, AtUtc={AtUtc:O}",
                        endpoint,
                        transition.Uid,
                        category,
                        transition.From,
                        transition.To,
                        transition.Trigger,
                        transition.Result,
                        transition.OccurredUtc);
                    continue;
                }

                logger.LogWarning(
                    "[NotifyFail] Endpoint={Endpoint}, StatusCode={StatusCode}, UID={Uid}, Category={Category}",
                    endpoint,
                    (int)response.StatusCode,
                    transition.Uid,
                    category);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[NotifyError] Endpoint={Endpoint}, UID={Uid}", endpoint, transition.Uid);
            }
        }
    }
}
