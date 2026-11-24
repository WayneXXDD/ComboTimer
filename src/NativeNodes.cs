using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WayneComboTimer;

internal static unsafe class NativeNodes
{
    internal static AtkTextNode* Text(uint id)
    {
        var text = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
        if (text == null) return null;
        text->NodeId = id;
        text->Type = NodeType.Text;
        text->NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
        text->SetWidth(200);
        text->SetHeight(24);
        text->Color.R = text->Color.G = text->Color.B = text->Color.A = 255;
        text->TextFlags = TextFlags.Edge;
        text->LineSpacing = 24;
        text->CharSpacing = 1;
        return text;
    }

    internal static AtkResNode* Find(AtkUnitBase* addon, uint id)
    {
        if (addon == null) return null;
        for (int i = 0; i < addon->UldManager.NodeListCount; i++)
        {
            var node = addon->UldManager.NodeList[i];
            if (node != null && node->NodeId == id) return node;
        }
        return null;
    }

    internal static void Append(AtkResNode* parent, AtkResNode* node)
    {
        node->ParentNode = parent;
        var child = parent->ChildNode;
        if (child == null) parent->ChildNode = node;
        else
        {
            while (child->PrevSiblingNode != null) child = child->PrevSiblingNode;
            child->PrevSiblingNode = node;
            node->NextSiblingNode = child;
        }
    }

    internal static void Unlink(AtkResNode* node)
    {
        if (node->ParentNode != null && node->ParentNode->ChildNode == node)
            node->ParentNode->ChildNode = node->PrevSiblingNode;
        if (node->NextSiblingNode != null) node->NextSiblingNode->PrevSiblingNode = node->PrevSiblingNode;
        if (node->PrevSiblingNode != null) node->PrevSiblingNode->NextSiblingNode = node->NextSiblingNode;
        node->NextSiblingNode = node->PrevSiblingNode = node->ParentNode = null;
    }

    internal static void RemoveText(AtkUnitBase* addon, uint id)
    {
        var node = Find(addon, id);
        if (node == null || node->Type != NodeType.Text) return;
        Unlink(node);
        addon->UldManager.UpdateDrawNodeList();
        node->Destroy(false);
        IMemorySpace.Free(node, (ulong)sizeof(AtkTextNode));
    }

    internal static void Colors(AtkTextNode* node, Vector4 text, Vector4 edge)
    {
        text = Vector4.Clamp(text, Vector4.Zero, Vector4.One) * 255;
        edge = Vector4.Clamp(edge, Vector4.Zero, Vector4.One) * 255;
        node->TextColor.R = (byte)text.X; node->TextColor.G = (byte)text.Y;
        node->TextColor.B = (byte)text.Z; node->TextColor.A = (byte)text.W;
        node->EdgeColor.R = (byte)edge.X; node->EdgeColor.G = (byte)edge.Y;
        node->EdgeColor.B = (byte)edge.Z; node->EdgeColor.A = (byte)edge.W;
    }
}
