namespace DeviceCheck.Options;

public sealed class DeviceCheckOptions
{
    public const string SectionName = "DeviceCheck";

    public string BaseUrl { get; set; } = string.Empty;

    public int CheckIntervalSeconds { get; set; } = 10;

    public int BusyRetryDelaySeconds { get; set; } = 3;

    public int RequestTimeoutSeconds { get; set; } = 5;

    public List<int> Uids { get; set; } = [];
}
