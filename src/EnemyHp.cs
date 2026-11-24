using System.Numerics;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WayneComboTimer;

internal static unsafe class EnemyHp
{
    internal static void Draw(AtkUnitBase* addon, EnemyHpSettings settings)
    {
        if (addon == null || !addon->IsVisible || addon->UldManager.NodeListCount < 12) return;
        for (var i = 0; i < 8; i++)
        {
            var node = addon->UldManager.NodeList[4 + i];
            if (node == null || !node->IsVisible() || (int)node->Type < 1000) continue;
            var tile = (AtkComponentNode*)node;
            if (tile->Component == null || tile->Component->UldManager.NodeListCount <= 10) continue;
            var bar = tile->Component->UldManager.NodeList[10];
            if (bar == null) continue;
            var label = (Math.Clamp(bar->ScaleX, 0, 1) * 100).ToString("0") + "%";
            var scale = Math.Clamp(settings.Size, .1f, 5);
            var size = ImGui.CalcTextSize(label) * scale;
            var position = new Vector2(addon->X + node->X * addon->Scale - 2,
                addon->Y + (node->Y + node->Height / 2f) * addon->Scale);
            position += new Vector2(settings.OffsetX - (settings.IsLeftAligned ? 0 : size.X),
                settings.OffsetY - size.Y / 2);
            ImGui.SetNextWindowViewport(ImGui.GetMainViewport().ID);
            ImGui.SetNextWindowPos(ImGui.GetMainViewport().Pos + position);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(settings.PaddingX, settings.PaddingY));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, settings.FrameBorder);
            ImGui.PushStyleColor(ImGuiCol.Border, settings.FrameBorderColor);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, settings.BGColor ?? ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg]);
            ImGui.PushStyleColor(ImGuiCol.Text, settings.Color ?? ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
            try
            {
                ImGui.Begin("##WayneEnemyHp" + i, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing);
                ImGui.SetWindowFontScale(scale);
                ImGui.TextUnformatted(label);
                ImGui.End();
            }
            finally
            {
                ImGui.PopStyleColor(3);
                ImGui.PopStyleVar(3);
            }
        }
    }
}
