using System.Diagnostics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WayneComboTimer;

public sealed unsafe class Plugin : IDalamudPlugin
{
    private static readonly string[] Hosts = ["_ParameterWidget", "_ScreenText", "_EnemyList",
        "_TargetInfo", "_TargetInfoMainTarget", "_TargetInfoBuffDebuff", "_FocusTargetInfo"];
    private readonly IDalamudPluginInterface pi;
    private readonly IAddonLifecycle lifecycle;
    private readonly IGameGui gui;
    private readonly IObjectTable objects;
    private readonly ITargetManager targets;
    private readonly ICondition conditions;
    private readonly IClientState client;
    private readonly ICommandManager commands;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly Configuration config;
    private readonly TimerLogic timer;
    private readonly HudText hud = new();
    private readonly PersonalDebuffs personal = new();
    private readonly EnemyStatuses statuses;
    private readonly DistanceDisplay distances;
    private readonly StatisticsDisplay statistics;
    private readonly SettingsUi settings;
    private readonly bool[] originalLoaded = new bool[7];
    private bool dirty;
    private long lastSave;
    private long lastSourceCheck;
    private long lastError;

    public Plugin(IDalamudPluginInterface pi, IDataManager data, IObjectTable objects, IClientState client,
        ICondition conditions, IGameGui gameGui, IAddonLifecycle lifecycle, IFramework framework,
        ICommandManager commands, IPluginLog log, ITargetManager targets, IDtrBar dtr)
    {
        this.pi = pi; this.objects = objects; this.client = client; this.conditions = conditions;
        gui = gameGui; this.lifecycle = lifecycle; this.framework = framework; this.commands = commands;
        this.log = log; this.targets = targets;
        config = Configuration.Load(pi, log);
        timer = new(data);
        statuses = new(data);
        distances = new(gui, targets, objects);
        settings = new(pi, config, log, i => originalLoaded[i], MarkChanged);
        statistics = new(dtr, pi, settings.Open, MarkChanged);
        CheckSources();
        lifecycle.RegisterListener(AddonEvent.PreDraw, Hosts, OnDrawHud);
        lifecycle.RegisterListener(AddonEvent.PreFinalize, Hosts, OnFinalizeHud);
        framework.Update += OnFramework;
        pi.UiBuilder.Draw += Draw;
        pi.UiBuilder.OpenConfigUi += OpenSettings;
        pi.UiBuilder.OpenMainUi += OpenSettings;
        commands.AddHandler("/waynecombo", new CommandInfo((_, _) => OpenSettings())
            { HelpMessage = "開啟 Combo Timer 整合設定。" });
    }

    private void MarkChanged() => dirty = true;
    private void OpenSettings() => settings.Open();
    private bool Active(int module) => config.Modules[module] && !originalLoaded[module];

    private void CheckSources()
    {
        var plugins = pi.InstalledPlugins.ToArray();
        for (int i = 1; i < originalLoaded.Length; i++)
            originalLoaded[i] = plugins.Any(p => p.IsLoaded && p.InternalName == SettingsUi.Sources[i]);
        lastSourceCheck = Stopwatch.GetTimestamp();
    }

    private void OnFramework(IFramework _)
    {
        try
        {
            if (Stopwatch.GetElapsedTime(lastSourceCheck).TotalSeconds > 1) CheckSources();
            // Read game objects only on the framework thread; the worker receives just the DC ID.
            uint dataCenterId = objects.LocalPlayer?.CurrentWorld.ValueNullable?.DataCenter.RowId ?? 0u;
            statistics.Update(Active(5), Active(6) && client.IsLoggedIn, config.Fps, config.Ping, dataCenterId);
            var ping = statistics.Monitor.Current;
            settings.PingStatus = string.IsNullOrEmpty(ping.Address) ? ping.Error ?? "正在取得測量位址…" :
                (ping.UsingDataCenter ? "使用資料中心備援位址（連線經過轉接時，可能與實際遊戲連線延遲不同）" :
                    "使用遊戲連線位址") + "\n" + ping.Address +
                (ping.Error is null ? "" : "\n" + ping.Error);
        }
        catch (Exception e) { Report(e); }
    }

