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
    /// - body 含 busy => Busy
    /// - 其他/錯誤 => Dead
    /// </summary>
    public async Task<(DeviceHealthStatus status, string result)> ProbeAsync(int uid, CancellationToken cancellationToken)
    {
        try
        {
            var url = BuildUrl(uid);
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return (DeviceHealthStatus.Alive, "200 OK");
            }

            if (body.Contains("busy", StringComparison.OrdinalIgnoreCase))
            {
                return (DeviceHealthStatus.Busy, $"busy ({response.StatusCode})");
            }

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
    /// 組合最終探測 URL：{BaseUrl}/{uid}。
    /// </summary>
    private string BuildUrl(int uid)
    {
        return _options.BaseUrl.EndsWith('/')
            ? $"{_options.BaseUrl}{uid}"
            : $"{_options.BaseUrl}/{uid}";
    }
}
