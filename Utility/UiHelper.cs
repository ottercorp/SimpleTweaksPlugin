using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SimpleTweaksPlugin.Utility;

public static unsafe partial class UiHelper {
    public static void SetSize(AtkResNode* node, int? width, int? height) {
        if (node == null) return;
        if (width != null && width >= ushort.MinValue && width <= ushort.MaxValue) node->Width = (ushort) width.Value;
        if (height != null && height >= ushort.MinValue && height <= ushort.MaxValue) node->Height = (ushort) height.Value;
        node->DrawFlags |= 0x1;
    }

    public static void SetPosition(AtkResNode* node, float? x, float? y) {
        if (node == null) return;
        if (x != null) node->X = x.Value;
        if (y != null) node->Y = y.Value;
        node->DrawFlags |= 0x1;
    }

    public static void SetPosition(AtkUnitBase* atkUnitBase, float? x, float? y) {
        if (atkUnitBase == null) return;
        if (x >= short.MinValue && x <= short.MaxValue) atkUnitBase->X = (short) x.Value;
        if (y >= short.MinValue && x <= short.MaxValue) atkUnitBase->Y = (short) y.Value;
    }

    public static void SetWindowSize(AtkUnitBase* unitBase, ushort? width, ushort? height) {
        if (width == null || height == null) {
            var size = stackalloc ushort[2];
            unitBase->GetSize(&size[0], &size[1], false);
            width ??= size[0];
            height ??= size[1];
        }

        if (width < 14) throw new Exception("Invalid Width. Must be at least 14");

        var windowNode = unitBase->WindowNode;
        if (windowNode is null) return;

        unitBase->WindowNode->SetWidth(width.Value);
        unitBase->WindowNode->SetHeight(height.Value);

        if (unitBase->WindowHeaderCollisionNode is not null) {
            unitBase->WindowHeaderCollisionNode->SetWidth((ushort) (width - 14));
        }

        unitBase->SetSize(width.Value, height.Value);

        unitBase->WindowNode->Component->UldManager.UpdateDrawNodeList();
        unitBase->UpdateCollisionNodeList(false);
    }

    public static void ExpandNodeList(AtkComponentNode* componentNode, ushort addSize) {
        var newNodeList = ExpandNodeList(componentNode->Component->UldManager.NodeList, componentNode->Component->UldManager.NodeListCount, (ushort) (componentNode->Component->UldManager.NodeListCount + addSize));
        componentNode->Component->UldManager.NodeList = newNodeList;
    }

    public static void ExpandNodeList(AtkUnitBase* atkUnitBase, ushort addSize) {
        var newNodeList = ExpandNodeList(atkUnitBase->UldManager.NodeList, atkUnitBase->UldManager.NodeListCount, (ushort)(atkUnitBase->UldManager.NodeListCount + addSize));
        atkUnitBase->UldManager.NodeList = newNodeList;
    }

    private static AtkResNode** ExpandNodeList(AtkResNode** originalList, ushort originalSize, ushort newSize = 0) {
        if (newSize <= originalSize) newSize = (ushort)(originalSize + 1);
        var oldListPtr = new IntPtr(originalList);
        var newListPtr = Alloc((ulong)((newSize + 1) * 8));
        var clone = new IntPtr[originalSize];
        Marshal.Copy(oldListPtr, clone, 0, originalSize);
        Marshal.Copy(clone, 0, newListPtr, originalSize);
        return (AtkResNode**)(newListPtr);
    }

    public static AtkResNode* CloneNode(AtkResNode* original) {
        var size = original->Type switch
        {
            NodeType.Res => sizeof(AtkResNode),
            NodeType.Image => sizeof(AtkImageNode),
            NodeType.Text => sizeof(AtkTextNode),
            NodeType.NineGrid => sizeof(AtkNineGridNode),
            NodeType.Counter => sizeof(AtkCounterNode),
            NodeType.Collision => sizeof(AtkCollisionNode),
            _ => throw new Exception($"Unsupported Type: {original->Type}")
        };

        var allocation = Alloc((ulong)size);
        var bytes = new byte[size];
        Marshal.Copy(new IntPtr(original), bytes, 0, bytes.Length);
        Marshal.Copy(bytes, 0, allocation, bytes.Length);

        var newNode = (AtkResNode*)allocation;
        newNode->ParentNode = null;
        newNode->ChildNode = null;
        newNode->ChildCount = 0;
        newNode->PrevSiblingNode = null;
        newNode->NextSiblingNode = null;
        return newNode;
    }

    public static void Close(AtkUnitBase* atkUnitBase, bool unknownBool = false) {
        if (!Ready) return;
        _atkUnitBaseClose(atkUnitBase, (byte) (unknownBool ? 1 : 0));
    }
}