    private void OnDrawHud(AddonEvent _, AddonArgs args)
    {
        try
        {
            var addon = (AtkUnitBase*)args.Addon.Address;
            var player = objects.LocalPlayer;
            bool combat = conditions[ConditionFlag.InCombat];
            bool pve = !client.IsPvP;
            if (args.AddonName is "_ParameterWidget" or "_ScreenText")
            {
                if (args.AddonName == (config.UseScreenText ? "_ScreenText" : "_ParameterWidget"))
                {
                    bool show = Active(0) && player != null && !(config.UseScreenText && Cutscene());
                    bool viper = player?.ClassJob.RowId == TimerLogic.ViperJob && pve;
                    hud.Draw(addon, config, show ? timer.Remaining(player!, viper) : null,
                        show && config.AlwaysVisible);
                }
                else hud.Remove(addon);
            }
            if (args.AddonName == "_EnemyList")
                statuses.Update((AddonEnemyList*)addon, Active(2) && pve, player?.EntityId ?? 0, config.EnemyDebuffs);
            if (args.AddonName is "_TargetInfo" or "_TargetInfoBuffDebuff")
                personal.Update(addon, Active(4) && pve, targets.Target as Dalamud.Game.ClientState.Objects.Types.IBattleChara,
                    player?.EntityId ?? 0, config.PersonalDebuffs.Scale);
            if (DistanceDisplay.Hosts.Contains(args.AddonName))
                distances.Update(addon, args.AddonName, Active(3) && pve && !Cutscene(), combat, config.Distance);
        }
        catch (Exception e) { Report(e); }
    }

    private void Draw()
    {
        try
        {
            settings.Draw();
            if (Active(1) && conditions[ConditionFlag.InCombat] && !client.IsPvP)
                EnemyHp.Draw(gui.GetAddonByName<AtkUnitBase>("_EnemyList"), config.EnemyHp);
            statistics.Draw(Active(6) && client.IsLoggedIn, Cutscene(), config.Ping);
            if (dirty && !ImGui.IsAnyItemActive() && Stopwatch.GetElapsedTime(lastSave).TotalMilliseconds > 500)
            {
                pi.SavePluginConfig(config);
                lastSave = Stopwatch.GetTimestamp();
                dirty = false;
            }
        }
        catch (Exception e) { Report(e); }
    }

    private bool Cutscene() => conditions[ConditionFlag.WatchingCutscene] ||
        conditions[ConditionFlag.WatchingCutscene78] || conditions[ConditionFlag.OccupiedInCutSceneEvent];

    private void OnFinalizeHud(AddonEvent _, AddonArgs args) => Cleanup((AtkUnitBase*)args.Addon.Address, args.AddonName);

    private void Cleanup(AtkUnitBase* addon, string name)
    {
        if (addon == null) return;
        if (name is "_ParameterWidget" or "_ScreenText") hud.Remove(addon);
        if (name == "_EnemyList") statuses.Release((AddonEnemyList*)addon);
        if (name is "_TargetInfo" or "_TargetInfoBuffDebuff") personal.Restore(addon);
        if (DistanceDisplay.Hosts.Contains(name)) distances.Release(addon);
    }

    private void Report(Exception error)
    {
        if (Stopwatch.GetElapsedTime(lastError).TotalSeconds < 10) return;
        lastError = Stopwatch.GetTimestamp();
        log.Error(error, "Combo Timer integrated display update failed.");
    }

    public void Dispose()
    {
        commands.RemoveHandler("/waynecombo");
        pi.UiBuilder.OpenConfigUi -= OpenSettings;
        pi.UiBuilder.OpenMainUi -= OpenSettings;
        pi.UiBuilder.Draw -= Draw;
        framework.Update -= OnFramework;
        lifecycle.UnregisterListener(AddonEvent.PreDraw, OnDrawHud);
        lifecycle.UnregisterListener(AddonEvent.PreFinalize, OnFinalizeHud);
        framework.RunOnFrameworkThread(() =>
        {
            foreach (var name in Hosts) Cleanup(gui.GetAddonByName<AtkUnitBase>(name), name);
            statistics.Dispose();
        }).GetAwaiter().GetResult();
        settings.Dispose();
        if (dirty) pi.SavePluginConfig(config);
    }
}
