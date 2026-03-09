using DeviceCheck.Models;
using DeviceCheck.Options;
using DeviceCheck.Services;
using NLog.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 業務設定集中於 config.json，啟用 reloadOnChange 讓設定調整可即時生效。
builder.Configuration.AddJsonFile("config.json", optional: false, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

// DeviceCheck 監控主設定驗證。
builder.Services
    .AddOptions<DeviceCheckOptions>()
    .Bind(builder.Configuration.GetSection(DeviceCheckOptions.SectionName))
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "DeviceCheck:BaseUrl 必須是合法的絕對 URL")
    .Validate(o => o.CheckIntervalSeconds > 0, "DeviceCheck:CheckIntervalSeconds 必須大於 0")
    .Validate(o => o.BusyRetryDelaySeconds > 0, "DeviceCheck:BusyRetryDelaySeconds 必須大於 0")
    .Validate(o => o.RequestTimeoutSeconds > 0, "DeviceCheck:RequestTimeoutSeconds 必須大於 0")
    .Validate(o => o.ProbeRetryCount >= 0, "DeviceCheck:ProbeRetryCount 必須大於或等於 0")
    .Validate(o => o.ProbeRetryDelaySeconds >= 0, "DeviceCheck:ProbeRetryDelaySeconds 必須大於或等於 0")
    .Validate(o => o.Uids.Count > 0, "DeviceCheck:Uids 不能為空")
    .ValidateOnStart();

// 通知設定驗證（通知啟用時必須提供合法 Endpoint）。
builder.Services
    .AddOptions<NotificationOptions>()
    .Bind(builder.Configuration.GetSection(NotificationOptions.SectionName))
    .Validate(o => !o.Enabled || Uri.TryCreate(o.EndpointUrl, UriKind.Absolute, out _), "Notification:EndpointUrl 必須是合法的絕對 URL")
    .ValidateOnStart();

builder.Services.AddSingleton<AliasConfigService>();
builder.Services.AddSingleton<DeviceRegistry>();
builder.Services.AddHttpClient<DeviceProbeClient>();
builder.Services.AddHttpClient<NotificationClient>();
builder.Services.AddHostedService<DeviceMonitorService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

// 僅提供 API，不做環境區分，啟動即開放 Swagger 方便測試與整合。
app.UseSwagger();
app.UseSwaggerUI();

// 設備主動心跳：成功則刷新 LastSeen 與下一次檢查時間。
app.MapPost("/api/devices/{uid:int}/heartbeat", (int uid, DeviceRegistry registry) =>
{
    return registry.Touch(uid)
        ? Results.Ok(new { uid, message = "heartbeat accepted" })
        : Results.NotFound(new { uid, message = "uid not tracked" });
});

app.MapGet("/api/devices", (DeviceRegistry registry) => Results.Ok(registry.GetAll()));

// 查詢單一設備目前狀態。
app.MapGet("/api/devices/{uid:int}", (int uid, DeviceRegistry registry) =>
{
    DeviceState? state = registry.Get(uid);
    return state is null ? Results.NotFound(new { uid, message = "uid not tracked" }) : Results.Ok(state);
});

app.MapPut("/api/devices/{uid:int}/alias", (int uid, UpdateAliasRequest request, DeviceRegistry registry, AliasConfigService aliasConfigService) =>
{
    // 避免空白別名寫入。
    string alias = request.Alias.Trim();
    if (string.IsNullOrWhiteSpace(alias))
    {
        return Results.BadRequest(new { uid, message = "alias cannot be empty" });
    }

    if (!registry.SetAlias(uid, alias))
    {
        return Results.NotFound(new { uid, message = "uid not tracked" });
    }

    aliasConfigService.UpdateAlias(uid, alias);
    return Results.Ok(new { uid, alias, message = "alias updated" });
});

app.Run();
