// Status icon layout is adapted from Limiana/EnemyListDebuffs under the MIT license.
// Allocation, lifetime and integration with the combined plugin are implemented here.
using System.Diagnostics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Plugin.Services;
using StatusSheet = Lumina.Excel.Sheets.Status;

namespace WayneComboTimer;

internal sealed unsafe class EnemyStatuses(IDataManager data)
{
    private readonly Group?[] groups = new Group?[8];
    private long lastUpdate;

    internal void Update(AddonEnemyList* addon, bool enabled, uint playerId, EnemyDebuffSettings settings)
    {
        if (addon == null) return;
        if (!enabled || playerId == 0)
        {
            Release(addon);
            return;
        }
        if (Stopwatch.GetElapsedTime(lastUpdate).TotalMilliseconds < Math.Clamp(settings.UpdateInterval, 16, 5000))
            return;
        lastUpdate = Stopwatch.GetTimestamp();
        var stage = AtkStage.Instance();
        if (stage == null) return;
        var numbers = stage->GetNumberArrayData(NumberArrayType.EnemyList);
        if (numbers == null || numbers->IntArray == null) return;

        for (int i = 0; i < 8; i++)
        {
            var button = *(&addon->EnemyOneComponent)[i];
            if (button == null || button->UldManager.RootNode == null) continue;
            if (groups[i] == null)
                groups[i] = Group.Create(&button->UldManager, (uint)(0x57545000 + i * 16));
            var group = groups[i];
            if (group == null) continue;
            group.Hide();
            if (i >= addon->EnemyCount) continue;
            uint enemyId = (uint)numbers->IntArray[8 + i * 6];
            var character = CharacterManager.Instance()->LookupBattleCharaByEntityId(enemyId);
            if (character == null) continue;
            var statuses = character->GetStatusManager();
            if (statuses == null) continue;
            group.Layout(settings);
            var count = 0;
            for (int j = 0; j < statuses->Status.Length && count < 4; j++)
            {
                ref var status = ref statuses->Status[j];
                if (status.StatusId == 0 || status.SourceObject.ObjectId != playerId) continue;
                var row = data.GetExcelSheet<StatusSheet>().GetRowOrDefault(status.StatusId);
                if (!row.HasValue || row.Value.Icon == 0) continue;
                group.Show(count++, row.Value.Icon, (int)MathF.Abs(status.RemainingTime));
            }
        }
    }

