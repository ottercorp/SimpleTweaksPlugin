using Dalamud.Utility;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.STD;
using SimpleTweaksPlugin.TweakSystem;
using SimpleTweaksPlugin.Utility;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Utility.Signatures;
using Dalamud.Memory;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.UI;
using JetBrains.Annotations;
using Lumina.Excel.Sheets;
using SimpleTweaksPlugin.Events;

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment;

[TweakName("Fast Item Search")]
[TweakDescription("Enable superfast searches for the market board & crafting log.")]
[TweakAuthor("Asriel")]
[TweakAutoConfig]
[TweakReleaseVersion("1.9.2.0")]
[Changelog("1.9.2.1", "Fix random ordering (results are now always in the same order)")]
public unsafe class FastSearch : UiAdjustments.SubTweak {
    public class FastSearchConfig : TweakConfig {
        [TweakConfigOption("Use Fuzzy Search")]
        public bool UseFuzzySearch;
    }

    [TweakConfig] public FastSearchConfig Config { get; private set; }
    
    [TweakHook(typeof(AgentRecipeNote), nameof(AgentRecipeNote.SearchRecipe), nameof(RecipeNoteRecieveDetour))]
    private readonly HookWrapper<AgentRecipeNote.Delegates.SearchRecipe> searchRecipeHook;

    [TweakHook(typeof(RecipeSearchContext), true, nameof(RecipeSearchContext.Iterate), nameof(RecipeNoteIterateDetour))]
    private readonly HookWrapper<RecipeSearchContext.Delegates.Iterate> searchIterateHook = null!;

    private delegate void AgentItemSearchUpdateDelegate(AgentItemSearch* a1);


    [UsedImplicitly, TweakHook, Signature("E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 80 BB ?? ?? ?? ?? ?? 0F 85", DetourName = nameof(AgentItemSearchUpdateDetour))]
    private readonly HookWrapper<AgentItemSearchUpdateDelegate> agentItemSearchUpdate1Hook = null!;

    private delegate void AgentItemSearchUpdateAtkValuesDelegate(AgentItemSearch* a1, uint a2, byte* a3, bool a4);

    [TweakHook, Signature("40 55 56 41 56 B8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 2B E0 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 83 79 20 00", DetourName = nameof(AgentItemSearchUpdateAtkValuesDetour))]
    private readonly HookWrapper<AgentItemSearchUpdateAtkValuesDelegate> agentItemSearchUpdateAtkValuesHook;

    private delegate void AgentItemSearchPushFoundItemsDelegate(AgentItemSearch* a1);

    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 89 9C 24 ?? ?? ?? ?? 41 2B C9")]
    private readonly AgentItemSearchPushFoundItemsDelegate agentItemSearchPushFoundItems;

    [AddonPostSetup("ItemSearch")]
    private void SetupItemSearch(AddonItemSearch* addon) {
        var checkbox = addon->PartialSearchCheckBox;
        var text = checkbox->AtkComponentButton.ButtonTextNode;
        text->SetText(Config.UseFuzzySearch ? "Fuzzy Item Search" : "Fast Item Search");
    }

    private void RecipeNoteRecieveDetour(AgentRecipeNote* agentRecipeNote, Utf8String* a2, byte a3, bool a4) {
        if (!agentRecipeNote->RecipeSearchProcessing) {
            searchRecipeHook.Original(agentRecipeNote, a2, a3, a4);

            RecipeSearch(a2->ToString(), &agentRecipeNote->SearchResults);
            agentRecipeNote->SearchContext->IsComplete = true;
        }
    }

    private void RecipeNoteIterateDetour() { }

    private void AgentItemSearchUpdateDetour(AgentItemSearch* agentItemSearch) {
        if (agentItemSearch->IsPartialSearching && !agentItemSearch->IsItemPushPending) {
            ItemSearch(agentItemSearch->StringData->SearchParam.ToString(), agentItemSearch);
            agentItemSearchPushFoundItems(agentItemSearch);
        }
    }

    private void AgentItemSearchUpdateAtkValuesDetour(AgentItemSearch* agentItemSearch, uint a2, byte* a3, bool a4) {
        var partialString = Service.Data.GetExcelSheet<Addon>().GetRow(3136).Text.ExtractText();
        var isPartial = MemoryHelper.ReadStringNullTerminated((nint)a3).Equals(partialString, StringComparison.Ordinal);
        if (isPartial) {
            var newText = Encoding.UTF8.GetBytes(Config.UseFuzzySearch ? "Fuzzy Item Search" : "Fast Item Search");
            fixed (byte* t = newText) {
                a3 = t;
            }
        }

        agentItemSearchUpdateAtkValuesHook.Original(agentItemSearch, a2, a3, a4);
    }

    private void RecipeSearch(string input, StdVector<uint>* output) {
        if (string.IsNullOrWhiteSpace(input))
            return;

        var sheet = Service.Data.GetExcelSheet<Recipe>();
        var validRows = sheet.Where(r => r.RecipeLevelTable.RowId != 0 && r.ItemResult.RowId != 0);
        var matcher = new FuzzyMatcher(input.ToLowerInvariant(), Config.UseFuzzySearch ? MatchMode.FuzzyParts : MatchMode.Simple);
        var query = validRows.AsParallel().Select(i => (Item: i, Score: matcher.Matches(i.ItemResult.Value!.Name.ToDalamudString().ToString().ToLowerInvariant()))).Where(t => t.Score > 0).OrderByDescending(t => t.Score).ThenBy(t => t.Item.RowId).Select(t => t.Item.RowId);

        output->AddRangeCopy(query);
    }

    private void ItemSearch(string input, AgentItemSearch* agent) {
        if (string.IsNullOrWhiteSpace(input))
            return;
        var sheet = Service.Data.GetExcelSheet<Item>();
        var marketItems = sheet.Where(i => i.ItemSearchCategory.RowId != 0);
        var matcher = new FuzzyMatcher(input.ToLowerInvariant(), Config.UseFuzzySearch ? MatchMode.FuzzyParts : MatchMode.Simple);
        var query = marketItems.AsParallel().Select(i => (Item: i, Score: matcher.Matches(i.Name.ToDalamudString().ToString().ToLowerInvariant()))).Where(t => t.Score > 0).OrderByDescending(t => t.Score).ThenBy(t => t.Item.RowId).Select(t => t.Item.RowId);
        foreach (var item in query) {
            agent->ItemBuffer[agent->ItemCount++] = item;
            if (agent->ItemCount >= 100)
                break;
        }
    }
}
