using DeviceCheck.Options;
using DeviceCheck.Services;

// 建立 ASP.NET Core WebApplication builder
var builder = WebApplication.CreateBuilder(args);

// 綁定並驗證 DeviceCheck 設定，啟動時就先檢查避免執行中才失敗
builder.Services
    .AddOptions<DeviceCheckOptions>()
    .Bind(builder.Configuration.GetSection(DeviceCheckOptions.SectionName))
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "DeviceCheck:BaseUrl 必須是合法的絕對 URL")
    .Validate(o => o.CheckIntervalSeconds > 0, "DeviceCheck:CheckIntervalSeconds 必須大於 0")
    .Validate(o => o.BusyRetryDelaySeconds > 0, "DeviceCheck:BusyRetryDelaySeconds 必須大於 0")
    .Validate(o => o.RequestTimeoutSeconds > 0, "DeviceCheck:RequestTimeoutSeconds 必須大於 0")
    .Validate(o => o.Uids.Count > 0, "DeviceCheck:Uids 不能為空")
    .ValidateOnStart();

// 註冊核心服務：狀態儲存、探測 HTTP Client、背景檢查服務
builder.Services.AddSingleton<DeviceRegistry>();
builder.Services.AddHttpClient<DeviceProbeClient>();
builder.Services.AddHostedService<DeviceMonitorService>();

var app = builder.Build();

// 心跳 API：設備主動回報存活，系統會將該 UID 的下次檢查時間往後延
app.MapPost("/api/devices/{uid:int}/heartbeat", (int uid, DeviceRegistry registry) =>
{
    return registry.Touch(uid)
        ? Results.Ok(new { uid, message = "heartbeat accepted" })
        : Results.NotFound(new { uid, message = "uid not tracked" });
});

// 取得所有設備狀態 API
app.MapGet("/api/devices", (DeviceRegistry registry) => Results.Ok(registry.GetAll()));

// 取得單一設備狀態 API
app.MapGet("/api/devices/{uid:int}", (int uid, DeviceRegistry registry) =>
{
    var state = registry.Get(uid);
    return state is null ? Results.NotFound(new { uid, message = "uid not tracked" }) : Results.Ok(state);
});

app.Run();
