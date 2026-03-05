using DeviceCheck.Models;
using System.Collections.Concurrent;
using System.Threading;

namespace DeviceCheck.Services;

/// <summary>
/// 模擬通知接收端（In-Memory）。
/// </summary>
public sealed class SimulatedNotificationReceiver
{
    private readonly ConcurrentQueue<SimulatedNotificationMessage> _messages = new();
    private long _idSeed;

    public void Receive(string recipient, DeviceStatusTransition transition)
    {
        string category = transition.To == DeviceHealthStatus.Alive ? "異常→正常" : "正常→異常";

        _messages.Enqueue(new SimulatedNotificationMessage
        {
            Id = Interlocked.Increment(ref _idSeed),
            Recipient = recipient,
            Uid = transition.Uid,
            From = transition.From,
            To = transition.To,
            Category = category,
            Trigger = transition.Trigger,
            Result = transition.Result,
            ReceivedUtc = DateTimeOffset.UtcNow
        });
    }

    public IReadOnlyCollection<SimulatedNotificationMessage> GetAll()
        => [.. _messages.OrderBy(x => x.Id)];

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }
}
