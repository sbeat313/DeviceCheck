using System.Collections.Concurrent;
using DeviceCheck.Models;
using DeviceCheck.Options;
using Microsoft.Extensions.Options;

namespace DeviceCheck.Services;

public sealed class DeviceRegistry
{
    private readonly ConcurrentDictionary<int, DeviceState> _devices = new();
    private readonly TimeSpan _checkInterval;

    public DeviceRegistry(IOptions<DeviceCheckOptions> options)
    {
        var now = DateTimeOffset.UtcNow;
        _checkInterval = TimeSpan.FromSeconds(options.Value.CheckIntervalSeconds);

        foreach (var uid in options.Value.Uids.Distinct())
        {
            _devices.TryAdd(uid, new DeviceState
            {
                Uid = uid,
                LastSeenUtc = now,
                NextCheckUtc = now
            });
        }
    }

    public IReadOnlyCollection<DeviceState> GetAll() => _devices.Values.OrderBy(x => x.Uid).ToArray();

    public DeviceState? Get(int uid) => _devices.TryGetValue(uid, out var state) ? state : null;

    public bool Touch(int uid)
    {
        if (!_devices.TryGetValue(uid, out var state))
        {
            return false;
        }

        lock (state)
        {
            var now = DateTimeOffset.UtcNow;
            state.LastSeenUtc = now;
            state.NextCheckUtc = now.Add(_checkInterval);
            state.Status = DeviceHealthStatus.Alive;
            state.LastResult = "heartbeat";
        }

        return true;
    }

    public IReadOnlyList<DeviceState> DueForCheck(DateTimeOffset now)
    {
        var due = new List<DeviceState>();

        foreach (var state in _devices.Values)
        {
            lock (state)
            {
                if (state.NextCheckUtc <= now)
                {
                    state.NextCheckUtc = DateTimeOffset.MaxValue;
                    due.Add(state);
                }
            }
        }

        return due;
    }

    public void UpdateAfterProbe(DeviceState state, DeviceHealthStatus status, string result, TimeSpan nextDelay)
    {
        var now = DateTimeOffset.UtcNow;

        lock (state)
        {
            state.Status = status;
            state.LastCheckedUtc = now;
            state.LastResult = result;
            if (status == DeviceHealthStatus.Alive)
            {
                state.LastSeenUtc = now;
            }
            state.NextCheckUtc = now.Add(nextDelay);
        }
    }
}
