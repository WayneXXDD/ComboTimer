using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WayneComboTimer;

internal sealed unsafe class PersonalDebuffs
{
    private readonly Dictionary<nint, List<NodeState>> originals = [];
    private readonly record struct NodeState(nint Address, uint Id, float X, float Y, float ScaleX, float ScaleY);

    internal void Update(AtkUnitBase* addon, bool enabled, IBattleChara? target, uint playerId, float scale)
    {
        if (addon == null) return;
        if (!enabled || target == null || playerId == 0)
        {
            Restore(addon);
            return;
        }
        var split = addon->UldManager.NodeListCount < 53;
        var first = split ? 31 : 32;
        var second = split ? 16 : 17;
        if (addon->UldManager.NodeListCount <= first) return;
        int own = Math.Min(15, target.StatusList.Count(status => status.SourceId == playerId && status.StatusId != 0));
        var key = (nint)addon;
        if (!originals.TryGetValue(key, out var saved))
        {
            saved = [];
            for (var i = first; i >= second - 14; i--)
            {
                var node = addon->UldManager.NodeList[i];
                if (node != null)
                    saved.Add(new((nint)node, node->NodeId, node->X, node->Y, node->ScaleX, node->ScaleY));
            }
            originals.Add(key, saved);
        }
        scale = Math.Clamp(scale, .5f, 4);
        for (var i = 0; i < 15; i++)
        {
            var node = addon->UldManager.NodeList[first - i];
            if (node == null) continue;
            var enlarged = i < own;
            node->SetScale(enlarged ? scale : 1, enlarged ? scale : 1);
            node->SetPositionFloat(i * 25 + Math.Min(i, own) * (int)((scale - 1) * 25),
                enlarged ? -(int)(41 * (scale - 1) / 4.5f) : 0);
        }
        for (var i = second; i >= second - 14; i--)
        {
            var node = addon->UldManager.NodeList[i];
            if (node != null) node->SetPositionFloat(node->X, own > 0 ? (int)(scale * 41) : 41);
        }
    }

    internal void Restore(AtkUnitBase* addon)
    {
        if (addon == null || !originals.Remove((nint)addon, out var saved)) return;
        // Match both the pointer and node ID inside the live addon before restoring.
        foreach (var item in saved)
        {
            for (int i = 0; i < addon->UldManager.NodeListCount; i++)
            {
                var node = addon->UldManager.NodeList[i];
                if ((nint)node != item.Address || node == null || node->NodeId != item.Id) continue;
                node->SetPositionFloat(item.X, item.Y);
                node->SetScale(item.ScaleX, item.ScaleY);
                break;
            }
        }
    }
}
