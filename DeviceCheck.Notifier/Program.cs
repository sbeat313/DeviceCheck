using DeviceCheck.Notifier.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

WebApplication app = builder.Build();

app.MapPost("/api/notifications", (NotificationRequest request, ILoggerFactory loggerFactory) =>
{
    ILogger logger = loggerFactory.CreateLogger("Notifier");
    logger.LogInformation(
        "[通知] UID={Uid}, 別名={Alias}, 狀態由 {FromStatus} -> {ToStatus}, 訊息: {Message}, 時間: {OccurredAtUtc}",
        request.Uid,
        request.Alias,
        request.FromStatus,
        request.ToStatus,
        request.Message,
        request.OccurredAtUtc);

    return Results.Ok(new
    {
        message = "notification accepted"
    });
});

app.MapGet("/", () => Results.Ok(new { service = "DeviceCheck.Notifier", status = "running" }));

app.Run();
