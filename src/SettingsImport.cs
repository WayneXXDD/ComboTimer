using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WayneComboTimer;

// Imports only this plugin's known setting types. Newtonsoft $type metadata is treated as ordinary data.
internal static class SettingsImport
{
    internal static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true };

    internal static T Read<T>(string json) where T : class, new()
        => Normalize(JsonNode.Parse(json))?.Deserialize<T>(JsonOptions) ?? new T();

    private static JsonNode? Normalize(JsonNode? node)
    {
        if (node is JsonArray array)
            return new JsonArray(array.Select(Normalize).ToArray());
        if (node is not JsonObject obj)
            return node?.DeepClone();
        if (obj["$values"] is JsonArray values)
            return Normalize(values);

        var result = new JsonObject();
        foreach (var (key, value) in obj)
        {
            if (key.StartsWith('$'))
                continue;
            var name = key.Length > 1 && key[0] == 'm' && char.IsUpper(key[1]) ? key[1..] : key;
            if (name != key && obj.ContainsKey(name))
                continue;
            result[name] = Normalize(value);
        }
        return result;
    }
    internal static void ImportPingWindowPositions(PingSettings settings, string ini)
    {
        int window = 0;
        foreach (var raw in ini.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith('['))
            {
                window = line.StartsWith("[Window]") && line.Contains("###PingPluginMonitor]") ? 1 :
                    line.StartsWith("[Window]") && line.Contains("###PingPluginGraph]") ? 2 : 0;
                continue;
            }
            if (window == 0 || !line.StartsWith("Pos=")) continue;
            var values = line[4..].Split(',');
            if (values.Length != 2 ||
                !float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                !float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
                !float.IsFinite(x) || !float.IsFinite(y))
                continue;
            if (window == 1) settings.MonitorPosition = new Vector2(x, y);
            else settings.GraphPosition = new Vector2(x, y);
        }
    }

}
