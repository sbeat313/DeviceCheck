using DeviceCheck.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DeviceCheck.Services;

/// <summary>
/// 管理 UID 別名設定並同步寫回 config.json。
/// </summary>
public sealed class AliasConfigService(IHostEnvironment environment, IOptions<DeviceCheckOptions> options)
{
    private readonly string _configPath = Path.Combine(environment.ContentRootPath, "config.json");
    private readonly DeviceCheckOptions _options = options.Value;
    private readonly Lock _sync = new();


    public bool UpdateAlias(int uid, string alias)
    {
        _options.UidAliases[uid] = alias;

        lock (_sync)
        {
            JsonObject root;

            if (File.Exists(_configPath))
            {
                root = JsonNode.Parse(File.ReadAllText(_configPath))?.AsObject() ?? [];
            }
            else
            {
                root = [];
            }

            JsonObject deviceCheck = root[DeviceCheckOptions.SectionName]?.AsObject() ?? [];
            JsonObject aliases = deviceCheck["UidAliases"]?.AsObject() ?? [];
            aliases[uid.ToString()] = alias;
            deviceCheck["UidAliases"] = aliases;
            root[DeviceCheckOptions.SectionName] = deviceCheck;

            File.WriteAllText(_configPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        return true;
    }
}
