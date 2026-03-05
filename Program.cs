using DeviceCheck.Models;
using DeviceCheck.Options;
using DeviceCheck.Services;
using NLog.Web;

// 建立 ASP.NET Core WebApplication builder
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 啟用 NLog：改由 NLog 接管日誌輸出
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// 綁定並驗證 DeviceCheck 設定，啟動時就先檢查避免執行中才失敗
builder.Services
    .AddOptions<DeviceCheckOptions>()
    .Bind(builder.Configuration.GetSection(DeviceCheckOptions.SectionName))
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "DeviceCheck:BaseUrl 必須是合法的絕對 URL")
    .Validate(o => o.CheckIntervalSeconds > 0, "DeviceCheck:CheckIntervalSeconds 必須大於 0")
    .Validate(o => o.BusyRetryDelaySeconds > 0, "DeviceCheck:BusyRetryDelaySeconds 必須大於 0")
    .Validate(o => o.RequestTimeoutSeconds > 0, "DeviceCheck:RequestTimeoutSeconds 必須大於 0")
    .Validate(o => o.Uids.Count > 0, "DeviceCheck:Uids 不能為空")
    .Validate(o => o.NotificationRecipients.TrueForAll(x => !string.IsNullOrWhiteSpace(x)), "DeviceCheck:NotificationRecipients 不能包含空白收件人")
    .ValidateOnStart();

// 註冊核心服務：狀態儲存、探測 HTTP Client、通知、模擬接收端、背景檢查服務
builder.Services.AddSingleton<DeviceRegistry>();
builder.Services.AddSingleton<SimulatedNotificationReceiver>();
builder.Services.AddSingleton<DeviceNotificationService>();
builder.Services.AddHttpClient<DeviceProbeClient>();
builder.Services.AddHostedService<DeviceMonitorService>();

WebApplication app = builder.Build();

// 心跳 API：設備主動回報存活，系統會將該 UID 的下次檢查時間往後延
app.MapPost("/api/devices/{uid:int}/heartbeat", async (int uid, DeviceRegistry registry, DeviceNotificationService notificationService) =>
{
    bool touched = registry.Touch(uid, out DeviceStatusTransition? transition);
    if (!touched)
    {
        return Results.NotFound(new { uid, message = "uid not tracked" });
    }

    if (transition is not null)
    {
        await notificationService.NotifyTransitionAsync(transition, CancellationToken.None);
    }

    return Results.Ok(new { uid, message = "heartbeat accepted" });
});

// 取得所有設備狀態 API
app.MapGet("/api/devices", (DeviceRegistry registry) => Results.Ok(registry.GetAll()));

// 取得單一設備狀態 API
app.MapGet("/api/devices/{uid:int}", (int uid, DeviceRegistry registry) =>
{
    DeviceState? state = registry.Get(uid);
    return state is null ? Results.NotFound(new { uid, message = "uid not tracked" }) : Results.Ok(state);
});

// 模擬通知接收端 API：查看已接收通知
app.MapGet("/api/notifications/simulated", (SimulatedNotificationReceiver receiver) => Results.Ok(receiver.GetAll()));

// 模擬通知接收端 API：手動塞入通知（方便整合測試）
app.MapPost("/api/notifications/simulated", (DeviceStatusTransition transition, SimulatedNotificationReceiver receiver) =>
{
    receiver.Receive("manual-test", transition);
    return Results.Accepted();
});

// 模擬通知接收端 API：清空佇列
app.MapDelete("/api/notifications/simulated", (SimulatedNotificationReceiver receiver) =>
{
    receiver.Clear();
    return Results.NoContent();
});

app.Run();
