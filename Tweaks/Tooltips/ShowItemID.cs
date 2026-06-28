using System;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Text;
using SimpleTweaksPlugin.Events;
using SimpleTweaksPlugin.TweakSystem;
using static SimpleTweaksPlugin.Tweaks.TooltipTweaks.ItemTooltipField;

namespace SimpleTweaksPlugin.Tweaks.Tooltips;

[TweakName("Show ID")]
[TweakDescription("Show the ID of actions and items on their tooltips.")]
[TweakAutoConfig]
[Changelog("1.10.3.2", "Added option to show original action ID alongside resolved.")]
[Changelog("1.10.3.2", "Fixed ID being cut off on items with long category names, like BLM weapons.")]
public unsafe class ShowItemID : TooltipTweaks.SubTweak {
    public class Configs : TweakConfig {
        [TweakConfigOption("Use Hexadecimal ID")]
        public bool Hex;

        public bool ShouldShowBoth() => Hex;
        [TweakConfigOption("Show Both HEX and Decimal", 1, ConditionalDisplay = true, SameLine = true)]
        public bool Both;

        [TweakConfigOption("Show Resolved Action ID", 2)]
        public bool ShowResolvedActionId = true;

        public bool ShouldShowShowOriginalActionId() => ShowResolvedActionId && (!Hex || !Both);
        [TweakConfigOption("Show Original Action ID", 3, ConditionalDisplay = true, SameLine = true)]
        public bool ShowOriginalActionId;
    }

    [TweakConfig] public Configs Config { get; private set; }

    [AddonPostRefresh("ActionDetail")]
    private void ActionDetailRefresh(AtkUnitBase* unitBase) {
        var node = unitBase->GetTextNodeById(6);
        if (node == null) return;
        node->TextFlags |= TextFlags.MultiLine;
    }

    [AddonPostRefresh("ItemDetail")]
    private void ItemDetailRefresh(AtkUnitBase* unitBase) {
        var node = unitBase->GetTextNodeById(35);
        if (node == null) return;
        node->TextFlags |= TextFlags.MultiLine;
    }

    public override void OnGenerateItemTooltip(NumberArrayData* numberArrayData, StringArrayData* stringArrayData) {
        var seStr = GetTooltipString(stringArrayData, ItemUiCategory);
        if (seStr == null) return;
        if (seStr.TextValue.EndsWith(']')) return;
        var id = AgentItemDetail.Instance()->ItemId;
        if (id < 2000000) id %= 500000;
        seStr.Payloads.Add(new UIForegroundPayload(3));
        seStr.Payloads.Add(new TextPayload($"   ["));
        if (Config.Hex == false || Config.Both) {
            seStr.Payloads.Add(new TextPayload($"{id}"));
        }

        if (Config.Hex) {
            if (Config.Both) seStr.Payloads.Add(new TextPayload(" - "));
            seStr.Payloads.Add(new TextPayload($"0x{id:X}"));
        }

        seStr.Payloads.Add(new TextPayload($"]"));
        seStr.Payloads.Add(new UIForegroundPayload(0));
        try {
            SetTooltipString(stringArrayData, ItemUiCategory, seStr);
        } catch (Exception ex) {
            Plugin.Error(this, ex);
        }
    }

    public override void OnActionTooltip(AtkUnitBase* addon, TooltipTweaks.HoveredActionDetail action) {
        var categoryText = addon->GetTextNodeById(6);
        if (categoryText == null) return;
        var seStr = categoryText->NodeText.AsReadOnlySeStringSpan();
        if (seStr.PayloadCount > 1) return;
        var builder = new SeStringBuilder();
        builder.Append(seStr);
        
        var id = Config.ShowResolvedActionId ? ActionManager.Instance()->GetAdjustedActionId(action.Id) : action.Id;
        if (seStr.PayloadCount >= 1) {
            if (Config.ShowResolvedActionId && Config.ShowOriginalActionId && (!Config.Both || !Config.Hex) && id != action.Id) {
                builder.AppendNewLine();
            } else {
                builder.Append(" ");
            }
        }

        builder.PushColorType(3);
        builder.Append("[");
        if (Config.ShowResolvedActionId && Config.ShowOriginalActionId && (!Config.Both || !Config.Hex) && id != action.Id) {
            builder.Append($"{action.Id}→");
        }

        if (!Config.Hex || Config.Both) {
            builder.Append($"{id}");
        }

        if (Config.Hex) {
            if (Config.Both) builder.Append(" - ");
            builder.Append($"0x{id:X}");
        }

        builder.Append("]");
        builder.PopColorType();
        categoryText->SetText(builder.GetViewAsSpan());
    }
}
