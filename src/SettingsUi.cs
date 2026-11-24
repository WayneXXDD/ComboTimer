using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace WayneComboTimer;

internal sealed class SettingsUi : IDisposable
{
    internal static readonly string[] Names = ["Combo Timer", "EnemyListHP", "EnemyListDebuffs", "Distance", "BigPlayerDebuffs", "FPS Plugin", "PingPlugin"];
    internal static readonly string[] Sources = ["SimpleTweaksPlugin", "EnmityHp", "EnemyListDebuffs", "Distance",
        "BigPlayerDebuffs", "FPSPlugin", "PingPlugin"];
    private readonly IDalamudPluginInterface pi;
    private readonly Configuration c;
    private readonly IPluginLog log;
    private readonly Func<int, bool> sourceLoaded;
    private readonly Action changed;
    private readonly IFontHandle? font;
    private List<string> importResults = [];
    private int selected;
    private bool open;
    private bool resize = true;
    internal string PingStatus { get; set; } = "正在取得測量位址…";

    internal SettingsUi(IDalamudPluginInterface pi, Configuration config, IPluginLog log,
        Func<int, bool> sourceLoaded, Action changed)
    {
        this.pi = pi; c = config; this.log = log; this.sourceLoaded = sourceLoaded; this.changed = changed;
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msjh.ttc");
        if (File.Exists(path))
        {
            font = pi.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(toolkit =>
            {
                toolkit.AddFontFromFile(path, new SafeFontConfig
                {
                    SizePx = 17,
                    GlyphRanges = [0x20, 0x7e, 0x3000, 0x30ff, 0x3400, 0x9fff, 0]
                });
            }));
        }
    }

    internal void Open(int page = 0) { selected = Math.Clamp(page, 0, 6); open = true; }

    internal void Draw()
    {
        if (!open) return;
        using var scope = font?.Push();
        var scale = ImGui.GetIO().FontGlobalScale;
        if (resize || c.Collapsed)
            ImGui.SetNextWindowSize(c.Collapsed ? new(c.WindowSize.X, ImGui.GetFrameHeight() + 4) : c.WindowSize, ImGuiCond.Always);
        else
            ImGui.SetNextWindowSize(c.WindowSize, ImGuiCond.FirstUseEver);
        resize = false;
        var flags = ImGuiWindowFlags.NoCollapse;
        if (c.Collapsed) flags |= ImGuiWindowFlags.NoResize;
        var visible = ImGui.Begin("Combo Timer###WayneComboTimer", ref open, flags);
        DrawCollapseButton(scale);
        if (visible && !c.Collapsed)
        {
            var size = ImGui.GetWindowSize();
            if (size != c.WindowSize) { c.WindowSize = size; changed(); }
            ImGui.BeginChild("modules", new Vector2(205 * scale, 0), true);
            ImGui.TextUnformatted("功能");
            ImGui.Separator();
            for (int i = 0; i < Names.Length; i++)
            {
                ImGui.PushID(i);
                var enabled = c.Modules[i];
                if (ImGui.Checkbox("##enabled", ref enabled))
                {
                    c.Modules[i] = enabled;
                    if (i == 2) c.EnemyDebuffs.Enabled = enabled;
                    if (i == 5) c.Fps.Enable = enabled;
                    changed();
                }
                ImGui.SameLine();
                if (ImGui.Selectable(Names[i], selected == i)) selected = i;
                ImGui.PopID();
                ImGui.Spacing();
            }
            ImGui.Spacing();
            if (ImGui.Button("匯入原設定"))
            {
                importResults = c.ImportOriginalSettings(pi, log);
                changed();
            }
            if (importResults.Count != 0 && ImGui.IsItemHovered())
                ImGui.SetTooltip(string.Join("\n", importResults));
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.BeginChild("settings", new Vector2(0, 0), false);
            ImGui.TextUnformatted(Names[selected]);
            ImGui.Separator();
            if (selected != 0 && sourceLoaded(selected))
            {
                ImGui.TextWrapped("原插件仍載入，這個模組暫停。停用原插件後會自動接手，沿用已匯入的設定。");
                if (ImGui.Button("開啟原插件清單")) pi.OpenPluginInstallerTo(searchText: Sources[selected]);
                ImGui.Separator();
            }
            switch (selected)
            {
                case 0: Combo(); break;
                case 1: EnemyHp(); break;
                case 2: EnemyDebuffs(); break;
                case 3: Distances(); break;
                case 4: Float("減益縮放", c.PersonalDebuffs.Scale, v => c.PersonalDebuffs.Scale = v, .5f, 4); break;
                case 5: Fps(); break;
                case 6: Ping(); break;
            }
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextUnformatted("設定自動儲存");
            ImGui.EndChild();
        }
        ImGui.End();
    }

    private void DrawCollapseButton(float scale)
    {
        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        var height = ImGui.GetFrameHeight();
        var max = pos + new Vector2(size.X - 32 * scale, height - 2);
        var min = new Vector2(max.X - 26 * scale, pos.Y + 2);
        var hovered = ImGui.IsMouseHoveringRect(min, max, false) && ImGui.IsWindowHovered();
        var draw = ImGui.GetWindowDrawList();
        draw.PushClipRect(pos, pos + size, false);
        if (hovered) draw.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.ButtonHovered), 3);
        var center = (min + max) / 2;
        float direction = c.Collapsed ? 1 : -1;
        var color = ImGui.GetColorU32(ImGuiCol.Text);
        draw.AddLine(center + new Vector2(-5, -2 * direction), center + new Vector2(0, 3 * direction), color, 1.5f);
        draw.AddLine(center + new Vector2(0, 3 * direction), center + new Vector2(5, -2 * direction), color, 1.5f);
        draw.PopClipRect();
        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            c.Collapsed = !c.Collapsed;
            resize = true;
            changed();
        }
    }

    private void Combo()
    {
        Check("隱藏 Combo 文字", c.NoComboText, v => c.NoComboText = v);
        Check("常駐顯示", c.AlwaysVisible, v => c.AlwaysVisible = v);
        Int("字體大小", c.FontSize, v => c.FontSize = v, 6, 255);
        Int("小數位數", c.DecimalPlaces, v => c.DecimalPlaces = v, 0, 4);
        Int("X 位置偏移", c.OffsetX, v => c.OffsetX = v);
        Int("Y 位置偏移", c.OffsetY, v => c.OffsetY = v);
        Color("文字顏色", c.Color, v => c.Color = v);
        Color("外框顏色", c.EdgeColor, v => c.EdgeColor = v);
        Check("補前導零", c.LeadingZero, v => c.LeadingZero = v);
        Check("使用替代介面掛載點", c.UseScreenText, v => c.UseScreenText = v);
        ImGui.Spacing();
        ImGui.TextUnformatted("顯示預覽");
        ImGui.TextColored(c.Color, (c.NoComboText ? "" : "Combo\n") + 57.83f.ToString("F" + Math.Clamp(c.DecimalPlaces, 0, 4)));
    }

    private void EnemyHp()
    {
        var s = c.EnemyHp;
        Check("靠左對齊", s.IsLeftAligned, v => s.IsLeftAligned = v);
        Int("X 位置偏移", s.OffsetX, v => s.OffsetX = v);
        Int("Y 位置偏移", s.OffsetY, v => s.OffsetY = v);
        Float("大小", s.Size, v => s.Size = v, .1f, 5);
        NullableColor("自訂文字顏色", s.Color, v => s.Color = v);
        NullableColor("自訂背景顏色", s.BGColor, v => s.BGColor = v);
        Float("邊框寬度", s.FrameBorder, v => s.FrameBorder = v, 0, 5);
        Color("邊框顏色", s.FrameBorderColor, v => s.FrameBorderColor = v);
        Float("水平留白", s.PaddingX, v => s.PaddingX = v, 0, 20);
        Float("垂直留白", s.PaddingY, v => s.PaddingY = v, 0, 20);
    }

    private void EnemyDebuffs()
    {
        var s = c.EnemyDebuffs;
        Int("更新間隔 ms", s.UpdateInterval, v => s.UpdateInterval = v, 16, 5000);
        Int("X 位置偏移", s.GroupX, v => s.GroupX = v);
        Int("Y 位置偏移", s.GroupY, v => s.GroupY = v);
        Float("縮放", s.Scale, v => s.Scale = v, .1f, 5);
        Int("圖示間距", s.NodeSpacing, v => s.NodeSpacing = v);
        Check("從右側填入", s.FillFromRight, v => s.FillFromRight = v);
        Int("圖示寬度", s.IconWidth, v => s.IconWidth = v, 1, 128);
        Int("圖示高度", s.IconHeight, v => s.IconHeight = v, 1, 128);
        Int("圖示 X 偏移", s.IconX, v => s.IconX = v);
        Int("圖示 Y 偏移", s.IconY, v => s.IconY = v);
        Int("倒數 X 偏移", s.DurationX, v => s.DurationX = v);
        Int("倒數 Y 偏移", s.DurationY, v => s.DurationY = v);
        Int("倒數字體", s.FontSize, v => s.FontSize = v, 6, 80);
        Int("倒數留白", s.DurationPadding, v => s.DurationPadding = v, 0, 30);
        Color("倒數顏色", s.DurationTextColor, v => s.DurationTextColor = v);
        Color("倒數外框顏色", s.DurationEdgeColor, v => s.DurationEdgeColor = v);
    }

    private void Distances()
    {
        var widgets = c.Distance.DistanceWidgetConfigs;
        for (int i = 0; i < widgets.Count; i++)
        {
            var w = widgets[i];
            ImGui.PushID(i);
            ImGui.TextUnformatted(string.IsNullOrWhiteSpace(w.WidgetName) ? "距離顯示 " + (i + 1) : w.WidgetName);
            Check("啟用這個顯示項目", w.Enabled, v => w.Enabled = v);
            Select("追蹤對象", w.ApplicableTargetType, v => w.ApplicableTargetType = v,
                ["目標", "暫選目標", "焦點目標", "滑鼠目標", "場上滑鼠目標", "介面滑鼠目標", "目標與暫選目標", "目標的目標"]);
            Select("掛載位置", w.UIAttachType, v => w.UIAttachType = v,
                ["自動", "螢幕", "目標欄", "焦點目標欄", "滑鼠位置"]);
            Position("位置偏移", w.TextPosition, v => w.TextPosition = v);
            Int("字體大小", w.FontSize, v => w.FontSize = v, 6, 80);
            Int("小數位數", w.DecimalPrecision, v => w.DecimalPrecision = v, 0, 4);
            Select("文字對齊", w.FontAlignment, v => w.FontAlignment = v,
                ["左上", "上中", "右上", "左中", "置中", "右中", "左下", "下中", "右下"]);
            Check("粗體", w.FontHeavy, v => w.FontHeavy = v);
            Check("計算到目標圈邊緣", w.DistanceIsToRing, v => w.DistanceIsToRing = v);
            Float("距離修正", w.DistanceOffset_Yalms, v => w.DistanceOffset_Yalms = v, -100, 100);
            Check("顯示單位", w.ShowUnits, v => w.ShowUnits = v);
            Check("顯示距離模式標記", w.ShowDistanceModeMarker, v => w.ShowDistanceModeMarker = v);
            Check("允許負數", w.AllowNegativeDistances, v => w.AllowNegativeDistances = v);
            Check("戰鬥中隱藏", w.HideInCombat, v => w.HideInCombat = v);
            Check("非戰鬥中隱藏", w.HideOutOfCombat, v => w.HideOutOfCombat = v);
            Color("文字顏色", w.TextColor, v => w.TextColor = v);
            Color("外框顏色", w.TextEdgeColor, v => w.TextEdgeColor = v);
            Check("沿用目標欄文字顏色", w.TrackTargetBarTextColor, v => w.TrackTargetBarTextColor = v);
            Check("依距離變色", w.UseDistanceBasedColor, v => w.UseDistanceBasedColor = v);
            if (w.UseDistanceBasedColor)
            {
                Float("近距離門檻", w.NearThresholdDistance_Yalms, v => w.NearThresholdDistance_Yalms = v, 0, 100);
                Float("遠距離門檻", w.FarThresholdDistance_Yalms, v => w.FarThresholdDistance_Yalms = v, 0, 100);
                Color("近距離顏色", w.NearThresholdTextColor, v => w.NearThresholdTextColor = v);
                Color("近距離外框", w.NearThresholdTextEdgeColor, v => w.NearThresholdTextEdgeColor = v);
                Color("遠距離顏色", w.FarThresholdTextColor, v => w.FarThresholdTextColor = v);
                Color("遠距離外框", w.FarThresholdTextEdgeColor, v => w.FarThresholdTextEdgeColor = v);
            }
            Check("顯示玩家距離", w.Filters.ShowDistanceOnPlayers, v => w.Filters.ShowDistanceOnPlayers = v);
            Check("顯示戰鬥 NPC 距離", w.Filters.ShowDistanceOnBattleNpc, v => w.Filters.ShowDistanceOnBattleNpc = v);
            Check("顯示事件 NPC 距離", w.Filters.ShowDistanceOnEventNpc, v => w.Filters.ShowDistanceOnEventNpc = v);
            Check("顯示寶箱距離", w.Filters.ShowDistanceOnTreasure, v => w.Filters.ShowDistanceOnTreasure = v);
            Check("顯示水晶距離", w.Filters.ShowDistanceOnAetheryte, v => w.Filters.ShowDistanceOnAetheryte = v);
            Check("顯示採集點距離", w.Filters.ShowDistanceOnGatheringNode, v => w.Filters.ShowDistanceOnGatheringNode = v);
            Check("顯示事件物件距離", w.Filters.ShowDistanceOnEventObj, v => w.Filters.ShowDistanceOnEventObj = v);
            Check("顯示寵物距離", w.Filters.ShowDistanceOnCompanion, v => w.Filters.ShowDistanceOnCompanion = v);
            Check("顯示房屋物件距離", w.Filters.ShowDistanceOnHousing, v => w.Filters.ShowDistanceOnHousing = v);
            if (ImGui.Button("移除此項"))
            {
                widgets.RemoveAt(i); changed(); ImGui.PopID(); return;
            }
            ImGui.Separator();
            ImGui.PopID();
        }
        if (widgets.Count < 32 && ImGui.Button("新增距離顯示")) { widgets.Add(new()); changed(); }
    }

    private void Fps()
    {
        var s = c.Fps;
        Check("顯示小數", s.ShowDecimals, v => s.ShowDecimals = v);
        Check("顯示平均值", s.ShowAverage, v => s.ShowAverage = v);
        Check("顯示最低值", s.ShowMinimum, v => s.ShowMinimum = v);
        Check("隱藏標籤", s.NoLabels, v => s.NoLabels = v);
        Check("使用 fps 後綴", s.AlternativeFPSLabel, v => s.AlternativeFPSLabel = v);
        Check("顯示滑鼠提示", s.DtrTooltip, v => s.DtrTooltip = v);
        Check("點擊 DTR 開啟設定", s.DtrOpenSettings, v => s.DtrOpenSettings = v);
        Int("歷史樣本數", s.HistorySnapshotCount, v => s.HistorySnapshotCount = v, 1, 3600);
    }

    private void Ping()
    {
        ImGui.TextWrapped(PingStatus);
        ImGui.Separator();
        var s = c.Ping;
        Select("顯示模式", s.DisplayMode, v => s.DisplayMode = v, ["完整浮窗", "精簡浮窗", "微型浮窗", "DTR"]);
        Select("測量方式", s.TrackingMode, v => s.TrackingMode = v, ["自動", "ICMP", "Windows API"]);
        Check("顯示浮窗", s.MonitorIsVisible, v => s.MonitorIsVisible = v);
        Check("顯示歷史圖", s.GraphIsVisible, v => s.GraphIsVisible = v);
        Check("鎖定視窗", s.LockWindows, v => s.LockWindows = v);
        Check("滑鼠穿透", s.ClickThrough, v => s.ClickThrough = v);
        Position("浮窗位置", s.MonitorPosition, v => s.MonitorPosition = v);
        Position("歷史圖位置", s.GraphPosition, v => s.GraphPosition = v);
        Float("字體大小", s.FontScale, v => s.FontScale = v, 8, 128);
        Float("背景透明度", s.MonitorBgAlpha, v => s.MonitorBgAlpha = v, 0, 1);
        Color("文字顏色", s.MonitorFontColor, v => s.MonitorFontColor = v);
        Color("錯誤顏色", s.MonitorErrorFontColor, v => s.MonitorErrorFontColor = v);
        Check("精簡數值顯示", s.MinimalDisplay, v => s.MinimalDisplay = v);
        Check("隱藏平均值", s.HideAveragePing, v => s.HideAveragePing = v);
        Check("微型顯示目前值", s.MicroDisplayLast, v => s.MicroDisplayLast = v);
        Check("微型顯示平均值", s.MicroDisplayAverage, v => s.MicroDisplayAverage = v);
        Check("DTR 顯示目前值", s.ServerBarDisplayLast, v => s.ServerBarDisplayLast = v);
        Check("DTR 顯示平均值", s.ServerBarDisplayAverage, v => s.ServerBarDisplayAverage = v);
        Check("隱藏錯誤顯示", s.HideErrors, v => s.HideErrors = v);
        Check("過場中隱藏", s.HideOverlaysDuringCutscenes, v => s.HideOverlaysDuringCutscenes = v);
        Int("平均值樣本數", s.PingQueueSize, v => s.PingQueueSize = v, 1, 300);
    }

    private void Check(string label, bool value, Action<bool> set)
    {
        if (ImGui.Checkbox(label, ref value)) { set(value); changed(); }
    }
    private void Int(string label, int value, Action<int> set, int min = -5000, int max = 5000)
    {
        ImGui.SetNextItemWidth(170);
        if (ImGui.DragInt(label, ref value, 1, min, max)) { set(Math.Clamp(value, min, max)); changed(); }
    }
    private void Float(string label, float value, Action<float> set, float min, float max)
    {
        ImGui.SetNextItemWidth(170);
        if (ImGui.DragFloat(label, ref value, .001f, min, max)) { set(Math.Clamp(value, min, max)); changed(); }
    }
    private void Color(string label, Vector4 value, Action<Vector4> set)
    {
        if (ImGui.ColorEdit4(label, ref value)) { set(value); changed(); }
    }
    private void NullableColor(string label, Vector4? value, Action<Vector4?> set)
    {
        bool enabled = value.HasValue;
        if (ImGui.Checkbox(label, ref enabled)) { set(enabled ? Vector4.One : null); changed(); }
        if (enabled)
        {
            var color = value ?? Vector4.One;
            ImGui.PushID(label);
            if (ImGui.ColorEdit4("##color", ref color)) { set(color); changed(); }
            ImGui.PopID();
        }
    }
    private void Position(string label, Vector2 value, Action<Vector2> set)
    {
        ImGui.SetNextItemWidth(220);
        if (ImGui.DragFloat2(label, ref value)) { set(value); changed(); }
    }
    private void Select(string label, int value, Action<int> set, string[] options)
    {
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo(label, ref value, options, options.Length)) { set(value); changed(); }
    }
    public void Dispose() => font?.Dispose();
}
