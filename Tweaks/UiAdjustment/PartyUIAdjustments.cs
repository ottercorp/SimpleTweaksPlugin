using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using System;
using System.Collections.Generic;
using SimpleTweaksPlugin.GameStructs;
using SimpleTweaksPlugin.Utility;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.UI;
using SimpleTweaksPlugin.Debugging;

namespace SimpleTweaksPlugin
{
    public partial class UiAdjustmentsConfig
    {
        public PartyUiAdjustments.Configs PartyUiAdjustments = new();
    }
}

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment
{
    public unsafe class PartyUiAdjustments : UiAdjustments.SubTweak
    {
        public class Configs
        {
            public bool HpPercent = true;
            public bool ShieldShift;
            public bool MpShield;
        }

        public Configs Config => PluginConfig.UiAdjustments.PartyUiAdjustments;

        private const string PartyNumber = "";

        private delegate long PartyUiUpdate(long a1, long a2, long a3);

        private Hook<PartyUiUpdate> partyUiUpdateHook;

        private AddonPartyList* party;
        private DataArray* data;
        private PartyStrings* stringarray;

        private IntPtr l1, l2, l3;


        public override string Name => "队伍列表修改";
        public override string Description => "队伍列表相关内容修改";


        protected override DrawConfigDelegate DrawConfigTree => (ref bool changed) =>
        {
            changed |= ImGui.Checkbox("HP及盾值百分比显示", ref Config.HpPercent);
            changed |= ImGui.Checkbox("使用盾值(估计值)替换MP值", ref Config.MpShield);
            changed |= ImGui.Checkbox("修改盾显示高度", ref Config.ShieldShift);

            if (changed) RefreshHooks();
        };


        private void RefreshHooks()
        {
            try
            {
                partyUiUpdateHook ??= Hook<PartyUiUpdate>.FromAddress(
                    Service.SigScanner.ScanText(
                        "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B 7A ?? 48 8B D9 49 8B 70 ?? 48 8B 47"),
                    new PartyUiUpdate(PartyListUpdateDelegate));


                if (Enabled) partyUiUpdateHook?.Enable();
                else partyUiUpdateHook?.Disable();

                if (!Config.ShieldShift) UnShiftShield();
                else ShiftShield();
                if (!Config.MpShield) ResetMp();
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
                throw;
            }
        }

        private void DisposeHooks()
        {
            partyUiUpdateHook?.Dispose();
            partyUiUpdateHook = null;
        }

        private void DisableHooks()
        {
            //if (!partyUiUpdateHook.IsDisposed) 
            partyUiUpdateHook?.Disable();
            UnShiftShield();
        }


        #region detors

        //PartyListUpdateDelegate(AtkUnitBase* addonPartyList, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
        private unsafe long PartyListUpdateDelegate(long a1, long a2, long a3)
        {
            if ((IntPtr) a1 != l1)
            {
                l1 = (IntPtr) a1;
                l2 = (IntPtr) (*(long*) (*(long*) (a2 + 0x20) + 0x20));
                l3 = (IntPtr) (*(long*) (*(long*) (a3 + 0x18) + 0x20) + 0x30); //+Index*0x68
                party = (AddonPartyList*) l1;
                data = (DataArray*) l2;
                stringarray = (PartyStrings*)l3;
                //SimpleLog.Information("NewAddress:");
                //SimpleLog.Information("L1:" + l1.ToString("X") + " L2:" + l2.ToString("X"));
                //SimpleLog.Information("L3:" + l3.ToString("X"));
            }
#if DEBUG
                PerformanceMonitor.Begin("PartyListLayout.Update");

#endif
                //UpdatePartyUi(false);
                var ret = partyUiUpdateHook.Original(a1, a2, a3);
                UpdatePartyUi(true);
#if DEBUG
                PerformanceMonitor.End("PartyListLayout.Update");
#endif
            return ret;
        }

        #endregion

        #region string functions

        private static void SplitString(string str, bool first, out string part1, out string part2)
        {
            str = str.Trim();
            if (str.Length == 0)
            {
                part1 = "";
                part2 = "";
                return;
            }

            var index = first ? str.IndexOf(' ') : str.LastIndexOf(' ');
            if (index == -1)
            {
                part1 = str;
                part2 = "";
            }
            else
            {
                part1 = str.Substring(0, index).Trim();
                part2 = str.Substring(index + 1).Trim();
            }
        }
        private static void SplitLvlName(byte* str,out string lvl,out string name)
        {
            try
            {
                var offset = 0;
                var c = 0;
                lvl = "";
                name = "";
                while (true)
                {
                    var b = *(str + offset);
                    if (b == 0x20)
                    {
                        if (*(str + offset - 1) == 0xA7 && *(str + offset - 2) == 0xBA && *(str + offset - 3) == 0xE7)
                        {
                            lvl = Encoding.UTF8.GetString(str, offset+1);
                            c = offset;
                        }
                    }
                    if (b == 0)
                    {
                        if(c == 0)
                            name = Encoding.UTF8.GetString(str, offset);
                        else
                            name = Encoding.UTF8.GetString(str + c+1, offset);
                        break;
                    }
                    offset += 1;
                }
            }
            catch(Exception e)
            {
                lvl = "";
                name = "";
                SimpleLog.Error(e);
            }
            
        }

        private void SetName(AtkTextNode* node, string payload)
        {
            if (node == null || payload == string.Empty) return;
            Common.WriteSeString(node->NodeText, payload);
        }

        private void SetHp(AtkTextNode* node, MemberData member)
        {
            var se = new SeString(new List<Payload>());
            if (member.CurrentHP == 1)
            {
                se.Payloads.Add(new TextPayload("1"));
            }
            else if (member.MaxHp == 1 || member.MaxHp == 0)
            {
                se.Payloads.Add(new TextPayload("???"));
            }
            else
            {
                se.Payloads.Add(new TextPayload((member.CurrentHP * 100 / member.MaxHp).ToString()));
                if (member.ShieldPercent != 0)
                {
                    UIForegroundPayload uiYellow =
                        new(559);
                    UIForegroundPayload uiNoColor =
                        new(0);

                    se.Payloads.Add(new TextPayload("+"));
                    se.Payloads.Add(uiYellow);
                    se.Payloads.Add(new TextPayload(member.ShieldPercent.ToString()));
                    se.Payloads.Add(uiNoColor);
                }

                se.Payloads.Add(new TextPayload("%"));
            }

            Common.WriteSeString(node->NodeText, se);
        }

        private string GetJobName(int id)
        {
            if (id < 0 || id > 40) return "打开方式不对";
            return Service.ClientState.ClientLanguage == ClientLanguage.English
                ? Service.Data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.ClassJob>().GetRow((uint) id)
                    .NameEnglish
                : Service.Data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.ClassJob>().GetRow((uint) id).Name;
        }

        private static AtkResNode* GetNodeById(AtkComponentBase* compBase, int id)
        {
            if (compBase == null) return null;
            if ((compBase->UldManager.Flags1 & 1) == 0 || id == 0) return null;
            if (compBase->UldManager.Objects == null) return null;
            var count = compBase->UldManager.Objects->NodeCount;
            var ptr = (long) compBase->UldManager.Objects->NodeList;
            //SimpleLog.Information($"{ptr:x} {count}");
            for (var i = 0; i < count; i++)
            {
                var node = (AtkResNode*) *(long*) (ptr + 8 * i);
                if (node->NodeID == id) return node;
            }

            return null;
        }
        
        #endregion


        private void ShiftShield()
        {
            if (l1 == IntPtr.Zero) return;
            for (var i = 0; i < 8; i++)
            {
                var hpBarComponentBase = (AtkComponentBase*)party->PartyMember[i].HPGaugeBar;

                if (hpBarComponentBase == null) return;
                for (int j = 2; j < 6; j++)
                {
                    var node = GetNodeById(hpBarComponentBase, j);
                    if (node != null)
                    {
                        node->Y = j switch
                        {
                            2 => 9 + 8,
                            3 or 4 => 0,
                            5 => 8 + 8,
                            _ => node->Y
                        };
                    }
                }
            }
        }

        private void UnShiftShield()
        {
            if (l1 == IntPtr.Zero) return;
            for (var i = 0; i < 8; i++)
            {
                var hpBarComponentBase =(AtkComponentBase*)party->PartyMember[i].HPGaugeBar;
                if (hpBarComponentBase == null) return;
                for (int j = 2; j < 6; j++)
                {
                    var node = GetNodeById(hpBarComponentBase, j);
                    if (node != null)
                    {
                        node->Y = j switch
                        {
                            2 => 9,
                            3 or 4 => -8,
                            5 => 8,
                            _ => node->Y
                        };
                    }
                }
            }
        }

        private void ShieldOnMp(int index)
        {
            if (l1 == IntPtr.Zero) return;
            var memberData = data->MemberData(index);
            if (memberData.HasMP == 0 ) return;
            var shield = memberData.ShieldPercent * memberData.MaxHp / 100;
            var node1 = (AtkTextNode*)GetNodeById((AtkComponentBase*)party->PartyMember[index].MPGaugeBar, 3);
            var node2 = (AtkTextNode*) GetNodeById((AtkComponentBase*)party->PartyMember[index].MPGaugeBar, 2);
            if (node1 == null || node2 == null) return;
            UIForegroundPayload uiYellow =
                new(559);
            SeString se = new(new List<Payload>());
            se.Payloads.Add(uiYellow);
            se.Payloads.Add(new TextPayload(shield.ToString()));
            Common.WriteSeString(node1->NodeText, se);
            if (node1->FontSize != 12)
            {
                node1->FontSize = 12;
                node1->AlignmentFontType -= 2;
            }

            Common.WriteSeString(node2->NodeText, "");
        }

        private void ResetMp()
        {
            if (l1 == IntPtr.Zero) return;
            for (var index = 0; index < 8; index++)
            {
                var node1 = (AtkTextNode*) GetNodeById((AtkComponentBase*)party->PartyMember[index].MPGaugeBar, 3);
                if (node1 == null) return;
                if (node1->FontSize == 12)
                {
                    node1->FontSize = 10;
                    node1->AlignmentFontType += 2;
                }
            }
        }


        private void UpdatePartyUi(bool done)
        {
            try
            {
                for (var index = 0; index < 8; index++)
                {
                    if (done)
                    {
                        if (Config.HpPercent)
                        {
                            var textNode = party->PartyMember[index].HPGaugeComponent->UldManager.SearchNodeById(2)->GetAsAtkTextNode();
                            //party->Member(index).HPGaugeComponent->UldManager.SearchNodeById(2)->Color.A =0xff; //hpvalue
                            //party->Member(index).HPGaugeComponent->UldManager.SearchNodeById(3)->Color.A = 0xff;// unk
                            //party->Member(index).HPGaugeComponent->UldManager.SearchNodeById(4)->Color.A = 0xff;//hpbar
                            if (textNode != null)
                            {
                                SetHp(textNode, data->MemberData(index));
                            }
                        }
                        if (Config.MpShield) ShieldOnMp(index);
                        if (Config.ShieldShift) ShiftShield();
                        else UnShiftShield();
                    }
                }
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
                throw;
            }
        }
        
        #region Framework

        public override void Enable()
        {
            if (Enabled) return;
            Enabled = true;
            RefreshHooks();
        }

        public override void Disable()
        {
            if (!Enabled) return;
            DisableHooks();
            SimpleLog.Debug($"[{GetType().Name}] Reset");
            Enabled = false;
        }


        public override void Dispose()
        {
            DisposeHooks();
            Enabled = false;
            Ready = false;
            SimpleLog.Debug($"[{GetType().Name}] Disposed");
        }

        #endregion
    }
}