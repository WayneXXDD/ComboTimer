using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using Dalamud.Interface.ManagedFontAtlas;
using GameFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace WayneComboTimer;

internal sealed unsafe class StatisticsDisplay(IDtrBar dtr, IDalamudPluginInterface pi, Action<int> openSettings, Action changed) : IDisposable
{
    internal PingMonitor Monitor { get; } = new();
    private IFontHandle? pingFont;
    private float fontSize;
    private IDtrBarEntry? fpsEntry;
    private IDtrBarEntry? pingEntry;
    private readonly Queue<float> fpsHistory = new();
    private long lastFps;
    private Vector2 lastMonitorPosition = new(float.NaN);
    private Vector2 lastGraphPosition = new(float.NaN);

    internal void Update(bool fpsActive, bool pingActive, FpsSettings fps, PingSettings ping, uint dataCenterId)
    {
        Monitor.QueueSize = Math.Clamp(ping.PingQueueSize, 1, 300);
        Monitor.TrackingMode = ping.TrackingMode;
        Monitor.SetActive(pingActive, dataCenterId);
        if (!fpsActive)
        {
            fpsEntry?.Remove(); fpsEntry = null;
            fpsHistory.Clear(); lastFps = 0;
        }
        else if (Stopwatch.GetElapsedTime(lastFps).TotalSeconds >= 1)
        {
            lastFps = Stopwatch.GetTimestamp();
            var framework = GameFramework.Instance();
            if (framework != null)
            {
                float value = framework->FrameRate;
                if (float.IsFinite(value) && value > 0 && !framework->WindowInactive) fpsHistory.Enqueue(value);
                while (fpsHistory.Count > Math.Clamp(fps.HistorySnapshotCount, 1, 3600)) fpsHistory.Dequeue();
                try { fpsEntry ??= dtr.Get("FPS Display"); }
                catch (ArgumentException) { }
                if (fpsEntry != null)
                {
                    var format = fps.ShowDecimals ? "F2" : "F0";
                    string text = value.ToString(format);
                    if (!fps.NoLabels) text = fps.AlternativeFPSLabel ? text + "fps" : "FPS:" + text;
                    if (fps.ShowAverage && fpsHistory.Count > 0) text += " / " + (fps.NoLabels ? "" : "Avg:") + fpsHistory.Average().ToString(format);
                    if (fps.ShowMinimum && fpsHistory.Count > 0) text += " / " + (fps.NoLabels ? "" : "Min:") + fpsHistory.Min().ToString(format);
                    fpsEntry.Text = text;
                    fpsEntry.Shown = true;
                    fpsEntry.Tooltip = fps.DtrTooltip && fpsHistory.Count > 0
                        ? (Dalamud.Game.Text.SeStringHandling.SeString)$"Average: {fpsHistory.Average():F1}\nMinimum: {fpsHistory.Min():F1}" : null;
                    fpsEntry.OnClick = fps.DtrOpenSettings ? _ => openSettings(5) : null;
                }
            }
        }

        if (!pingActive || ping.DisplayMode != 3)
        {
            pingEntry?.Remove(); pingEntry = null;
        }
        else
        {
            try { pingEntry ??= dtr.Get("Ping0"); }
            catch (ArgumentException) { }
            if (pingEntry != null)
            {
                var value = Monitor.Current;
                var pieces = new List<string>();
                if (ping.ServerBarDisplayLast) pieces.Add(value.Last.HasValue ? $"{value.Last:F0}ms" : "-- ms");
                if (ping.ServerBarDisplayAverage) pieces.Add(value.History.Length > 0 ? $"{value.Average:F0}ms" : "-- ms");
                pingEntry.Text = string.Join("/", pieces);
                pingEntry.Tooltip = "Ping";
                pingEntry.Shown = true;
                pingEntry.OnClick = _ => openSettings(6);
            }
        }
    }

    internal void Draw(bool active, bool cutscene, PingSettings settings)
    {
        if (!active || (cutscene && settings.HideOverlaysDuringCutscenes)) return;
        var reading = Monitor.Current;
        if (!reading.Last.HasValue && settings.HideErrors) return;
        var desiredFontSize = Math.Clamp(settings.FontScale, 8, 128);
        if (fontSize != desiredFontSize && !ImGui.IsAnyItemActive())
        {
            fontSize = desiredFontSize;
            pingFont?.Dispose();
            var pixels = desiredFontSize;
            pingFont = pi.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(toolkit =>
            {
                var fontConfig = new SafeFontConfig { SizePx = pixels };
                toolkit.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansCjkMedium, fontConfig);
                toolkit.AttachExtraGlyphsForDalamudLanguage(fontConfig);
            }));
        }
        using var fontScope = pingFont?.Push();
        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.AlwaysAutoResize;
        if (settings.LockWindows) flags |= ImGuiWindowFlags.NoMove;
        if (settings.ClickThrough) flags |= ImGuiWindowFlags.NoInputs;
        if (settings.MonitorIsVisible && settings.DisplayMode != 3)
        {
            ImGui.SetNextWindowPos(settings.MonitorPosition,
                settings.MonitorPosition != lastMonitorPosition ? ImGuiCond.Always : ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(Math.Clamp(settings.MonitorBgAlpha, 0, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            ImGui.Begin("Ping###WaynePingMonitor", flags);
            // Keep the atlas' global scaling, just as PingPlugin does.
            // Reset the per-window factor left by the old integration during a live update.
            ImGui.SetWindowFontScale(1);
            var latest = reading.Last.HasValue ? $"{reading.Last:F0}ms" : "-- ms";
            string text;
            if (settings.DisplayMode == 2)
            {
                var values = new List<string>();
                if (settings.MicroDisplayLast) values.Add(latest);
                if (settings.MicroDisplayAverage) values.Add(reading.History.Length > 0 ? $"{reading.Average:F0}ms" : "-- ms");
                text = string.Join("/", values);
            }
            else
            {
                text = settings.MinimalDisplay || settings.DisplayMode == 1 ? latest :
                    $"Address: {reading.Address}\nPing: {latest}";
                if (!settings.HideAveragePing) text += "\nAverage: " +
                    (reading.History.Length > 0 ? $"{reading.Average:F2}ms" : "-- ms");
            }
            ImGui.TextColored(reading.Last.HasValue ? settings.MonitorFontColor : settings.MonitorErrorFontColor, text);
            var position = ImGui.GetWindowPos();
            if (position != settings.MonitorPosition) { settings.MonitorPosition = position; changed(); }
            lastMonitorPosition = position;
            ImGui.End();
            ImGui.PopStyleVar();
        }
        if (settings.GraphIsVisible)
        {
            ImGui.SetNextWindowPos(settings.GraphPosition,
                settings.GraphPosition != lastGraphPosition ? ImGuiCond.Always : ImGuiCond.FirstUseEver);
            ImGui.Begin("Ping history###WaynePingGraph", flags);
            var values = reading.History.Select(v => (float)v).ToArray();
            if (values.Length > 0)
                ImGui.PlotLines("##ping", values, 0, default, 0,
                    Math.Max(100, values.Max() * 1.1f), new Vector2(300, 120));
            var position = ImGui.GetWindowPos();
            if (position != settings.GraphPosition) { settings.GraphPosition = position; changed(); }
            lastGraphPosition = position;
            ImGui.End();
        }
    }

    public void Dispose()
    {
        fpsEntry?.Remove(); pingEntry?.Remove();
        pingFont?.Dispose();
        Monitor.Dispose();
    }
}
