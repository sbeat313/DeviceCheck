using DeviceCheck.Models;
using DeviceCheck.Options;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace DeviceCheck.Services;

/// <summary>
/// 設備狀態註冊中心，負責管理所有 UID 的狀態與排程時間。
/// </summary>
public sealed class DeviceRegistry
{
    // 使用 ConcurrentDictionary 儲存設備狀態，key 為 UID。
    private readonly ConcurrentDictionary<int, DeviceState> _devices = new();

    // 一般檢查間隔（由設定注入）。
    private readonly TimeSpan _checkInterval;

    public DeviceRegistry(IOptions<DeviceCheckOptions> options)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _checkInterval = TimeSpan.FromSeconds(options.Value.CheckIntervalSeconds);

        // 初始化所有列管設備。Distinct() 可避免重複 UID。
        foreach (int uid in options.Value.Uids.Distinct())
        {
            _devices.TryAdd(uid, new DeviceState
            {
                Uid = uid,
                Alias = options.Value.UidAliases.TryGetValue(uid, out string? alias) ? alias : uid.ToString(),
                LastSeenUtc = now,
                NextCheckUtc = now
            });
        }
    }

    /// <summary>
    /// 取得全部設備狀態（依 UID 排序）。
    /// </summary>
    public IReadOnlyCollection<DeviceState> GetAll() => [.. _devices.Values.OrderBy(x => x.Uid)];

    /// <summary>
    /// 取得單一設備狀態；若未列管則回傳 null。
    /// </summary>
    public DeviceState? Get(int uid) => _devices.TryGetValue(uid, out DeviceState? state) ? state : null;

    /// <summary>
    /// 更新設備別名。
    /// </summary>
    public bool SetAlias(int uid, string alias)
    {
        if (!_devices.TryGetValue(uid, out DeviceState? state))
        {
            return false;
        }

        lock (state)
        {
            state.Alias = alias;
        }

        return true;
    }

    /// <summary>
    /// 處理設備心跳：更新 LastSeen 並把 NextCheck 往後延。
    /// </summary>
    public bool Touch(int uid)
    {
        if (!_devices.TryGetValue(uid, out DeviceState? state))
        {
            return false;
        }

        lock (state)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            state.LastSeenUtc = now;
            state.NextCheckUtc = now.Add(_checkInterval);
            state.Status = DeviceHealthStatus.Alive;
            state.LastResult = "heartbeat";
            state.ConsecutiveDeadCount = 0;
        }

        return true;
    }

    /// <summary>
    /// 挑出所有已到檢查時間的設備。
    /// </summary>
    public IReadOnlyList<DeviceState> DueForCheck(DateTimeOffset now)
    {
        List<DeviceState> due = [];

        foreach (DeviceState state in _devices.Values)
        {
            lock (state)
            {
                if (state.NextCheckUtc <= now)
                {
                    // 先暫時設成 MaxValue，避免在同一輪或並行流程被重複挑中。
                    state.NextCheckUtc = DateTimeOffset.MaxValue;
                    due.Add(state);
                }
            }
        }

        return due;
    }

    /// <summary>
    /// 套用探測結果並安排下次檢查時間。
    /// Dead 需要連續達標才視為確認異常，達標前狀態為 Unknown。
    /// </summary>
    public DeviceStatusTransition? UpdateAfterProbe(DeviceState state, DeviceHealthStatus status, string result, TimeSpan nextDelay, int deadConsecutiveThreshold)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DeviceStatusTransition? transition = null;

        lock (state)
        {
            DeviceHealthStatus previousStatus = state.Status;
            state.LastCheckedUtc = now;
            state.LastResult = result;

            state.Status = ResolveStatusWithDeadThreshold(state, status, deadConsecutiveThreshold);

            // 只有確認存活時才刷新 LastSeen。
            if (state.Status == DeviceHealthStatus.Alive)
            {
                state.LastSeenUtc = now;
            }

            state.NextCheckUtc = now.Add(nextDelay);

            if (HasDeadBoundaryChanged(previousStatus, state.Status))
            {
                transition = new DeviceStatusTransition
                {
                    Uid = state.Uid,
                    Alias = state.Alias,
                    FromStatus = previousStatus,
                    ToStatus = state.Status,
                    OccurredAtUtc = now,
                    Result = result
                };
            }
        }

        return transition;
    }

    private static DeviceHealthStatus ResolveStatusWithDeadThreshold(DeviceState state, DeviceHealthStatus probedStatus, int deadConsecutiveThreshold)
    {
        if (probedStatus == DeviceHealthStatus.Alive)
        {
            state.ConsecutiveDeadCount = 0;
            return DeviceHealthStatus.Alive;
        }

        if (probedStatus == DeviceHealthStatus.Busy)
        {
            state.ConsecutiveDeadCount = 0;
            return DeviceHealthStatus.Busy;
        }

        if (probedStatus == DeviceHealthStatus.Dead)
        {
            state.ConsecutiveDeadCount++;
            return state.ConsecutiveDeadCount >= deadConsecutiveThreshold
                ? DeviceHealthStatus.Dead
                : DeviceHealthStatus.Unknown;
        }

        state.ConsecutiveDeadCount = 0;
        return DeviceHealthStatus.Unknown;
    }

    /// <summary>
    /// 僅在「已確認異常（Dead）」邊界切換時才觸發通知。
    /// Alive/Busy/Unknown 之間互轉不通知。
    /// </summary>
    private static bool HasDeadBoundaryChanged(DeviceHealthStatus previous, DeviceHealthStatus current)
    {
        bool previousIsDead = previous == DeviceHealthStatus.Dead;
        bool currentIsDead = current == DeviceHealthStatus.Dead;
        return previousIsDead != currentIsDead;
    }
}
