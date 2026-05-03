using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SimpleTweaksPlugin.Events;
using SimpleTweaksPlugin.TweakSystem;
using SimpleTweaksPlugin.Utility;

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment;

[TweakName("Cleaner World Visit Menu")]
[TweakDescription("Cleans up the world visit menu and shows your current location in order on the list.")]
public unsafe class ImprovedWorldVisit : Tweak {
    [AddonPostSetup("WorldTravelSelect")]
    [AddonPostRequestedUpdate("WorldTravelSelect")]
    private void SetupWorldTravelSelect(AtkUnitBase* unitBase) {
        if (Service.KeyState[VirtualKey.SHIFT]) return;
        SimpleLog.Log("Rebuild World Visit Menu");
        
        var headerNode = unitBase->GetTextNodeById(19);
        var list = unitBase->GetComponentListById(20);

        if (headerNode == null || list == null || list->OwnerNode == null) return;

        for (uint i = 2; i <= 22; i++) {
            if (i == headerNode->NodeId || i == list->OwnerNode->NodeId) continue;
            var n = unitBase->GetNodeById(i);
            if (n != null) n->ToggleVisibility(false);
        }

        headerNode->SetPositionShort(16, 44);
        list->OwnerNode->SetPositionShort(23, 68);
        
        UiHelper.SetWindowSize(unitBase, (ushort)(list->OwnerNode->GetXShort() * 2 + list->OwnerNode->GetWidth()), (ushort)(list->OwnerNode->GetYShort() + list->OwnerNode->GetXShort() + 216));
    }
}
