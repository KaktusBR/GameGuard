using System.IO;
using System.Text.Json;

namespace GameGuard.Core;

public static class ConfigStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static GameGuardConfig Load(string path)
    {
        if (!File.Exists(path)) return GameGuardConfig.Default();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<GameGuardConfig>(json, Options) ?? GameGuardConfig.Default();
    }

    public static void Save(string path, GameGuardConfig config)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(config, Options));
    }
}
