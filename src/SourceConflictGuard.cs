using System.Text.Json;

namespace WayneComboTimer;

// Uses public loaded-plugin state and read-only Simple Tweaks settings, never reflection or hooks.
internal sealed class SourceConflictGuard(string? simpleTweaksConfigPath)
{
    internal static readonly IReadOnlyList<string> Sources = Array.AsReadOnly(new[]
    {
        "SimpleTweaksPlugin", "EnmityHp", "EnemyListDebuffs", "Distance",
        "BigPlayerDebuffs", "FPSPlugin", "PingPlugin"
    });
    private DateTime cachedWriteTime;
    private long cachedLength = -1;
    private bool? cachedSimpleState;
    internal bool SimpleStateUnknown { get; private set; }

    internal bool[] Detect(IEnumerable<(string InternalName, bool IsLoaded)> plugins)
    {
        var loaded = plugins.Where(p => p.IsLoaded).Select(p => p.InternalName).ToHashSet(StringComparer.Ordinal);
        bool? simpleEnabled = loaded.Contains(Sources[0]) ? ReadSimpleState() : false;
        SimpleStateUnknown = simpleEnabled is null;
        var blocked = new bool[Sources.Count];
        // Loading Simple Tweaks alone is harmless; only its Combo Timer overlaps.
        // If its state cannot be read, block our timer until a valid state is available.
        blocked[0] = loaded.Contains(Sources[0]) && simpleEnabled != false;
        for (int i = 1; i < blocked.Length; i++)
            blocked[i] = loaded.Contains(Sources[i]);
        return blocked;
    }

    internal static bool DisableModules(bool[] enabled, IReadOnlyList<bool> blocked)
    {
        bool changed = false;
        for (int i = 0; i < Math.Min(enabled.Length, blocked.Count); i++)
        {
            if (!blocked[i] || !enabled[i]) continue;
            enabled[i] = false;
            changed = true;
        }
        // Never enable a module merely because another plugin has stopped.
        return changed;
    }

    private bool? ReadSimpleState()
    {
        if (simpleTweaksConfigPath is null) return null;
        try
        {
            var file = new FileInfo(simpleTweaksConfigPath);
            if (!file.Exists) return null;
            if (cachedSimpleState.HasValue && file.LastWriteTimeUtc == cachedWriteTime && file.Length == cachedLength)
                return cachedSimpleState;
            var state = ParseSimpleState(File.ReadAllText(simpleTweaksConfigPath));
            if (state.HasValue)
            {
                cachedWriteTime = file.LastWriteTimeUtc;
                cachedLength = file.Length;
                cachedSimpleState = state;
            }
            return state;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return null;
        }
    }

    internal static bool? ParseSimpleState(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("EnabledTweaks", out var enabled)) return null;
            // Support legacy Newtonsoft collection metadata as well as current plain arrays.
            if (enabled.ValueKind == JsonValueKind.Object &&
                enabled.TryGetProperty("$values", out var values)) enabled = values;
            if (enabled.ValueKind != JsonValueKind.Array) return null;
            bool combo = false;
            foreach (var item in enabled.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String) return null;
                combo |= item.GetString() is "UiAdjustments@ComboTimer" or "ComboTimer";
            }
            return combo;
        }
        catch (JsonException) { return null; }
    }
}
