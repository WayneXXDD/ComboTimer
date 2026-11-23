using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using SheetAction = Lumina.Excel.Sheets.Action;

namespace WayneComboTimer;

internal sealed unsafe class TimerLogic(IDataManager data)
{
    internal const uint ViperJob = 41;

    private Dictionary<uint, byte>? nextLevels;

    internal float? Remaining(IBattleChara player, bool viperPve)
    {
        var actions = ActionManager.Instance();
        if (actions == null)
            return null;

        if (viperPve)
        {
            foreach (var status in player.StatusList)
            {
                var starter = ViperStarter.FromStatus(status.StatusId);
                if (starter == 0 || !(status.RemainingTime > 0))
                    continue;

                // The same slots also hold second/third steps and Reawaken actions.
                // Check the effective action and its native highlight before showing its buff.
                var effective = actions->GetAdjustedActionId(starter);
                var highlighted = effective == starter && actions->IsActionHighlighted(ActionType.Action, starter);
                var remaining = ViperStarter.Remaining(starter, effective, highlighted, status.RemainingTime);
                if (remaining.HasValue)
                    return remaining;
            }
            return null;
        }

        var combo = actions->Combo;
        if (combo.Timer <= 0 || combo.Action == 0)
            return null;
        nextLevels ??= BuildNextActionLevels();
        return nextLevels.TryGetValue(combo.Action, out var level) && player.Level >= level ? combo.Timer : null;
    }

    private Dictionary<uint, byte> BuildNextActionLevels()
    {
        var result = new Dictionary<uint, byte>();
        foreach (var action in data.GetExcelSheet<SheetAction>())
        {
            var previous = action.ActionCombo.RowId;
            if (previous != 0 && (!result.TryGetValue(previous, out var level) || action.ClassJobLevel < level))
                result[previous] = action.ClassJobLevel;
        }

        // Simple Tweaks' exceptions for Wheeling Thrust and Verholy.
        result[3556] = 64;
        result[7526] = 80;
        return result;
    }
}
