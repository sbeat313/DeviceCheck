using DeviceCheck.Models;
using DeviceCheck.Options;
using Microsoft.Extensions.Options;

namespace DeviceCheck.Services;

public sealed class DeviceMonitorService : BackgroundService
{
    private readonly DeviceRegistry _registry;
    private readonly DeviceProbeClient _probeClient;
    private readonly ILogger<DeviceMonitorService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _busyRetryDelay;

    public DeviceMonitorService(
        DeviceRegistry registry,
        DeviceProbeClient probeClient,
        IOptions<DeviceCheckOptions> options,
        ILogger<DeviceMonitorService> logger)
    {
        _registry = registry;
        _probeClient = probeClient;
        _logger = logger;

        _checkInterval = TimeSpan.FromSeconds(options.Value.CheckIntervalSeconds);
        _busyRetryDelay = TimeSpan.FromSeconds(options.Value.BusyRetryDelaySeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var dueDevices = _registry.DueForCheck(DateTimeOffset.UtcNow);
            foreach (var device in dueDevices)
            {
                var (status, result) = await _probeClient.ProbeAsync(device.Uid, stoppingToken);
                var delay = status == DeviceHealthStatus.Busy ? _busyRetryDelay : _checkInterval;
                _registry.UpdateAfterProbe(device, status, result, delay);

                _logger.LogInformation(
                    "UID {Uid} check result: {Status} ({Result}), next check in {Delay}s",
                    device.Uid,
                    status,
                    result,
                    delay.TotalSeconds);
            }
        }
    }
}
