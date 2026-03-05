using DeviceCheck.Models;
using DeviceCheck.Options;
using Microsoft.Extensions.Options;

namespace DeviceCheck.Services;

public sealed class DeviceProbeClient
{
    private readonly HttpClient _httpClient;
    private readonly DeviceCheckOptions _options;

    public DeviceProbeClient(HttpClient httpClient, IOptions<DeviceCheckOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
    }

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
            return (DeviceHealthStatus.Dead, "timeout");
        }
        catch (Exception ex)
        {
            return (DeviceHealthStatus.Dead, $"error: {ex.Message}");
        }
    }

    private string BuildUrl(int uid)
    {
        return _options.BaseUrl.EndsWith('/')
            ? $"{_options.BaseUrl}{uid}"
            : $"{_options.BaseUrl}/{uid}";
    }
}
