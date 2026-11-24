using System.Numerics;
using System.Text.Json;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace WayneComboTimer;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool IntegrationImported { get; set; }
    public bool[] Modules { get; set; } = [true, false, false, false, false, false, false];
    public bool Collapsed { get; set; }
    public Vector2 WindowSize { get; set; } = new(920, 680);
    public EnemyHpSettings EnemyHp { get; set; } = new();
    public EnemyDebuffSettings EnemyDebuffs { get; set; } = new();
    public PersonalDebuffSettings PersonalDebuffs { get; set; } = new();
    public DistanceSettings Distance { get; set; } = new();
    public FpsSettings Fps { get; set; } = new();
    public PingSettings Ping { get; set; } = new();
    public bool AlwaysVisible { get; set; }
    public bool NoComboText { get; set; }
    public int FontSize { get; set; } = 12;
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public Vector4 Color { get; set; } = Vector4.One;
    public Vector4 EdgeColor { get; set; } = new(240 / 255f, 142 / 255f, 55 / 255f, 1);
    public bool LeadingZero { get; set; } = true;
    public int DecimalPlaces { get; set; } = 2;
    public bool UseScreenText { get; set; }

    internal static Configuration Load(IDalamudPluginInterface pi, IPluginLog log)
    {
        if (pi.GetPluginConfig() is Configuration existing)
        {
            existing.Modules = NormalizeModules(existing.Modules);
            if (!existing.IntegrationImported)
                existing.ImportOriginalSettings(pi, log);
            return existing;
        }

        var root = pi.ConfigDirectory.Parent;
        if (root != null)
        {
            var file = Path.Combine(root.FullName, "SimpleTweaksPlugin", "UiAdjustments@ComboTimer.json");
            try
            {
                if (File.Exists(file))
                {
                    var imported = JsonSerializer.Deserialize<Configuration>(
                        File.ReadAllText(file), new JsonSerializerOptions { IncludeFields = true });
                    if (imported != null)
                    {
                        imported.ImportOriginalSettings(pi, log);
                        return imported;
                    }
                }
            }
            catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
            {
                log.Warning(e, "Could not import Simple Tweaks Combo Timer appearance.");
            }
        }

        var defaults = new Configuration();
        defaults.ImportOriginalSettings(pi, log);
        return defaults;
    }
    internal static bool[] NormalizeModules(bool[]? modules)
    {
        var result = new bool[7];
        result[0] = true;
        if (modules != null)
            Array.Copy(modules, result, Math.Min(result.Length, modules.Length));
        return result;
    }

    internal List<string> ImportOriginalSettings(IDalamudPluginInterface pi, IPluginLog log)
    {
        var messages = new List<string>();
        var directory = pi.ConfigDirectory.Parent?.FullName;
        if (directory == null)
            return ["找不到原設定資料夾。"];

        // Each file is deserialized before assignment, so one malformed file cannot reset another module.
        Import("EnmityHp", 1, json => EnemyHp = SettingsImport.Read<EnemyHpSettings>(json));
        Import("EnemyListDebuffs", 2, json =>
        {
            var settings = SettingsImport.Read<EnemyDebuffSettings>(json);
            EnemyDebuffs = settings;
            Modules[2] = settings.Enabled;
        });
        Import("Distance", 3, json => Distance = SettingsImport.Read<DistanceSettings>(json));
        Import("BigPlayerDebuffs", 4, json =>
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("bScale", out var scale) || !scale.TryGetSingle(out var value) ||
                !float.IsFinite(value) || value <= 0)
                throw new JsonException("Invalid personal status scale.");
            PersonalDebuffs = new PersonalDebuffSettings { Scale = value };
        });
        Import("FPSPlugin", 5, json =>
        {
            var settings = SettingsImport.Read<FpsSettings>(json);
            Fps = settings;
            Modules[5] = settings.Enable;
        });
        Import("PingPlugin", 6, json =>
        {
            var settings = SettingsImport.Read<PingSettings>(json);
            // ImGui may have saved a moved monitor's position separately from PingPlugin.json.
            var iniPath = Path.Combine(Directory.GetParent(directory)?.FullName ?? directory, "dalamudUI.ini");
            if (File.Exists(iniPath))
                SettingsImport.ImportPingWindowPositions(settings, File.ReadAllText(iniPath));
            if (settings.TrackingMode == 3) settings.TrackingMode = 0;
            Ping = settings;
        });
        Version = 2;
        IntegrationImported = true;
        pi.SavePluginConfig(this);
        return messages;

        void Import(string name, int index, Action<string> apply)
        {
            var path = Path.Combine(directory, name + ".json");
            if (!File.Exists(path))
            {
                messages.Add(name + "：沒有找到設定檔");
                return;
            }
            try
            {
                var content = File.ReadAllText(path);
                var wasEnabled = Modules[index];
                Modules[index] = true;
                try { apply(content); }
                catch { Modules[index] = wasEnabled; throw; }
                messages.Add(name + "：已匯入");
            }
            catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
            {
                messages.Add(name + "：匯入失敗，可稍後重試");
                log.Warning(e, "Could not import settings from {Plugin}.", name);
            }
        }
    }

}
