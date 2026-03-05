using DeviceCheck.Options;
using DeviceCheck.Services;
using NLog.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services
    .AddOptions<DeviceCheckOptions>()
    .Bind(builder.Configuration.GetSection(DeviceCheckOptions.SectionName))
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "DeviceCheck:BaseUrl 必須是合法的絕對 URL")
    .Validate(o => o.CheckIntervalSeconds > 0, "DeviceCheck:CheckIntervalSeconds 必須大於 0")
    .Validate(o => o.BusyRetryDelaySeconds > 0, "DeviceCheck:BusyRetryDelaySeconds 必須大於 0")
    .Validate(o => o.RequestTimeoutSeconds > 0, "DeviceCheck:RequestTimeoutSeconds 必須大於 0")
    .Validate(o => o.Uids.Count > 0, "DeviceCheck:Uids 不能為空")
    .ValidateOnStart();

builder.Services
    .AddOptions<NotificationOptions>()
    .Bind(builder.Configuration.GetSection(NotificationOptions.SectionName))
    .Validate(o => !o.Enabled || Uri.TryCreate(o.EndpointUrl, UriKind.Absolute, out _), "Notification:EndpointUrl 必須是合法的絕對 URL")
    .Validate(o => !o.Enabled || o.Recipients.Count > 0, "Notification:Recipients 在啟用通知時不能為空")
    .ValidateOnStart();

builder.Services.AddSingleton<DeviceRegistry>();
builder.Services.AddHttpClient<DeviceProbeClient>();
builder.Services.AddHttpClient<NotificationClient>();
builder.Services.AddHostedService<DeviceMonitorService>();

WebApplication app = builder.Build();

app.MapPost("/api/devices/{uid:int}/heartbeat", (int uid, DeviceRegistry registry) =>
{
    return registry.Touch(uid)
        ? Results.Ok(new { uid, message = "heartbeat accepted" })
        : Results.NotFound(new { uid, message = "uid not tracked" });
});

app.MapGet("/api/devices", (DeviceRegistry registry) => Results.Ok(registry.GetAll()));

app.MapGet("/api/devices/{uid:int}", (int uid, DeviceRegistry registry) =>
{
    DeviceCheck.Models.DeviceState? state = registry.Get(uid);
    return state is null ? Results.NotFound(new { uid, message = "uid not tracked" }) : Results.Ok(state);
});

app.Run();
