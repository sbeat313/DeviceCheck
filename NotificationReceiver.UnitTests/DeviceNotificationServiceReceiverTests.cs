using DeviceCheck.Models;
using DeviceCheck.Options;
using DeviceCheck.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NotificationReceiver.UnitTests;

public sealed class DeviceNotificationServiceReceiverTests
{
    [Fact]
    public async Task NotifyTransitionAsync_ShouldPostTransition_ToIndependentReceiver()
    {
        int port = GetFreePort();
        string endpoint = $"http://127.0.0.1:{port}/api/notifications/";

        using HttpListener listener = new();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        Task<string> receiveTask = Task.Run(async () =>
        {
            HttpListenerContext context = await listener.GetContextAsync();

            using StreamReader reader = new(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
            string body = await reader.ReadToEndAsync();

            context.Response.StatusCode = (int)HttpStatusCode.Accepted;
            context.Response.Close();

            return body;
        });

        DeviceCheckOptions options = new()
        {
            BaseUrl = "http://127.0.0.1:5099",
            Uids = [1001],
            NotificationEndpoints = [endpoint]
        };

        using HttpClient httpClient = new();
        DeviceNotificationService service = new(httpClient, Options.Create(options), NullLogger<DeviceNotificationService>.Instance);

        DeviceStatusTransition transition = new()
        {
            Uid = 1001,
            From = DeviceHealthStatus.Alive,
            To = DeviceHealthStatus.Dead,
            Trigger = "probe",
            Result = "503 ServiceUnavailable",
            OccurredUtc = DateTimeOffset.UtcNow
        };

        await service.NotifyTransitionAsync(transition, CancellationToken.None);

        string receivedBody = await receiveTask;
        DeviceStatusTransition? received = JsonSerializer.Deserialize<DeviceStatusTransition>(receivedBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(received);
        Assert.Equal(transition.Uid, received!.Uid);
        Assert.Equal(transition.From, received.From);
        Assert.Equal(transition.To, received.To);
        Assert.Equal(transition.Trigger, received.Trigger);
        Assert.Equal(transition.Result, received.Result);
    }

    private static int GetFreePort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
