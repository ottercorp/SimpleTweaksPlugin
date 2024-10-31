// using SimpleTweaksPlugin.TweakSystem;
// using SimpleTweaksPlugin.Utility;

// namespace SimpleTweaksPlugin.Tweaks.UiAdjustment;

// [TweakName("真实排队顺序")]
// [TweakDescription("显示组队查找器和跨服等界面的真实等待顺序.")]
// [TweakAuthor("Ariiisu")]
// //https://github.com/Ariiisu/AccurateWorldTravelQueue/blob/main/AccurateWorldTravelQueue/Memory.cs
// public class RealPFQueue : UiAdjustments.SubTweak
// {
//     const string CheckAddress = "83 F8 ?? 73 ?? 44 8B C0 1B D2";
//     const string AddonTextAddress = "81 C2 F5 ?? ?? ?? E8 ?? ?? ?? ?? 48 8B D0 48 8D 8C 24";

//     static readonly byte?[] PatchCheckBytes = { 0x90, 0x90, 0x90, 0x90, 0x90 };
//     static readonly byte?[] PatchAddonBytes = { 0xF4, 0x30 };
    
//     MemoryPatch patch1 = new MemoryPatch(CheckAddress, PatchCheckBytes, false);
//     MemoryPatch patch2 = new MemoryPatch(AddonTextAddress, PatchAddonBytes, false);
    
//     protected override void Enable()
//     {
//         patch1.Enable();
//         patch2.Enable();
//         base.Enable();
//     }

//     protected override void Disable()
//     {
//         patch1.Disable();
//         patch2.Disable();
//         base.Disable();
//     }
// }