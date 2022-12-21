﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SimpleTweaksPlugin.TweakSystem;

#if DEBUG
using SimpleTweaksPlugin.Debugging;
#endif

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment {
    public unsafe class HideJobGauge : UiAdjustments.SubTweak {
        public override string Name => "隐藏职业量谱";
        public override string Description => "允许在非“战斗”和“任务”中，隐藏职业职业量谱";

        public class Configs : TweakConfig {

            [TweakConfigOption("任务中显示", 1)]
            public bool ShowInDuty;

            [TweakConfigOption("战斗中显示", 2)]
            public bool ShowInCombat;

            public bool ShouldShowCombatBuffer() => ShowInCombat;
            [TweakConfigOption("Out of Combat Time (Seconds)", 3, EditorSize = 100, IntMin = 0, IntMax = 300, ConditionalDisplay = true)]
            public int CombatBuffer;

            [TweakConfigOption("Show While Weapon Is Drawn", 4)]
            public bool ShowWhileWeaponDrawn;
        }

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;
        private readonly Stopwatch outOfCombatTimer = new Stopwatch();
        
        public override void Enable() {
            outOfCombatTimer.Restart();
            Config = LoadConfig<Configs>() ?? new Configs();
            Service.Framework.Update += FrameworkUpdate;
            base.Enable();
        }
        
        private void FrameworkUpdate(Framework framework) {
            try {
                Update();
            } catch {
                // 
            }
            
        }

        private readonly ushort[] nonCombatTerritory = {
            1055, // Island Sanctuary
        };

        private bool InCombatDuty => Service.Condition[ConditionFlag.BoundByDuty] && !nonCombatTerritory.Contains(Service.ClientState.TerritoryType);
        
        private void Update(bool reset = false) {
            var stage = AtkStage.GetSingleton();
            var loadedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
            var addonList = &loadedUnitsList->AtkUnitEntries;
            #if DEBUG
            PerformanceMonitor.Begin();
            #endif
            for (var i = 0; i < loadedUnitsList->Count; i++) {
                var addon = addonList[i];
                var name = Helper.Common.PtrToUTF8(new IntPtr(addon->Name));
                
                if (name != null && name.StartsWith("JobHud")) {
                    if (reset || Config.ShowInDuty && InCombatDuty) {
                        if (addon->UldManager.NodeListCount == 0) addon->UldManager.UpdateDrawNodeList();
                    } else if (Config.ShowInCombat && Service.Condition[ConditionFlag.InCombat]) {
                        outOfCombatTimer.Restart();
                        if (addon->UldManager.NodeListCount == 0) addon->UldManager.UpdateDrawNodeList();
                    } else if (Config.ShowInCombat && outOfCombatTimer.ElapsedMilliseconds < Config.CombatBuffer * 1000) {
                        if (addon->UldManager.NodeListCount == 0) addon->UldManager.UpdateDrawNodeList();
                    } else if (Config.ShowWhileWeaponDrawn && Service.ClientState.LocalPlayer != null && Service.ClientState.LocalPlayer.StatusFlags.HasFlag(StatusFlags.WeaponOut)) {
                        if (addon->UldManager.NodeListCount == 0) addon->UldManager.UpdateDrawNodeList();
                    } else {
                        addon->UldManager.NodeListCount = 0;
                    }
                }

            }
            #if DEBUG
            PerformanceMonitor.End();
            #endif
        }

        public override void Disable() {
            Service.Framework.Update -= FrameworkUpdate;
            try {
                Update(true);
            } catch {
                //
            }
            SaveConfig(Config);
            base.Disable();
        }
    }
}
