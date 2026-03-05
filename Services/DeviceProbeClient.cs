using DeviceCheck.Models;
using DeviceCheck.Options;
using Microsoft.Extensions.Options;

namespace DeviceCheck.Services;

/// <summary>
/// 對外部設備 API 進行探測，並轉換為內部健康狀態。
/// </summary>
public sealed class DeviceProbeClient
{
    private readonly HttpClient _httpClient;
    private readonly DeviceCheckOptions _options;

    public DeviceProbeClient(HttpClient httpClient, IOptions<DeviceCheckOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        // 每次探測請求逾時時間。
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
    }

    /// <summary>
    /// 探測指定 UID：
    /// - 200 => Alive
    /// - 486 => Busy
    /// - 503 與其他 => Dead
    /// </summary>
    public async Task<(DeviceHealthStatus status, string result)> ProbeAsync(int uid, CancellationToken cancellationToken)
    {
        try
        {
            string url = BuildUrl(uid);
            using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (DeviceHealthStatus.Alive, "200 OK");
            }

            if ((int)response.StatusCode == 486)
            {
                return (DeviceHealthStatus.Busy, "486 BusyHere");
            }

            // 依需求：503 視為 Dead
            return (DeviceHealthStatus.Dead, $"{(int)response.StatusCode} {response.StatusCode}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient 因 timeout 取消（非服務關閉）。
            return (DeviceHealthStatus.Dead, "timeout");
        }
        catch (Exception ex)
        {
            return (DeviceHealthStatus.Dead, $"error: {ex.Message}");
        }
    }

    /// <summary>
    /// 組合最終探測 URL：{BaseUrl}/Ctrl/{uid}/RadioCheck。
    /// </summary>
    private string BuildUrl(int uid)
    {
        string baseUrl = _options.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/Ctrl/{uid}/RadioCheck";
    }
}
