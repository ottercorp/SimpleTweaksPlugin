using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using SimpleTweaksPlugin.Events;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin.Tweaks.Tooltips;

[TweakName("Armoire Tooltip for Glamour Dresser")]
[TweakDescription("Show a hint on the tooltip of items stored in the glamour dresser when they could be stored in armoire.")]
[TweakReleaseVersion("1.14.1.2")]
public unsafe class ArmoireTooltipForGlamourDresser : Tweak {
    private IReadOnlySet<uint> ArmoireItems => field != null ? field : field = Service.Data.GetExcelSheet<Cabinet>().Where(r => r.Item.RowId != 0).Select(i => i.Item.RowId).ToHashSet();
    
    [AddonPreRefresh("MiragePrismPrismItemDetail")]
    private void ItemDetailRefresh(AddonRefreshArgs args) {
        if (args.AtkValueCount != 15) return;
        if (ArmoireItems.Contains(AgentMiragePrismPrismItemDetail.Instance()->ItemId)) {
            var atkValue = (AtkValue*) args.AtkValueEnumerable.Skip(7).First().Address;
            if (atkValue->Type != AtkValueType.ManagedString) return;
            var seStr = SeString.Parse(atkValue->String.AsSpan());
            if (seStr.TextValue.Trim().Length != 0) {
                seStr.Append(new NewLinePayload());
                seStr.Append(new NewLinePayload());
            }
            
            seStr.Append($"{SeIconChar.BoxedStar.ToIconString()} ");
            seStr.Append(Service.Data.GetExcelSheet<Addon>().GetRow(11991).Text.ToDalamudString());
            atkValue->SetManagedString(seStr.Encode());
        }
    }
}
