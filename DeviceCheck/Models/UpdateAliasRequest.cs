namespace DeviceCheck.Models;

/// <summary>
/// 更新設備別名請求。
/// </summary>
public sealed class UpdateAliasRequest
{
    public string Alias { get; set; } = string.Empty;
}