    internal void Release(AddonEnemyList* addon)
    {
        if (addon == null) return;
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i]?.Dispose();
            groups[i] = null;
        }
        lastUpdate = 0;
    }

    private sealed class Group
    {
        private nint root;
        private nint owner;
        private readonly nint[] icons = new nint[4];
        private readonly nint[] timers = new nint[4];
        private readonly uint[] iconIds = new uint[4];

        internal static Group? Create(AtkUldManager* uld, uint id)
        {
            var group = new Group { owner = (nint)uld };
            var node = IMemorySpace.GetUISpace()->Create<AtkResNode>();
            if (node == null) return null;
            group.root = (nint)node;
            node->NodeId = id;
            node->Type = NodeType.Res;
            node->NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
            for (int i = 0; i < 4; i++)
            {
                var icon = CreateIcon(id + (uint)(1 + i * 2));
                var timer = NativeNodes.Text(id + (uint)(2 + i * 2));
                group.icons[i] = (nint)icon;
                group.timers[i] = (nint)timer;
                if (icon == null || timer == null)
                {
                    group.Dispose();
                    return null;
                }
                timer->AlignmentFontType = 4;
                timer->TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize;
                timer->SetNumber(20);
                NativeNodes.Append(node, (AtkResNode*)icon);
                NativeNodes.Append(node, (AtkResNode*)timer);
            }
            node->ChildCount = 8;
            var last = uld->RootNode;
            while (last->PrevSiblingNode != null) last = last->PrevSiblingNode;
            last->PrevSiblingNode = node;
            node->NextSiblingNode = last;
            node->ParentNode = uld->RootNode;
            uld->UpdateDrawNodeList();
            return group;
        }

        internal void Layout(EnemyDebuffSettings settings)
        {
            var parent = (AtkResNode*)root;
            var scale = Math.Clamp(settings.Scale, .1f, 5);
            parent->SetPositionFloat(settings.GroupX, settings.GroupY);
            parent->SetScale(scale, scale);
            parent->ToggleVisibility(true);
            var sample = (AtkTextNode*)timers[0];
            sample->FontSize = (byte)Math.Clamp(settings.FontSize, 6, 80);
            sample->SetNumber(20);
            ushort textWidth = 0, textHeight = 0;
            sample->GetTextDrawSize(&textWidth, &textHeight);
            var width = Math.Max(Math.Clamp(settings.IconWidth, 1, 128),
                textWidth + 2 * Math.Clamp(settings.DurationPadding, 0, 30));
            parent->SetWidth((ushort)Math.Clamp(width * 4 + settings.NodeSpacing * 3, 1, 65535));
            parent->SetHeight((ushort)Math.Clamp(Math.Max(settings.IconY + settings.IconHeight,
                settings.DurationY + textHeight + settings.DurationPadding * 2), 1, 65535));
            for (int i = 0; i < 4; i++)
            {
                var x = (settings.FillFromRight ? 3 - i : i) * (width + settings.NodeSpacing);
                var icon = (AtkImageNode*)icons[i];
                var timer = (AtkTextNode*)timers[i];
                icon->SetPositionFloat(x + settings.IconX, settings.IconY);
                icon->SetWidth((ushort)Math.Clamp(settings.IconWidth, 1, 128));
                icon->SetHeight((ushort)Math.Clamp(settings.IconHeight, 1, 128));
                timer->SetPositionFloat(x + settings.DurationX, settings.DurationY);
                timer->FontSize = (byte)Math.Clamp(settings.FontSize, 6, 80);
                timer->SetWidth((ushort)width);
                timer->SetHeight((ushort)(textHeight + 2 * Math.Clamp(settings.DurationPadding, 0, 30)));
                NativeNodes.Colors(timer, settings.DurationTextColor, settings.DurationEdgeColor);
            }
        }

        internal void Hide()
        {
            for (int i = 0; i < 4; i++)
            {
                if (icons[i] != 0) ((AtkResNode*)icons[i])->ToggleVisibility(false);
                if (timers[i] != 0) ((AtkResNode*)timers[i])->ToggleVisibility(false);
            }
        }

        internal void Show(int index, uint iconId, int seconds)
        {
            var icon = (AtkImageNode*)icons[index];
            var timer = (AtkTextNode*)timers[index];
            if (iconIds[index] != iconId)
            {
                icon->LoadIconTexture(iconId, 0);
                iconIds[index] = iconId;
            }
            timer->SetNumber(seconds);
            icon->ToggleVisibility(true);
            timer->ToggleVisibility(true);
        }

        internal void Dispose()
        {
            if (root == 0) return;
            NativeNodes.Unlink((AtkResNode*)root);
            if (owner != 0) ((AtkUldManager*)owner)->UpdateDrawNodeList();
            for (int i = 0; i < 4; i++)
            {
                if (icons[i] != 0)
                {
                    var icon = (AtkImageNode*)icons[i];
                    NativeNodes.Unlink((AtkResNode*)icon);
                    icon->UnloadTexture();
                    var parts = icon->PartsList;
                    if (parts != null)
                    {
                        if (parts->Parts != null)
                        {
                            if (parts->Parts->UldAsset != null)
                                IMemorySpace.Free(parts->Parts->UldAsset, (ulong)sizeof(AtkUldAsset));
                            IMemorySpace.Free(parts->Parts, (ulong)sizeof(AtkUldPart));
                        }
                        IMemorySpace.Free(parts, (ulong)sizeof(AtkUldPartsList));
                        icon->PartsList = null;
                    }
                    icon->Destroy(false);
                    IMemorySpace.Free(icon, (ulong)sizeof(AtkImageNode));
                    icons[i] = 0;
                }
                if (timers[i] != 0)
                {
                    var timer = (AtkTextNode*)timers[i];
                    NativeNodes.Unlink((AtkResNode*)timer);
                    timer->Destroy(false);
                    IMemorySpace.Free(timer, (ulong)sizeof(AtkTextNode));
                    timers[i] = 0;
                }
            }
            var node = (AtkResNode*)root;
            node->Destroy(false);
            IMemorySpace.Free(node, (ulong)sizeof(AtkResNode));
            root = owner = 0;
        }

        private static AtkImageNode* CreateIcon(uint id)
        {
            var icon = IMemorySpace.GetUISpace()->Create<AtkImageNode>();
            if (icon == null) return null;
            var parts = (AtkUldPartsList*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPartsList), 8);
            var part = (AtkUldPart*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPart), 8);
            var asset = (AtkUldAsset*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldAsset), 8);
            if (parts == null || part == null || asset == null)
            {
                if (parts != null) IMemorySpace.Free(parts, (ulong)sizeof(AtkUldPartsList));
                if (part != null) IMemorySpace.Free(part, (ulong)sizeof(AtkUldPart));
                if (asset != null) IMemorySpace.Free(asset, (ulong)sizeof(AtkUldAsset));
                icon->Destroy(false);
                IMemorySpace.Free(icon, (ulong)sizeof(AtkImageNode));
                return null;
            }
            *parts = default; *part = default; *asset = default;
            asset->AtkTexture.Ctor();
            part->Width = 24; part->Height = 32; part->UldAsset = asset;
            parts->Parts = part; parts->PartCount = 1;
            icon->PartsList = parts;
            icon->NodeId = id;
            icon->Type = NodeType.Image;
            icon->NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
            icon->Color.R = icon->Color.G = icon->Color.B = icon->Color.A = 255;
            icon->WrapMode = 1;
            icon->Flags = ImageNodeFlags.AutoFit;
            return icon;
        }
    }
}
