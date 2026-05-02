using System;
using JetBrains.Annotations;

namespace SimpleTweaksPlugin.TweakSystem; 

[AttributeUsage(AttributeTargets.Field)]
[MeansImplicitUse(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
public class TweakHookAttribute : Attribute {
    public bool AutoEnable { get; init; } = true;
    public Type AddressType { get; init; }
    public string AddressName { get; init; } = string.Empty;
    public string DetourName { get; init; } = string.Empty;

    public bool VirtualFunction { get; init; } = false;
    
    public TweakHookAttribute() { }

    public TweakHookAttribute(Type type, string function, string detourName) {
        AddressType = type;
        AddressName = function;
        DetourName = detourName;
    }
    
    public TweakHookAttribute(Type type, bool virtualFunction, string function, string detourName) {
        AddressType = type;
        AddressName = function;
        DetourName = detourName;
        VirtualFunction = virtualFunction;
    }
}
