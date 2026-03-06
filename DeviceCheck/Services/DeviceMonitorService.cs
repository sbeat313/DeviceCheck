using DeviceCheck.Models;
using DeviceCheck.Options;
using Microsoft.Extensions.Options;

namespace DeviceCheck.Services;

/// <summary>
/// 背景監控服務：定期找出到期設備並執行探測。
/// </summary>
public sealed class DeviceMonitorService(DeviceRegistry registry, DeviceProbeClient probeClient, NotificationClient notificationClient, IOptions<DeviceCheckOptions> options, ILogger<DeviceMonitorService> logger) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(options.Value.CheckIntervalSeconds);
    private readonly TimeSpan _busyRetryDelay = TimeSpan.FromSeconds(options.Value.BusyRetryDelaySeconds);
    private readonly int _deadConsecutiveThreshold = Math.Max(1, options.Value.DeadConsecutiveThreshold);

    /// <summary>
    /// 主循環：每秒檢查一次是否有到期設備。
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            IReadOnlyList<DeviceState> dueDevices = registry.DueForCheck(DateTimeOffset.UtcNow);
            foreach (DeviceState device in dueDevices)
            {
                (DeviceHealthStatus status, string result) = await probeClient.ProbeAsync(device.Uid, stoppingToken);

                // busy 使用短延遲重試，其餘使用一般週期。
                TimeSpan delay = status == DeviceHealthStatus.Busy ? _busyRetryDelay : _checkInterval;
                DeviceStatusTransition? transition = registry.UpdateAfterProbe(device, status, result, delay, _deadConsecutiveThreshold);

                if (transition is not null)
                {
                    await notificationClient.SendStatusTransitionAsync(transition, stoppingToken);
                }

                logger.LogInformation(
                    "UID {Uid} check result: {Status} ({Result}), next check in {Delay}s",
                    device.Uid,
                    status,
                    result,
                    delay.TotalSeconds);
            }
        }
    }
}
