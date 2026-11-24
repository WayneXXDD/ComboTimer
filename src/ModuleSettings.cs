using System.Numerics;

namespace WayneComboTimer;

public sealed class EnemyHpSettings
{
    public bool IsLeftAligned { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public float Size { get; set; } = 1;
    public Vector4? Color { get; set; }
    public Vector4? BGColor { get; set; }
    public float FrameBorder { get; set; }
    public Vector4 FrameBorderColor { get; set; } = new(.66f, 0, 0, 1);
    public float PaddingX { get; set; } = 2;
    public float PaddingY { get; set; }
}

public sealed class EnemyDebuffSettings
{
    public bool Enabled { get; set; } = true;
    public int UpdateInterval { get; set; } = 100;
    public int GroupX { get; set; } = -79;
    public int GroupY { get; set; }
    public int NodeSpacing { get; set; }
    public float Scale { get; set; } = 1;
    public bool FillFromRight { get; set; } = true;
    public int IconX { get; set; }
    public int IconY { get; set; }
    public int IconWidth { get; set; } = 18;
    public int IconHeight { get; set; } = 24;
    public int DurationX { get; set; } = -2;
    public int DurationY { get; set; } = 16;
    public int FontSize { get; set; } = 12;
    public int DurationPadding { get; set; } = 2;
    public Vector4 DurationTextColor { get; set; } = Vector4.One;
    public Vector4 DurationEdgeColor { get; set; } = new(0, 0, 0, 1);
}

public sealed class PersonalDebuffSettings
{
    public float Scale { get; set; } = 1.2f;
}

public sealed class DistanceSettings
{
    public List<DistanceWidget> DistanceWidgetConfigs { get; set; } = [];
}

public sealed class DistanceWidget
{
    public string WidgetName { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool HideInCombat { get; set; }
    public bool HideOutOfCombat { get; set; }
    public Vector2 TextPosition { get; set; } = new(0, 0);
    public int ApplicableTargetType { get; set; } = 6;
    public int UIAttachType { get; set; }
    public bool DistanceIsToRing { get; set; } = true;
    public float DistanceOffset_Yalms { get; set; }
    public bool ShowUnits { get; set; } = true;
    public bool ShowDistanceModeMarker { get; set; }
    public int DecimalPrecision { get; set; } = 2;
    public bool AllowNegativeDistances { get; set; }
    public int FontSize { get; set; } = 14;
    public bool FontHeavy { get; set; }
    public int FontAlignment { get; set; } = 8;
    public Vector4 TextColor { get; set; } = new(1, .97f, .69f, 1);
    public Vector4 TextEdgeColor { get; set; } = new(.38f, .31f, 0, 1);
    public bool TrackTargetBarTextColor { get; set; } = true;
    public bool UseDistanceBasedColor { get; set; }
    public Vector4 NearThresholdTextColor { get; set; } = new(1, .7f, 0, 1);
    public Vector4 NearThresholdTextEdgeColor { get; set; } = new(.4f, .26f, 0, 1);
    public Vector4 FarThresholdTextColor { get; set; } = new(.93f, .28f, .07f, 1);
    public Vector4 FarThresholdTextEdgeColor { get; set; } = new(.3f, .07f, 0, 1);
    public float FarThresholdDistance_Yalms { get; set; } = 3.5f;
    public float NearThresholdDistance_Yalms { get; set; } = 2;
    public DistanceFilters Filters { get; set; } = new();
}

public sealed class DistanceFilters
{
    public bool ShowDistanceOnPlayers { get; set; } = true;
    public bool ShowDistanceOnBattleNpc { get; set; } = true;
    public bool ShowDistanceOnEventNpc { get; set; }
    public bool ShowDistanceOnTreasure { get; set; }
    public bool ShowDistanceOnAetheryte { get; set; }
    public bool ShowDistanceOnGatheringNode { get; set; }
    public bool ShowDistanceOnEventObj { get; set; }
    public bool ShowDistanceOnCompanion { get; set; }
    public bool ShowDistanceOnHousing { get; set; }
}

public sealed class FpsSettings
{
    public bool ShowDecimals { get; set; }
    public bool Enable { get; set; } = true;
    public bool ShowAverage { get; set; }
    public bool ShowMinimum { get; set; }
    public bool NoLabels { get; set; }
    public bool AlternativeFPSLabel { get; set; } = true;
    public bool DtrTooltip { get; set; } = true;
    public bool DtrOpenSettings { get; set; } = true;
    public int HistorySnapshotCount { get; set; } = 300;
}

public sealed class PingSettings
{
    public Vector2 GraphPosition { get; set; } = new(600, 150);
    public Vector2 MonitorPosition { get; set; } = new(300, 150);
    public float FontScale { get; set; } = 17;
    public float MonitorBgAlpha { get; set; }
    public Vector4 MonitorFontColor { get; set; } = new(1, 1, 0, 1);
    public Vector4 MonitorErrorFontColor { get; set; } = new(1, 0, 0, 1);
    public bool ClickThrough { get; set; } = true;
    public bool GraphIsVisible { get; set; }
    public bool MonitorIsVisible { get; set; } = true;
    public bool LockWindows { get; set; } = true;
    public bool MinimalDisplay { get; set; }
    public bool MicroDisplayLast { get; set; } = true;
    public bool MicroDisplayAverage { get; set; }
    public bool ServerBarDisplayLast { get; set; } = true;
    public bool ServerBarDisplayAverage { get; set; }
    public int DisplayMode { get; set; }
    public bool HideErrors { get; set; }
    public bool HideOverlaysDuringCutscenes { get; set; }
    public bool HideAveragePing { get; set; }
    public int TrackingMode { get; set; }
    public int PingQueueSize { get; set; } = 20;
}
