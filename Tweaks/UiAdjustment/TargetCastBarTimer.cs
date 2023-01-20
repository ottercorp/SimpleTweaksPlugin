﻿using System;
using System.Numerics;
using Dalamud.Game;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SimpleTweaksPlugin.Enums;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using SimpleTweaksPlugin.TweakSystem;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using SimpleTweaksPlugin.Utility;
using Newtonsoft.Json.Linq;
using static Lumina.Data.Parsing.Uld.NodeData;
using XivCommon.Functions;

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment {
    public unsafe class TargetCastBarTimer : UiAdjustments.SubTweak {

        public class Config : TweakConfig {
            public bool ShowCastTimeLeft;
            public Alignment CastTimeAlignment = Alignment.TopLeft;
            public int Offset = 8;
        }

        public Config LoadedConfig { get; private set; }

        public override string Name => "Target Castbar Timer";
        public override string Description => "Show Remain Cast Time in Target Castbar";

        private readonly Vector2 buttonSize = new Vector2(26, 22);

        private delegate long SetTargetCast(AgentHUD* agentHUD, NumberArrayData* numberArrayData, StringArrayData* stringArrayData, FFXIVClientStructs.FFXIV.Client.Game.Character.Character* chara);
        private HookWrapper<SetTargetCast> setTargetCastHook;

        private delegate void UpdateTargetCastBar(AtkUnitBase* targetInfoBase, NumberArrayData* numberArrayData, StringArrayData* stringArrayData, AtkUnitBase* castBar, bool changed);
        private HookWrapper<UpdateTargetCastBar> updateTargetCastBarHook;
        protected override DrawConfigDelegate DrawConfigTree => (ref bool changed) => {
            var bSize = buttonSize * ImGui.GetIO().FontGlobalScale;
            ImGui.SetNextItemWidth(90 * ImGui.GetIO().FontGlobalScale);
            ImGui.SetNextItemWidth(90 * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputInt($"###{GetType().Name}_Offset", ref LoadedConfig.Offset)) {
                if (LoadedConfig.Offset > MaxOffset) LoadedConfig.Offset = MaxOffset;
                if (LoadedConfig.Offset < MinOffset) LoadedConfig.Offset = MinOffset;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2));
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)FontAwesomeIcon.ArrowUp}", bSize)) {
                LoadedConfig.Offset = 8;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Above progress bar");

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)FontAwesomeIcon.CircleNotch}", bSize)) {
                LoadedConfig.Offset = 24;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Original Position");


            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)FontAwesomeIcon.ArrowDown}", bSize)) {
                LoadedConfig.Offset = 32;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Below progress bar");
            ImGui.PopStyleVar();
            ImGui.SameLine();
            ImGui.Text("Cast time vertical offset");

            changed |= ImGuiExt.HorizontalAlignmentSelector("Cast Time Alignment", ref LoadedConfig.CastTimeAlignment, VerticalAlignment.Bottom);
        };

        private const int MinOffset = 0;
        private const int MaxOffset = 48;

        private const int TargetCastTimeNodeId = CustomNodes.TargetCastBarTimer;
        private AtkTextNode* TargetCastTimeNode;

        private AtkTextNode* AddCastTimeTextNode(AtkUnitBase* splitCastBar) {
            var cloneTextNode = (AtkTextNode*)splitCastBar->UldManager.NodeList[5];
            var textNode = UiHelper.CloneNode(cloneTextNode);
            textNode->AtkResNode.NodeID = TargetCastTimeNodeId;
            var newStrPtr = UiHelper.Alloc(512);
            textNode->NodeText.StringPtr = (byte*)newStrPtr;
            textNode->NodeText.BufSize = 512;
            textNode->SetText("");
            UiHelper.ExpandNodeList(splitCastBar, 1);
            splitCastBar->UldManager.NodeList[splitCastBar->UldManager.NodeListCount++] = (AtkResNode*)textNode;

            var nextNode = (AtkResNode*)cloneTextNode;
            while (nextNode->PrevSiblingNode != null) nextNode = nextNode->PrevSiblingNode;

            textNode->AtkResNode.ParentNode = nextNode->ParentNode;
            textNode->AtkResNode.ChildNode = null;
            textNode->AtkResNode.NextSiblingNode = nextNode;
            textNode->AtkResNode.PrevSiblingNode = null;
            nextNode->PrevSiblingNode = (AtkResNode*)textNode;
            nextNode->ParentNode->ChildCount += 1;
            return textNode;
        }
        private void UpdateCastTimeTextNode(AtkUnitBase* splitCastBar, float remainTime) {
        }
        public override void Enable() {
            if (Enabled) return;
            LoadedConfig = LoadConfig<Config>() ?? new Config();
            setTargetCastHook ??= Common.Hook<SetTargetCast>("E8 ?? ?? ?? ?? 8B D8 85 C0 79 ?? 45 33 C0", SetTargetCastDetour);
            setTargetCastHook?.Enable();
            updateTargetCastBarHook ??= Common.Hook<UpdateTargetCastBar>("E8 ?? ?? ?? ?? 4C 8D 8F ?? ?? ?? ?? 4D 8B C6", UpdateTargetCastBarDetour);
            updateTargetCastBarHook?.Enable();
            Enabled = true;
        }

        private AtkTextNode* GetTargetCastTimeNode(AtkUnitBase* targetInfoBase) {
            var count = targetInfoBase->UldManager.NodeListCount;
            for (var i = 7; i < count; i++) {
                var node = targetInfoBase->UldManager.NodeList[i];
                if (node->NodeID == TargetCastTimeNodeId) {
                    return (AtkTextNode*)node;
                };
            }
            return null;
        }

        private void UpdateTargetCastBarDetour(AtkUnitBase* targetInfoBase, NumberArrayData* numberArrayData, StringArrayData* stringArrayData, AtkUnitBase* castBar, bool changed) {
            updateTargetCastBarHook.Original(targetInfoBase, numberArrayData, stringArrayData, castBar, changed);
            if (LoadedConfig.ShowCastTimeLeft) {
                var targetCastTimeNode = GetTargetCastTimeNode(targetInfoBase);
                if (targetCastTimeNode == null)
                    targetCastTimeNode = AddCastTimeTextNode(targetInfoBase);

                targetCastTimeNode->AlignmentFontType = (byte)(0x26 + (byte)LoadedConfig.CastTimeAlignment);
                targetCastTimeNode->AtkResNode.Height = (ushort)LoadedConfig.Offset;
                targetCastTimeNode->FontSize = 15;
                //targetCastTimeNode->SetText(RemainCastTime.ToString("00.00"));
                targetCastTimeNode->AtkResNode.ToggleVisibility(targetInfoBase->UldManager.NodeList[5]->IsVisible);
                TargetCastTimeNode= targetCastTimeNode;
            }
        }

        private float RemainCastTime = 0f;
        private unsafe long SetTargetCastDetour(AgentHUD* agentHUD, NumberArrayData* numberArrayData, StringArrayData* stringArrayData, FFXIVClientStructs.FFXIV.Client.Game.Character.Character* chara) {
            var ret = setTargetCastHook.Original(agentHUD, numberArrayData, stringArrayData, chara);
            if (ret != 0xFFFFFFFF && ret != 0) {
                if (LoadedConfig.ShowCastTimeLeft) {
                    var cast = chara->GetCastInfo();
                    if ((IntPtr)cast != IntPtr.Zero) {
                        RemainCastTime = cast->AdjustedTotalCastTime - cast->CurrentCastTime;
                        if (TargetCastTimeNode!=null)
                            // More accuracy
                            TargetCastTimeNode->SetText(RemainCastTime.ToString("00.00"));
                    }
                }
            }
            return ret;
        }

        private void HideTargetInfoCastBarText() {
            var splitCastBar = Common.GetUnitBase("_TargetInfoCastBar");
            var targetCastTimeNode=GetTargetCastTimeNode(splitCastBar);
            if (targetCastTimeNode != null) {
                targetCastTimeNode->AtkResNode.ToggleVisibility(false);
            }
        }

        public override void Disable() {
            if (!Enabled) return;
            SaveConfig(LoadedConfig);
            //PluginConfig.UiAdjustments.TargetC = null;
            setTargetCastHook.Disable();
            updateTargetCastBarHook.Disable();
            HideTargetInfoCastBarText();
            SimpleLog.Debug($"[{GetType().Name}] Reset");
            Enabled = false;
            base.Disable();
        }

        public override void Dispose() {
            Enabled = false;
            Ready = false;
            base.Dispose();
        }
    }
}