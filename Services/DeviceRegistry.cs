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
    private sealed class DeviceDecisionState
    {
        public int AliveStreak { get; set; }
        public int AbnormalStreak { get; set; }
        public DeviceHealthStatus LastFinalStatus { get; set; } = DeviceHealthStatus.Unknown;
    }

    // 使用 ConcurrentDictionary 儲存設備狀態，key 為 UID。
    private readonly ConcurrentDictionary<int, DeviceState> _devices = new();
    private readonly ConcurrentDictionary<int, DeviceDecisionState> _decisionStates = new();

    // 一般檢查間隔（由設定注入）。
    private readonly TimeSpan _checkInterval;
    private readonly int _decisionThresholdCount;

    public DeviceRegistry(IOptions<DeviceCheckOptions> options)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _checkInterval = TimeSpan.FromSeconds(options.Value.CheckIntervalSeconds);
        _decisionThresholdCount = options.Value.DecisionThresholdCount;

        // 初始化所有列管設備。Distinct() 可避免重複 UID。
        foreach (int uid in options.Value.Uids.Distinct())
        {
            _devices.TryAdd(uid, new DeviceState
            {
                Uid = uid,
                LastSeenUtc = now,
                NextCheckUtc = now
            });

            _decisionStates.TryAdd(uid, new DeviceDecisionState());
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
    /// 心跳視為立即恢復正常判定。
    /// </summary>
    public bool Touch(int uid, out DeviceStatusTransition? transition)
    {
        transition = null;

        if (!_devices.TryGetValue(uid, out DeviceState? state) || !_decisionStates.TryGetValue(uid, out DeviceDecisionState? decisionState))
        {
            return false;
        }

        lock (state)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DeviceHealthStatus beforeFinal = decisionState.LastFinalStatus;

            state.LastSeenUtc = now;
            state.NextCheckUtc = now.Add(_checkInterval);
            state.Status = DeviceHealthStatus.Alive;
            state.LastResult = "heartbeat";

            decisionState.AliveStreak = _decisionThresholdCount;
            decisionState.AbnormalStreak = 0;
            decisionState.LastFinalStatus = DeviceHealthStatus.Alive;

            if (HasFinalNormalAbnormalBoundaryChange(beforeFinal, decisionState.LastFinalStatus))
            {
                transition = new DeviceStatusTransition
                {
                    Uid = state.Uid,
                    From = beforeFinal,
                    To = decisionState.LastFinalStatus,
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
    /// 需達到連續判定次數才會成為最終正常/異常；未達次數時為 Unknown。
    /// </summary>
    public DeviceStatusTransition? UpdateAfterProbe(DeviceState state, DeviceHealthStatus observedStatus, string result, TimeSpan nextDelay)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (!_decisionStates.TryGetValue(state.Uid, out DeviceDecisionState? decisionState))
        {
            return null;
        }

        lock (state)
        {
            bool observedAlive = observedStatus == DeviceHealthStatus.Alive;
            DeviceHealthStatus beforeFinal = decisionState.LastFinalStatus;
            DeviceHealthStatus afterStatus = DeviceHealthStatus.Unknown;

            if (observedAlive)
            {
                decisionState.AliveStreak++;
                decisionState.AbnormalStreak = 0;

                if (decisionState.AliveStreak >= _decisionThresholdCount)
                {
                    afterStatus = DeviceHealthStatus.Alive;
                    decisionState.LastFinalStatus = DeviceHealthStatus.Alive;
                    state.LastSeenUtc = now;
                }
            }
            else
            {
                decisionState.AbnormalStreak++;
                decisionState.AliveStreak = 0;

                if (decisionState.AbnormalStreak >= _decisionThresholdCount)
                {
                    afterStatus = observedStatus;
                    decisionState.LastFinalStatus = observedStatus;
                }
            }

            state.Status = afterStatus;
            state.LastCheckedUtc = now;
            state.LastResult = result;
            state.NextCheckUtc = now.Add(nextDelay);

            if (!HasFinalNormalAbnormalBoundaryChange(beforeFinal, decisionState.LastFinalStatus))
            {
                return null;
            }

            return new DeviceStatusTransition
            {
                Uid = state.Uid,
                From = beforeFinal,
                To = decisionState.LastFinalStatus,
                Trigger = "probe",
                Result = result,
                OccurredUtc = now
            };
        }
    }

    private static bool HasFinalNormalAbnormalBoundaryChange(DeviceHealthStatus before, DeviceHealthStatus after)
        => IsFinalNormal(before) != IsFinalNormal(after) && IsFinalVerdict(before) && IsFinalVerdict(after);

    private static bool IsFinalNormal(DeviceHealthStatus status) => status == DeviceHealthStatus.Alive;

    private static bool IsFinalVerdict(DeviceHealthStatus status)
        => status == DeviceHealthStatus.Alive || status == DeviceHealthStatus.Busy || status == DeviceHealthStatus.Dead;
}
