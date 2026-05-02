using System.Linq;
using Dalamud;
using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin.Tweaks.Chat;

[TweakName("Sticky Shout Chat")]
[TweakDescription("Prevents the game from automatically switching out of shout chat.")]
[TweakReleaseVersion("1.9.6.0")]
public class StickyShoutChat : ChatTweaks.SubTweak {
    [Signature("05 75 0C 8B D7 E8 ?? ?? ?? ?? E9", ScanType = ScanType.Text)]
    private nint editAddress;

    protected override void Enable() {
        Service.Chat.ChatMessage -= ChatOnCheckMessageHandled;
        Service.Chat.ChatMessage += ChatOnCheckMessageHandled;
        SafeMemory.Write(editAddress, (sbyte)-2);
    }

    private readonly string[] errorMessages = [
        "“/shout” requires a valid string.",
        "“/shout ” requires a valid string.",
    ];
    
    private unsafe void ChatOnCheckMessageHandled(IHandleableChatMessage chatMessage) {
        if (chatMessage.LogKind != XivChatType.ErrorMessage) return;
        var text = chatMessage.Message.TextValue;
        if (!errorMessages.Any(m => m.Equals(text))) return;
        RaptureShellModule.Instance()->ChangeChatChannel(5, 0, Utf8String.FromString(""), true);
        chatMessage.PreventOriginal();
    }

    protected override void Disable() {
        Service.Chat.ChatMessage -= ChatOnCheckMessageHandled;
        if (editAddress == nint.Zero) return;
        if (SafeMemory.Write(editAddress, (sbyte)5))
            editAddress = nint.Zero;
    }
}
