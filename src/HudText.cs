using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WayneComboTimer;

// Native HUD layout and styling adapted from Simple Tweaks' Combo Timer (AGPL-3.0).
internal sealed unsafe class HudText
{
    private const uint NodeId = 0x57544354;
    private string? lastText;

    internal void Draw(AtkUnitBase* addon, Configuration config, float? seconds, bool showIdle)
    {
        if (addon == null || addon->RootNode == null)
            return;

        var text = (AtkTextNode*)FindNode(addon);
        if (seconds == null && !showIdle)
        {
            if (text != null)
                text->AtkResNode.ToggleVisibility(false);
            return;
        }
        if (text == null)
        {
            text = Create(addon);
            if (text == null)
                return;
            lastText = null;
        }

        ref var node = ref text->AtkResNode;
        node.ToggleVisibility(true);
        node.SetPositionFloat(-45f + config.OffsetX, 15f + config.OffsetY);
        text->FontSize = text->LineSpacing = (byte)Math.Clamp(config.FontSize, 6, 255);
        var c = Vector4.Clamp(config.Color, Vector4.Zero, Vector4.One) * 255f;
        var e = Vector4.Clamp(config.EdgeColor, Vector4.Zero, Vector4.One) * 255f;
        text->TextColor.R = (byte)c.X; text->TextColor.G = (byte)c.Y;
        text->TextColor.B = (byte)c.Z; text->TextColor.A = (byte)c.W;
        text->EdgeColor.R = (byte)e.X; text->EdgeColor.G = (byte)e.Y;
        text->EdgeColor.B = (byte)e.Z; text->EdgeColor.A = (byte)e.W;
        var places = Math.Clamp(config.DecimalPlaces, 0, 4);
        var format = (config.LeadingZero ? "00" : "0") + (places > 0 ? "." + new string('0', places) : "");
        var display = (seconds ?? 0f).ToString(format);
        if (!config.NoComboText)
            display = "Combo\n" + display;
        if (display != lastText)
        {
            text->SetText(display);
            lastText = display;
        }
    }

    private static AtkResNode* FindNode(AtkUnitBase* addon)
    {
        for (int i = 0; i < addon->UldManager.NodeListCount; i++)
        {
            var node = addon->UldManager.NodeList[i];
            if (node != null && node->NodeId == NodeId && node->Type == NodeType.Text)
                return node;
        }
        return null;
    }

    private static AtkTextNode* Create(AtkUnitBase* addon)
    {
        var text = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
        if (text == null)
            return null;
        var node = &text->AtkResNode;
        node->Type = NodeType.Text;
        node->NodeId = NodeId;
        node->NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
        node->SetWidth(200);
        node->SetHeight(14);
        node->Color.R = node->Color.G = node->Color.B = node->Color.A = 255;
        text->AlignmentFontType = 0x14;
        text->CharSpacing = 1;
        text->TextFlags = TextFlags.Edge | TextFlags.MultiLine;
        var root = addon->RootNode;
        node->ParentNode = root;
        var sibling = root->ChildNode;
        if (sibling == null)
            root->ChildNode = node;
        else
        {
            while (sibling->PrevSiblingNode != null)
                sibling = sibling->PrevSiblingNode;
            node->NextSiblingNode = sibling;
            sibling->PrevSiblingNode = node;
        }
        addon->UldManager.UpdateDrawNodeList();
        return text;
    }

    internal void Remove(AtkUnitBase* addon)
    {
        if (addon == null)
            return;
        var node = FindNode(addon);
        if (node == null || node->ParentNode == null || node->ParentNode != addon->RootNode)
            return;
        if (node->ParentNode->ChildNode == node)
            node->ParentNode->ChildNode = node->PrevSiblingNode;
        if (node->NextSiblingNode != null)
            node->NextSiblingNode->PrevSiblingNode = node->PrevSiblingNode;
        if (node->PrevSiblingNode != null)
            node->PrevSiblingNode->NextSiblingNode = node->NextSiblingNode;
        node->PrevSiblingNode = node->NextSiblingNode = node->ParentNode = null;
        addon->UldManager.UpdateDrawNodeList();
        node->Destroy(false);
        IMemorySpace.Free(node, (ulong)sizeof(AtkTextNode));
        lastText = null;
    }
}
