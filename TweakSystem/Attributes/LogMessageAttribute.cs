using SimpleTweaksPlugin.Events;

namespace SimpleTweaksPlugin.TweakSystem;

public class LogMessageAttribute(params uint[] logMessageIds) : EventAttribute {
    public uint[] LogMessageIds { get; } = logMessageIds;
}
