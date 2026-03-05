namespace DeviceCheck.Models;

/// <summary>
/// 設備健康狀態。
/// </summary>
public enum DeviceHealthStatus
{
    /// <summary>尚未檢查。</summary>
    Unknown,

    /// <summary>設備存活。</summary>
    Alive,

    /// <summary>設備忙碌中，稍後重試。</summary>
    Busy,

    /// <summary>設備不存活或探測失敗。</summary>
    Dead
}
