using System;
using System.Runtime.InteropServices;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SimpleTweaksPlugin.Utility; 

public unsafe partial class UiHelper {
    public static bool Ready;

    public static void Setup(ISigScanner scanner) {
        Ready = true;
    }

    public static IntPtr Alloc(ulong size) {
        return new IntPtr(IMemorySpace.GetUISpace()->Malloc(size, 8UL));
    }

    public static IntPtr Alloc(int size) {
        if (size <= 0) throw new ArgumentException("Allocation size must be positive.");
        return Alloc((ulong) size);
    }
}