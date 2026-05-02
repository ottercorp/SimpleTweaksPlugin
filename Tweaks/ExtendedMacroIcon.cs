using System;
using System.Runtime.InteropServices;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using SimpleTweaksPlugin.TweakSystem;
using SimpleTweaksPlugin.Utility;

namespace SimpleTweaksPlugin.Tweaks;

[TweakName("Extended Macro Icons")]
[TweakDescription("Allow using specific Icon IDs when using '/macroicon # id' inside of a macro.")]
[TweakReleaseVersion("1.8.3.0")]
public unsafe class ExtendedMacroIcon : Tweak {
    
    [TweakHook(typeof(RaptureMacroModule), nameof(RaptureMacroModule.TryResolveMacroIcon), nameof(SetupMacroIconDetour))]
    private HookWrapper<RaptureMacroModule.Delegates.TryResolveMacroIcon> setupMacroIconHook;

    [TweakHook(typeof(RaptureHotbarModule.HotbarSlot), nameof(RaptureHotbarModule.HotbarSlot.GetIconIdForSlot), nameof(GetIconIdDetour))]
    private HookWrapper<RaptureHotbarModule.HotbarSlot.Delegates.GetIconIdForSlot> getIconIdHook;

    private const RaptureHotbarModule.HotbarSlotType IconCategory = (RaptureHotbarModule.HotbarSlotType)255;

    [StructLayout(LayoutKind.Explicit, Size = 0x120)]
    public struct MacroIconTextCommand {
        [FieldOffset(0x00)] public ushort TextCommandId;
        [FieldOffset(0x08)] public uint Id;
        [FieldOffset(0x0C)] public int Category;
    }

    private bool SetupMacroIconDetour(RaptureMacroModule* macroModule, UIModule* uiModule, RaptureHotbarModule.HotbarSlotType* outCategory, uint* outId, int macroPage, uint macroIndex, uint* a7) {
        try {
            var macro = macroModule->GetMacro((uint)macroPage, macroIndex);
            var shellModule = uiModule->GetRaptureShellModule();
            var result = stackalloc MacroIconTextCommand[1];

            if (shellModule->TryGetMacroIconCommand(macro, result) && result->TextCommandId == 207 && result->Category is 270 or 271 && result->Id > 0) {
                *outCategory = IconCategory;
                *outId = result->Id;
                return true;
            }
        } catch (Exception ex) {
            SimpleLog.Error(ex);
        }

        return setupMacroIconHook.Original(macroModule, uiModule, outCategory, outId, macroPage, macroIndex, a7);
    }

    private int GetIconIdDetour(RaptureHotbarModule.HotbarSlot* a1, RaptureHotbarModule.HotbarSlotType category, uint id) => category == IconCategory ? (int)id : getIconIdHook.Original(a1, category, id);
}
