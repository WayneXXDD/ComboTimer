using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WayneComboTimer;

public sealed unsafe class Plugin : IDalamudPlugin
{
    private static readonly string[] Hosts = ["_ParameterWidget", "_ScreenText"];
    private readonly IDalamudPluginInterface pi;
    private readonly IAddonLifecycle lifecycle;
    private readonly IGameGui gameGui;
    private readonly IObjectTable objects;
    private readonly ICondition conditions;
    private readonly IClientState client;
    private readonly ICommandManager commands;
    private readonly IFramework framework;
    private readonly Configuration config;
    private readonly TimerLogic timer;
    private readonly HudText hud = new();
    private bool settingsOpen;
    private bool settingsDirty;

    public Plugin(IDalamudPluginInterface pi, IDataManager data, IObjectTable objects, IClientState client,
        ICondition conditions, IGameGui gameGui, IAddonLifecycle lifecycle, IFramework framework,
        ICommandManager commands, IPluginLog log)
    {
        this.pi = pi;
        this.objects = objects;
        this.client = client;
        this.conditions = conditions;
        this.gameGui = gameGui;
        this.lifecycle = lifecycle;
        this.framework = framework;
        this.commands = commands;
        config = Configuration.Load(pi, log);
        timer = new TimerLogic(data);
        lifecycle.RegisterListener(AddonEvent.PreDraw, Hosts, OnDrawHud);
        lifecycle.RegisterListener(AddonEvent.PreFinalize, Hosts, OnFinalizeHud);
        pi.UiBuilder.Draw += DrawSettings;
        pi.UiBuilder.OpenConfigUi += OpenSettings;
        commands.AddHandler("/waynecombo", new CommandInfo((_, _) => OpenSettings()) { HelpMessage = "Open Combo Timer settings." });
    }

    private void OnDrawHud(AddonEvent _, AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (args.AddonName != (config.UseScreenText ? Hosts[1] : Hosts[0]))
        {
            hud.Remove(addon);
            return;
        }

        if (config.UseScreenText && (conditions[ConditionFlag.WatchingCutscene] ||
            conditions[ConditionFlag.WatchingCutscene78] || conditions[ConditionFlag.OccupiedInCutSceneEvent]))
        {
            hud.Draw(addon, config, null, false);
            return;
        }

        var player = objects.LocalPlayer;
        var viperPve = player?.ClassJob.RowId == TimerLogic.ViperJob && !client.IsPvP;
        var seconds = player == null ? null : timer.Remaining(player, viperPve);
        hud.Draw(addon, config, seconds, !viperPve && config.AlwaysVisible);
    }

    private void OnFinalizeHud(AddonEvent _, AddonArgs args) => hud.Remove((AtkUnitBase*)args.Addon.Address);
    private void OpenSettings() => settingsOpen = true;

    private void DrawSettings()
    {
        if (!settingsOpen)
            return;
        bool visible = ImGui.Begin("Combo Timer###WayneComboTimer", ref settingsOpen);
        if (visible)
        {
            ImGui.TextWrapped("Viper shows the remaining enhancement only while Steel Fangs or Reaving Fangs is highlighted. Other jobs use the normal combo timer.");
            var label = config.NoComboText;
            if (ImGui.Checkbox("Hide 'Combo' text", ref label)) { config.NoComboText = label; settingsDirty = true; }
            var idle = config.AlwaysVisible;
            if (ImGui.Checkbox("Always visible on other jobs", ref idle)) { config.AlwaysVisible = idle; settingsDirty = true; }
            var size = config.FontSize;
            if (ImGui.SliderInt("Font size", ref size, 6, 255)) { config.FontSize = size; settingsDirty = true; }
            var x = config.OffsetX;
            if (ImGui.DragInt("X position offset", ref x)) { config.OffsetX = x; settingsDirty = true; }
            var y = config.OffsetY;
            if (ImGui.DragInt("Y position offset", ref y)) { config.OffsetY = y; settingsDirty = true; }
            var color = config.Color;
            if (ImGui.ColorEdit4("Text color", ref color)) { config.Color = color; settingsDirty = true; }
            var edge = config.EdgeColor;
            if (ImGui.ColorEdit4("Outline color", ref edge)) { config.EdgeColor = edge; settingsDirty = true; }
            var leading = config.LeadingZero;
            if (ImGui.Checkbox("Leading zero", ref leading)) { config.LeadingZero = leading; settingsDirty = true; }
            var decimals = config.DecimalPlaces;
            if (ImGui.SliderInt("Decimal places", ref decimals, 0, 4)) { config.DecimalPlaces = decimals; settingsDirty = true; }
            var screen = config.UseScreenText;
            if (ImGui.Checkbox("Alternative UI attachment", ref screen)) { config.UseScreenText = screen; settingsDirty = true; }
        }
        ImGui.End();
        if (settingsDirty && !ImGui.IsAnyItemActive())
        {
            pi.SavePluginConfig(config);
            settingsDirty = false;
        }
    }

    public void Dispose()
    {
        commands.RemoveHandler("/waynecombo");
        pi.UiBuilder.OpenConfigUi -= OpenSettings;
        pi.UiBuilder.Draw -= DrawSettings;
        lifecycle.UnregisterListener(AddonEvent.PreDraw, OnDrawHud);
        lifecycle.UnregisterListener(AddonEvent.PreFinalize, OnFinalizeHud);
        framework.RunOnFrameworkThread(() =>
        {
            foreach (var host in Hosts)
                hud.Remove(gameGui.GetAddonByName<AtkUnitBase>(host));
        }).GetAwaiter().GetResult();
        if (settingsDirty)
            pi.SavePluginConfig(config);
    }
}
