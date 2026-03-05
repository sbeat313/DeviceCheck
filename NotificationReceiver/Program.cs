using System.Collections.Concurrent;
using System.Threading;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

ConcurrentQueue<NotificationEnvelope> messages = new();
long seed = 0;

app.MapPost("/api/notifications", (NotificationPayload payload) =>
{
    messages.Enqueue(new NotificationEnvelope
    {
        Id = Interlocked.Increment(ref seed),
        ReceivedUtc = DateTimeOffset.UtcNow,
        Payload = payload
    });

    return Results.Accepted(new { message = "notification accepted" });
});

app.MapGet("/api/notifications", () => Results.Ok(messages.OrderBy(x => x.Id)));

app.MapDelete("/api/notifications", () =>
{
    while (messages.TryDequeue(out _))
    {
    }

    return Results.NoContent();
});

app.Run();

public sealed class NotificationPayload
{
    public required int Uid { get; init; }
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Trigger { get; init; }
    public required string Result { get; init; }
    public required DateTimeOffset OccurredUtc { get; init; }
}

public sealed class NotificationEnvelope
{
    public required long Id { get; init; }
    public required DateTimeOffset ReceivedUtc { get; init; }
    public required NotificationPayload Payload { get; init; }
}
