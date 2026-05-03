using System;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using SimpleTweaksPlugin.TweakSystem;
using SimpleTweaksPlugin.Utility;

namespace SimpleTweaksPlugin.Tweaks;

[TweakName("Open loot window when items are added")]
[TweakDescription("Open the loot rolling window when new items are added to be rolled on.")]
public unsafe class AutoOpenLootWindow : Tweak {
    private string castYourLot = "Cast your lot.";
    
    protected override void Enable() {
        Service.Chat.ChatMessage += HandleChat;
        castYourLot = Service.Data.GetExcelSheet<Addon>(Service.ClientState.ClientLanguage).GetRow(5194).Text.ExtractText();
    }
    
    private void HandleChat(IHandleableChatMessage message) {
        try {
            if ((ushort)message.LogKind != 2105) return;
            if (message.Message.TextValue.Equals(castYourLot)) {
                if (Service.Condition.Cutscene()) {
                    Common.FrameworkUpdate -= TryOpenAfterCutsceneFrameworkUpdate;
                    Common.FrameworkUpdate += TryOpenAfterCutsceneFrameworkUpdate;
                } else {
                    TryOpenWindow();
                }
            }
        } catch (Exception ex) {
            SimpleLog.Error(ex);
        }
    }

    private byte throttle;

    private void TryOpenAfterCutsceneFrameworkUpdate() {
        throttle++;
        if (throttle <= 10) return;
        throttle = 0;
        if (Service.Condition[ConditionFlag.WatchingCutscene] || Service.Condition[ConditionFlag.WatchingCutscene78] || Service.Condition[ConditionFlag.OccupiedInCutSceneEvent]) {
            return;
        }

        Common.FrameworkUpdate -= TryOpenAfterCutsceneFrameworkUpdate;
        TryOpenWindow();
    }

    private static void TryOpenWindow() {
        SimpleLog.Verbose("Try opening NeedGreed");
        var needGreedWindow = (AtkUnitBase*)Service.GameGui.GetAddonByName("NeedGreed", 1).Address;
        if (needGreedWindow != null) {
            SimpleLog.Verbose("NeedGreed already open.");
            return;
        }

        SimpleLog.Verbose("Opening NeedGreed window.");
        var notification = (AtkUnitBase*)Service.GameGui.GetAddonByName("_Notification", 1).Address;
        if (notification == null) {
            SimpleLog.Verbose("_Notification not open.");
            return;
        }

        Common.GenerateCallback(notification, 0, 2);
    }

    protected override void Disable() {
        Service.Chat.CheckMessageHandled -= HandleChat;
        Common.FrameworkUpdate -= TryOpenAfterCutsceneFrameworkUpdate;
    }
}
