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
            return existing;

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
                        pi.SavePluginConfig(imported);
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
        pi.SavePluginConfig(defaults);
        return defaults;
    }
}
