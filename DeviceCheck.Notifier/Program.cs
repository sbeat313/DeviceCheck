using DeviceCheck.Notifier.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

WebApplication app = builder.Build();

app.MapPost("/api/notifications", (NotificationRequest request, ILoggerFactory loggerFactory) =>
{
    ILogger logger = loggerFactory.CreateLogger("Notifier");
    logger.LogInformation(
        "[通知] UID={Uid}, 狀態由 {FromStatus} -> {ToStatus}, 通知對象: {Recipients}, 訊息: {Message}, 時間: {OccurredAtUtc}",
        request.Uid,
        request.FromStatus,
        request.ToStatus,
        string.Join(",", request.Recipients),
        request.Message,
        request.OccurredAtUtc);

    return Results.Ok(new
    {
        message = "notification accepted",
        receivedRecipients = request.Recipients.Count
    });
});

app.MapGet("/", () => Results.Ok(new { service = "DeviceCheck.Notifier", status = "running" }));

app.Run();
