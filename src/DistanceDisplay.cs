// Distance anchoring, colors and target selection follow PunishedPineapple/Distance (MIT).
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WayneComboTimer;

internal sealed unsafe class DistanceDisplay(IGameGui gui, ITargetManager targets, IObjectTable objects)
{
    internal static readonly string[] Hosts = ["_TargetInfo", "_TargetInfoMainTarget", "_FocusTargetInfo", "_ScreenText"];
    private const uint FirstId = 0x57546000;
    private readonly Dictionary<nint, int> previousCounts = [];

    internal void Update(AtkUnitBase* addon, string host, bool enabled, bool combat, DistanceSettings settings)
    {
        if (addon == null || addon->RootNode == null) return;
        var widgets = settings.DistanceWidgetConfigs ?? [];
        var count = Math.Min(32, widgets.Count);
        previousCounts.TryGetValue((nint)addon, out var oldCount);
        for (int i = count; i < oldCount; i++) NativeNodes.RemoveText(addon, FirstId + (uint)i);
        previousCounts[(nint)addon] = count;

        for (int i = 0; i < count; i++)
        {
            var config = widgets[i];
            var id = FirstId + (uint)i;
            var node = (AtkTextNode*)NativeNodes.Find(addon, id);
            var player = objects.LocalPlayer;
            var target = GetTarget(config.ApplicableTargetType);
            bool show = enabled && config.Enabled && player != null && target != null &&
                target.GameObjectId != player.GameObjectId && Allows(config.Filters, target.ObjectKind) &&
                !(combat && config.HideInCombat) && !(!combat && config.HideOutOfCombat) &&
                host == HostFor(config);
            if (!show)
            {
                if (node != null) node->ToggleVisibility(false);
                continue;
            }
            if (node == null)
            {
                node = NativeNodes.Text(id);
                if (node == null) continue;
                node->SetHeight(14);
                NativeNodes.Append(addon->RootNode, (AtkResNode*)node);
                addon->UldManager.UpdateDrawNodeList();
            }

            var delta = target!.Position - player!.Position;
            var distance = MathF.Sqrt(delta.X * delta.X + delta.Z * delta.Z);
            if (config.DistanceIsToRing) distance -= target.HitboxRadius;
            distance -= config.DistanceOffset_Yalms;
            var displayed = config.AllowNegativeDistances ? distance : Math.Max(0, distance);
            var marker = config.ShowDistanceModeMarker ? (config.DistanceIsToRing ? "◯ " : "· ") : "";
            node->SetText(marker + displayed.ToString("F" + Math.Clamp(config.DecimalPrecision, 0, 4)) +
                (config.ShowUnits ? "y" : ""));
            var position = config.TextPosition;
            if (config.UIAttachType == 4 || (config.UIAttachType == 0 && config.ApplicableTargetType is 3 or 4 or 5))
                position += ImGui.GetMousePos() - ImGui.GetMainViewport().Pos;
            node->SetPositionFloat(position.X, position.Y);
            node->FontSize = (byte)Math.Clamp(config.FontSize, 6, 80);
            node->AlignmentType = (AlignmentType)Math.Clamp(config.FontAlignment, 0, 8);
            node->FontType = config.FontHeavy ? FontType.MiedingerMed : FontType.Axis;
            var color = config.TextColor;
            var edge = config.TextEdgeColor;
            if (config.UseDistanceBasedColor)
            {
                if (distance > config.FarThresholdDistance_Yalms)
                {
                    color = config.FarThresholdTextColor; edge = config.FarThresholdTextEdgeColor;
                }
                else if (distance > config.NearThresholdDistance_Yalms)
                {
                    color = config.NearThresholdTextColor; edge = config.NearThresholdTextEdgeColor;
                }
            }
            NativeNodes.Colors(node, color, edge);
            node->Color.A = 255;
            if (config.TrackTargetBarTextColor) MatchTargetColor(node, config.ApplicableTargetType);
            node->ToggleVisibility(true);
        }
    }

    private string HostFor(DistanceWidget config)
    {
        int attach = config.UIAttachType;
        if (attach == 1 || attach == 4) return "_ScreenText";
        if (attach == 3 || (attach == 0 && config.ApplicableTargetType == 2)) return "_FocusTargetInfo";
        if (attach == 0 && config.ApplicableTargetType is 3 or 4 or 5) return "_ScreenText";
        var split = gui.GetAddonByName<AtkUnitBase>("_TargetInfoMainTarget");
        return split != null && split->IsVisible ? "_TargetInfoMainTarget" : "_TargetInfo";
    }

    private IGameObject? GetTarget(int type) => type switch
    {
        0 => targets.Target,
        1 => targets.SoftTarget,
        2 => targets.FocusTarget,
        3 => UiMouseover() ?? targets.MouseOverTarget,
        4 => targets.MouseOverTarget,
        5 => UiMouseover(),
        6 => targets.SoftTarget ?? targets.Target,
        7 => (targets.SoftTarget ?? targets.Target)?.TargetObject,
        _ => null
    };

    private IGameObject? UiMouseover()
    {
        var module = PronounModule.Instance();
        return module != null && module->UiMouseOverTarget != null
            ? objects.CreateObjectReference((nint)module->UiMouseOverTarget) : null;
    }

    private static bool Allows(DistanceFilters? f, ObjectKind kind)
    {
        f ??= new();
        return kind switch
        {
            ObjectKind.Pc => f.ShowDistanceOnPlayers,
            ObjectKind.BattleNpc => f.ShowDistanceOnBattleNpc,
            ObjectKind.EventNpc => f.ShowDistanceOnEventNpc,
            ObjectKind.Treasure => f.ShowDistanceOnTreasure,
            ObjectKind.Aetheryte => f.ShowDistanceOnAetheryte,
            ObjectKind.GatheringPoint => f.ShowDistanceOnGatheringNode,
            ObjectKind.EventObj => f.ShowDistanceOnEventObj,
            ObjectKind.Companion => f.ShowDistanceOnCompanion,
            ObjectKind.HousingEventObject => f.ShowDistanceOnHousing,
            _ => false
        };
    }

    private void MatchTargetColor(AtkTextNode* destination, int type)
    {
        var addon = gui.GetAddonByName<AtkUnitBase>("_TargetInfo");
        var split = gui.GetAddonByName<AtkUnitBase>("_TargetInfoMainTarget");
        int index = type == 7 ? 49 : 39;
        if (type == 2)
        {
            addon = gui.GetAddonByName<AtkUnitBase>("_FocusTargetInfo");
            index = 10;
        }
        else if (addon == null || !addon->IsVisible)
        {
            addon = split;
            index = type == 7 ? 12 : 8;
        }
        if (addon == null || addon->UldManager.NodeListCount <= index) return;
        var source = addon->UldManager.NodeList[index];
        if (source == null || source->Type != NodeType.Text) return;
        var text = (AtkTextNode*)source;
        destination->TextColor = text->TextColor;
        destination->EdgeColor = text->EdgeColor;
        destination->Color.A = text->Color.A;
    }

    internal void Release(AtkUnitBase* addon)
    {
        if (addon == null || !previousCounts.Remove((nint)addon, out var count)) return;
        for (int i = 0; i < count; i++) NativeNodes.RemoveText(addon, FirstId + (uint)i);
    }
}
