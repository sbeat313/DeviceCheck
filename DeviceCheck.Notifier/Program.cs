using DeviceCheck.Notifier.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 極簡通知接收服務：僅接收並記錄 DeviceCheck 的狀態轉換通知。
WebApplication app = builder.Build();

// 接收通知事件並以結構化日誌輸出。
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

// 健康檢查端點。
app.MapGet("/", () => Results.Ok(new { service = "DeviceCheck.Notifier", status = "running" }));

app.Run();
