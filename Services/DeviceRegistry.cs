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
    /// 處理設備心跳：更新 LastSeen 並把 NextCheck 往後延。
    /// 若跨越正常/異常邊界，會回傳轉換事件。
    /// </summary>
    public bool Touch(int uid, out DeviceStatusTransition? transition)
    {
        transition = null;

        if (!_devices.TryGetValue(uid, out DeviceState? state))
        {
            return false;
        }

        lock (state)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DeviceHealthStatus before = state.Status;

            state.LastSeenUtc = now;
            state.NextCheckUtc = now.Add(_checkInterval);
            state.Status = DeviceHealthStatus.Alive;
            state.LastResult = "heartbeat";

            if (HasNormalAbnormalBoundaryChange(before, state.Status))
            {
                transition = new DeviceStatusTransition
                {
                    Uid = state.Uid,
                    From = before,
                    To = state.Status,
                    Trigger = "heartbeat",
                    Result = state.LastResult,
                    OccurredUtc = now
                };
            }
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
    /// 若跨越正常/異常邊界，會回傳轉換事件。
    /// </summary>
    public DeviceStatusTransition? UpdateAfterProbe(DeviceState state, DeviceHealthStatus status, string result, TimeSpan nextDelay)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        lock (state)
        {
            DeviceHealthStatus before = state.Status;

            state.Status = status;
            state.LastCheckedUtc = now;
            state.LastResult = result;

            // 只有確認存活時才刷新 LastSeen。
            if (status == DeviceHealthStatus.Alive)
            {
                state.LastSeenUtc = now;
            }

            state.NextCheckUtc = now.Add(nextDelay);

            if (!HasNormalAbnormalBoundaryChange(before, status))
            {
                return null;
            }

            return new DeviceStatusTransition
            {
                Uid = state.Uid,
                From = before,
                To = status,
                Trigger = "probe",
                Result = result,
                OccurredUtc = now
            };
        }
    }

    private static bool HasNormalAbnormalBoundaryChange(DeviceHealthStatus before, DeviceHealthStatus after)
        => IsNormal(before) != IsNormal(after);

    private static bool IsNormal(DeviceHealthStatus status) => status == DeviceHealthStatus.Alive;
}
