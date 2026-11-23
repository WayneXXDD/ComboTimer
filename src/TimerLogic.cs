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
        if (viperPve)
        {
            foreach (var status in player.StatusList)
            {
                // The enhancement keeps its duration while the hotbar shows later combo steps.
                var remaining = ViperStarter.Remaining(status.StatusId, status.RemainingTime);
                if (remaining.HasValue)
                    return remaining;
            }
            return null;
        }

        var actions = ActionManager.Instance();
        if (actions == null)
            return null;

        var combo = actions->Combo;
        if (combo.Timer <= 0 || combo.Action == 0)
            return null;
        nextLevels ??= BuildNextActionLevels();
        return ComboActions.HasNext(combo.Action, player.Level, nextLevels) ? combo.Timer : null;
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
