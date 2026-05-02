using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using SimpleTweaksPlugin.Enums;
using SimpleTweaksPlugin.TweakSystem;
using SimpleTweaksPlugin.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using SimpleTweaksPlugin.Events;

namespace SimpleTweaksPlugin.Tweaks.Tooltips;
[TweakName("Track Outfits")]
[TweakDescription("Shows whether or not you've made an outfit out of the hovered item.")]
[TweakAuthor("croizat")]
[TweakReleaseVersion("1.10.8.0")]
[Changelog("1.14.0.0", "Fixed HQ outfits not working.")]
public unsafe class TrackOutfits : TooltipTweaks.SubTweak
{
    [TweakHook(typeof(UIState), nameof(UIState.IsItemActionUnlocked), nameof(IsItemActionUnlockedDetour))]
    private HookWrapper<UIState.Delegates.IsItemActionUnlocked> isItemActionUnlockedHookWrapper;

    [LinkHandler(LinkHandlerId.TrackOutfitsIdentifier)]
    private DalamudLinkPayload identifier;
    
    private record OwnedOutfit(uint SetId, List<uint> OwnedItems, List<uint> MissingItems);
    private DateTime ownedOutfitCacheLastUpdate = DateTime.MinValue;
    
    
    private List<OwnedOutfit> OwnedOutfits {
        get {
            var cacheTime = AgentMiragePrismPrismBox.Instance()->IsAgentActive() ? 1 : 30;
            if (DateTime.Now - ownedOutfitCacheLastUpdate < TimeSpan.FromSeconds(cacheTime)) {
                return field;
            }
            ownedOutfitCacheLastUpdate = DateTime.Now;
            var agent = ItemFinderModule.Instance();
            var l = new List<OwnedOutfit>();
            for (ushort i = 0; i < agent->GlamourDresserItemIds.Length && i < agent->GlamourDresserItemSetUnlockBits.Length; i++) {
                var itemId = agent->GlamourDresserItemIds[i];
                if (itemId == 0) continue;
                if (!Service.Data.GetExcelSheet<MirageStoreSetItem>().TryGetRow(itemId, out var row)) continue;
                var ownedItems = new List<uint>();
                var missingItems = new List<uint>();
                var bits = ItemFinderModule.Instance()->GlamourDresserItemSetUnlockBits[i];
                for (int j = 0; j < 11; j++) {
                    RowRef<Item>? slot = j switch {
                        0 => row.MainHand,
                        1 => row.OffHand,
                        2 => row.Head,
                        3 => row.Body,
                        4 => row.Hands,
                        5 => row.Legs,
                        6 => row.Feet,
                        7 => row.Earrings,
                        8 => row.Necklace,
                        9 => row.Bracelets,
                        10 => row.Ring,
                        _ => null
                    };
                    
                    if (slot == null || slot.Value.RowId == 0) continue;
                    
                    if (MirageManager.Instance()->PrismBoxLoaded) {
                        if (MirageManager.Instance()->IsSetSlotUnlocked(i, j)) {
                            ownedItems.Add(slot.Value.RowId);
                        } else {
                            missingItems.Add(slot.Value.RowId);
                        }
                    } else {
                        if (((bits >> j) & 1) == 0) {
                            ownedItems.Add(slot.Value.RowId);
                        } else {
                            missingItems.Add(slot.Value.RowId);
                        }
                    }
                }
                l.Add(new OwnedOutfit(itemId, ownedItems, missingItems));
            }
            return field = l;
        }
    } = new();

    private long IsItemActionUnlockedDetour(UIState* uiState, void* item)
    {
        if (GetOutfits(ItemUtil.GetBaseId(Item.ItemId).ItemId) is { Length: > 0 } outfits)
        {
            foreach (var o in outfits)
                if (!OwnedOutfits.Any(oo => oo.SetId == o && oo.OwnedItems.Contains(Item.ItemId)))
                    return 2;
            return 1;
        }
        return isItemActionUnlockedHookWrapper!.Original(uiState, item);
    }

    public override void OnGenerateItemTooltip(NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
    {
        if (GetOutfits(ItemUtil.GetBaseId(Item.ItemId).ItemId) is { Length: > 0 } outfits)
        {
            var description = GetTooltipString(stringArrayData, TooltipTweaks.ItemTooltipField.ItemDescription);
            
            if (description == null || description.Payloads.Any(payload => payload is DalamudLinkPayload dlp && dlp.CommandId == identifier.CommandId))
                return; // Don't append when it already exists.

            description.Payloads.Add(identifier);
            description.Payloads.Add(RawPayload.LinkTerminator);

            description.Payloads.Add(new NewLinePayload());
            description.Payloads.Add(new TextPayload("Outfits"));

            foreach (var outfit in outfits) {
                var ownedOutfit = OwnedOutfits.FirstOrDefault(oo => oo?.SetId == outfit, null);
                var isOutfitOwned = ownedOutfit?.OwnedItems.Contains(Item.ItemId) ?? false;
                description.Payloads.Add(new NewLinePayload());
                description.Payloads.Add(new UIForegroundPayload((ushort)(isOutfitOwned ? 45 : 14)));
                description.Payloads.Add(new TextPayload($"    {Service.Data.GetExcelSheet<Item>().GetRow(outfit).Name} (Acquired: {(isOutfitOwned ? "Yes" : "No")})"));
                description.Payloads.Add(new UIForegroundPayload(0));
            }

            try
            {
                SetTooltipString(stringArrayData, TooltipTweaks.ItemTooltipField.ItemDescription, description);
            }
            catch (Exception ex)
            {
                SimpleLog.Error(ex);
            }
        }
    }

    private static uint[] GetOutfits(uint itemId)
    {
        return Service.Data.GetExcelSheet<MirageStoreSetItemLookup>()
            .Where(row => row.RowId == itemId)
            .SelectMany(row => row.Item.Where(x => x.Value.RowId != 0))
            .Select(x => x.Value.RowId)
            .ToArray();
    }
}
