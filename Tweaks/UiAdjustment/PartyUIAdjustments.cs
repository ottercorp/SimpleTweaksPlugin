#if DEBUG
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
#if DEBUG
using SimpleTweaksPlugin.Debugging;
#endif
using SimpleTweaksPlugin.GameStructs;
using SimpleTweaksPlugin.Utility;
using System.Text;
using System.Linq;
using System.IO;

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
            public bool Target;
            public bool Focus;
            public bool HpPercent = true;
            public bool PartyName;
            public bool ShieldShift;
            public bool MpShield;
            public float ShieldOffset;
        }

        public Configs Config => PluginConfig.UiAdjustments.PartyUiAdjustments;

        private const string PartyNumber = "";

        private delegate long PartyUiUpdate(long a1, long a2, long a3);

        private delegate void MainTargetUiUpdate(long a1, long a2, long a3);

        private delegate long FocusUiUpdate(long a1, long a2, long a3);

        private Hook<PartyUiUpdate> partyUiUpdateHook;
        private Hook<MainTargetUiUpdate> mainTargetUpdateHook;
        private Hook<MainTargetUiUpdate> targetUpdateHook;
        private Hook<FocusUiUpdate> focusUpdateHook;

        private PartyUi* party;
        private DataArray* data;
        private PartyStrings* stringarray;

        private AtkTextNode* focusTextNode;
        private AtkTextNode* tTextNode;
        private AtkTextNode* ttTextNode;
        private IntPtr l1, l2, l3;


        public override string Name => "队伍列表修改";
        public override string Description => "队伍列表相关内容修改";


        protected override DrawConfigDelegate DrawConfigTree => (ref bool changed) =>
        {
            changed |= ImGui.Checkbox("HP及盾值百分比显示", ref Config.HpPercent);
            changed |= ImGui.Checkbox("使用盾值(估计值)替换MP值", ref Config.MpShield);
            changed |= ImGui.Checkbox("修改盾显示高度", ref Config.ShieldShift);
            changed |= ImGui.SliderFloat("盾位移高度",ref Config.ShieldOffset,-16f,16f);
#if DEBUG
                changed |= ImGui.Checkbox("将队伍栏的队友姓名替换为职业名", ref Config.PartyName);
                ImGui.SameLine();
                changed |= ImGui.Checkbox("将目标栏的队友姓名替换为职业名", ref Config.Target);
                ImGui.SameLine();
                changed |= ImGui.Checkbox("将焦点栏的队友姓名替换为职业名", ref Config.Focus);
#endif
            if (changed) RefreshHooks();
        };


        private void RefreshHooks()
        {
            try
            {
                SimpleLog.Information($"partyUiUpdateAddress:0x{Service.SigScanner.ScanText(
                        "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B 7A ?? 48 8B D9 49 8B 70 ?? 48 8B 47"):x}");
                partyUiUpdateHook ??= Hook<PartyUiUpdate>.FromAddress(
                    Service.SigScanner.ScanText(
                        "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B 7A ?? 48 8B D9 49 8B 70 ?? 48 8B 47"),
                    new PartyUiUpdate(PartyListUpdateDelegate));
                
                targetUpdateHook ??= Hook<MainTargetUiUpdate>.FromAddress(
                    Service.SigScanner.ScanText(
                        "40 55 57 41 56 48 83 EC 40 48 8B 6A 48 48 8B F9 4D 8B 70 40 48 85 ED 0F 84 ?? ?? ?? ?? 4D 85 F6 0F 84 ?? ?? ?? ?? 48 8B 45 20 48 89 74 24 ?? 4C 89 7C 24 ?? 44 0F B6 B9 ?? ?? ?? ?? 83 38 00 8B 70 08 0F 95 C0"),
                    new MainTargetUiUpdate(TargetUpdateDelegate));
                
                mainTargetUpdateHook ??= Hook<MainTargetUiUpdate>.FromAddress(
                    Service.SigScanner.ScanText(
                        "40 55 57 41 56 48 83 EC 40 48 8B 6A 48 48 8B F9 4D 8B 70 40 48 85 ED 0F 84 ?? ?? ?? ?? 4D 85 F6 0F 84 ?? ?? ?? ?? 48 8B 45 20 48 89 74 24 ?? 4C 89 7C 24 ?? 44 0F B6 B9 ?? ?? ?? ?? 83 38 00 8B 70 08 0F 94 C0"),
                    new MainTargetUiUpdate(MainTargetUpdateDelegate));
                
                focusUpdateHook ??= Hook<FocusUiUpdate>.FromAddress(
                    Service.SigScanner.ScanText("40 53 41 54 41 56 41 57 48 83 EC 78 4C 8B 7A 48"),
                    new FocusUiUpdate(FocusUpdateDelegate));

                if (Enabled) partyUiUpdateHook?.Enable();
                else partyUiUpdateHook?.Disable();
//#if DEBUG
//                if (Config.Target) targetUpdateHook?.Enable();
//                else targetUpdateHook?.Disable();
//                if (Config.Target) mainTargetUpdateHook?.Enable();
//                else mainTargetUpdateHook?.Disable();
//                if (Config.Focus) focusUpdateHook?.Enable();
//                else focusUpdateHook?.Disable();
//#endif
                
//                if (!Config.ShieldShift) UnShiftShield();
//                    else ShiftShield();
//                if (!Config.MpShield) ResetMp();
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
            targetUpdateHook?.Dispose();
            targetUpdateHook = null;
            mainTargetUpdateHook?.Dispose();
            mainTargetUpdateHook = null;
            focusUpdateHook?.Dispose();
            focusUpdateHook = null;
            UnShiftShield();
        }

        private void DisableHooks()
        {
            //if (!partyUiUpdateHook.IsDisposed) 
            partyUiUpdateHook?.Disable();
            targetUpdateHook?.Disable();
            mainTargetUpdateHook?.Disable();
            focusUpdateHook?.Disable();
        }


        #region detors

        //PartyListUpdateDelegate(AtkUnitBase* addonPartyList, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
        private long PartyListUpdateDelegate(long a1, long a2, long a3)
        {
            SimpleLog.Information($"party address:0x{(IntPtr)a1:x}");
            SimpleLog.Information($"data address:0x{(IntPtr)(*(long*)(*(long*)(a2 + 0x20) + 0x20)):x}");
            SimpleLog.Information($"stringarray address:0x{(IntPtr)(*(long*)(*(long*)(a3 + 0x18) + 0x20) + 0x30):x}");
            if ((IntPtr) a1 != l1)
            {
                l1 = (IntPtr) a1;
                l2 = (IntPtr) (*(long*) (*(long*) (a2 + 0x20) + 0x20));
                l3 = (IntPtr) (*(long*) (*(long*) (a3 + 0x18) + 0x20) + 0x30); //+Index*0x68
                party = (PartyUi*) l1;
                data = (DataArray*) l2;
                stringarray = (PartyStrings*)l3;
                if (Config.ShieldShift) ShiftShield();

                //SimpleLog.Information("NewAddress:");
                //SimpleLog.Information("L1:" + l1.ToString("X") + " L2:" + l2.ToString("X"));
                //SimpleLog.Information("L3:" + l3.ToString("X"));
            }
            AtkUnitBase* AtkUnitBase = (AtkUnitBase*)l1;
            AtkTextNode* textNode = null;
            var a = "26558";
            var b = Encoding.UTF8.GetBytes(a);
            for (int i = 0; i < b.Length; i++)
            {
                SimpleLog.Information($"{b[i]:x}");
             }
            //for (uint i = 0; i < 300; i++)
            //{
                //var a = AtkUnitBase->GetTextNodeById(i);
            //    for (uint n = 0; n < AtkUnitBase->UldManager.NodeListCount; n++)
            //    {
            //        if (AtkUnitBase->UldManager.NodeList[n] == null)
            //            continue;
            //        SimpleLog.Information($"NodeID:0x{AtkUnitBase->UldManager.NodeList[n]->NodeID:x}");
            //        textNode = AtkUnitBase->UldManager.NodeList[n]->GetAsAtkTextNode();

            //    if (textNode != null)
            //    {
            //        try
            //        {
            //            AtkUnitBase->UldManager.NodeList[n]->ToggleVisibility(true);
            //            textNode->SetText("CC" + " " + n);
            //            SimpleLog.Information("TextNode" + " " + n);
            //        }
            //        catch (Exception ex)
            //        {
            //            SimpleLog.Error("error" + n);
            //        }
            //    }
            //}
            //}
#if DEBUG
            PerformanceMonitor.Begin("PartyListLayout.Update");

#endif
                UpdatePartyUi(false);
                var ret = partyUiUpdateHook.Original(a1, a2, a3);
                UpdatePartyUi(true);
#if DEBUG
                PerformanceMonitor.End("PartyListLayout.Update");
#endif
            return ret;
        }

        private void TargetUpdateDelegate(long a1, long a2, long a3)
        {
            targetUpdateHook.Original(a1, a2, a3);
            var targetUi = (AtkUnitBase*) a1;
            if (!targetUi->IsVisible) return;
            tTextNode = (AtkTextNode*) targetUi->UldManager.NodeList[39];
            ttTextNode = (AtkTextNode*) targetUi->UldManager.NodeList[49];
            UpdateTarget();
        }

        private void MainTargetUpdateDelegate(long a1, long a2, long a3)
        {
            mainTargetUpdateHook.Original(a1, a2, a3);
            var mainTargetUi = (AtkUnitBase*) a1;
            if (!mainTargetUi->IsVisible) return;
            tTextNode = (AtkTextNode*) mainTargetUi->UldManager.NodeList[8];
            ttTextNode = (AtkTextNode*) mainTargetUi->UldManager.NodeList[12];
            UpdateTarget();
        }

        private long FocusUpdateDelegate(long a1, long a2, long a3)
        {
            var ret = focusUpdateHook.Original(a1, a2, a3);
            focusTextNode = (AtkTextNode*) ((AtkUnitBase*) a1)->UldManager.NodeList[10];
            UpdateFocus();
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
            else if (member.MaxHp == 1)
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
            SimpleLog.Information($"{ptr:x} {count}");
            for (var i = 0; i < count; i++)
            {
                var node = (AtkResNode*) *(long*) (ptr + 8 * i);
                if (node->NodeID == id) return node;
            }

            return null;
        }

        private int GetIndex(string name)
        {
            try
            {
                if (l1 == IntPtr.Zero) return -1;
                if (data->QinXinCount > 0)
                {
                    for (var i = 8; i < 8 + data->QinXinCount; i++)
                    {
                        if (stringarray->MemberStrings(i).GetName() == name) return i;
                    }
                }
                else
                {
                    for (var i = 0; i <data->PlayerCount; i++)
                    {
                        if (stringarray->MemberStrings(i).GetName() == name) return i;
                    }
                }
                return -1;
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
                throw;
            }
        }

        #endregion


        private void ShiftShield()
        {
            if (l1 == IntPtr.Zero) return;
            for (var i = 0; i < 12; i++)
            {
                var hpBarComponentBase = party->Member(i).hpBarComponentBase;
                
                if (hpBarComponentBase == null) return;
                var shieldNode = (AtkNineGridNode*) GetNodeById(hpBarComponentBase, 5);
                var overShieldNode = (AtkImageNode*) GetNodeById(hpBarComponentBase, 2);
                if (shieldNode != null)
                    if (Math.Abs(shieldNode->AtkResNode.OriginY) < 1f)
                    {
                        shieldNode->AtkResNode.OriginY += 8f;
                        *(float*) ((long) shieldNode + 0x6C) += 8f;
                    }

                if (overShieldNode != null)
                    if (Math.Abs(overShieldNode->AtkResNode.OriginY) < 1f)
                    {
                        overShieldNode->AtkResNode.OriginY += 8f;
                        *(float*) ((long) overShieldNode + 0x6C) += 8f;
                    }
            }
        }

        private void UnShiftShield()
        {
            if (l1 == IntPtr.Zero) return;
            for (var i = 0; i < 12; i++)
            {
                var hpBarComponentBase = party->Member(i).hpBarComponentBase;
                if (hpBarComponentBase == null) return;
                var shieldNode = (AtkNineGridNode*) GetNodeById(hpBarComponentBase, 5);
                var overShieldNode = (AtkImageNode*) GetNodeById(hpBarComponentBase, 2);
                if (shieldNode != null)
                    if (Math.Abs(shieldNode->AtkResNode.OriginY - 8f) < 1f)
                    {
                        shieldNode->AtkResNode.OriginY -= 8f;
                        *(float*) ((long) shieldNode + 0x6C) -= 8f;
                    }

                if (overShieldNode != null)
                    if (Math.Abs(overShieldNode->AtkResNode.OriginY - 8f) < 1f)
                    {
                        overShieldNode->AtkResNode.OriginY -= 8f;
                        *(float*) ((long) overShieldNode + 0x6C) -= 8f;
                    }
            }
        }

        private void ShieldOnMp(int index)
        {
            if (l1 == IntPtr.Zero) return;
            var memberData = data->MemberData(index);
            if (memberData.HasMP == 0 ) return;
            var shield = memberData.ShieldPercent * memberData.MaxHp / 100;
            var node1 = (AtkTextNode*) GetNodeById(party->Member(index).mpBarComponentBase, 3);
            var node2 = (AtkTextNode*) GetNodeById(party->Member(index).mpBarComponentBase, 2);
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
                var node1 = (AtkTextNode*) GetNodeById(party->Member(index).mpBarComponentBase, 3);
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
                for (var index = 0; index < 11; index++)
                {
                    if (index >= data->PlayerCount && index < 8 || index >= 8 + data->QinXinCount ) continue;
                    if (!done) //改名
                    {
#if DEBUG
                        if (!Config.PartyName) return;
                        if (index >= 8) return;
                        var lvlname = party->Member(index).nameTextNode->NodeText.StringPtr;
                        var job = data->MemberData(index).JobId;
                        SplitLvlName(lvlname, out var lvl,
                            out var namejob);
                        //SimpleLog.Information($"{lvl}：{namejob}");
                        job = job > 0xF293 ? job - 0xF294 : 0;
                        var nameTextNode = party->Member(index).nameTextNode;
                        if (nameTextNode == null)
                            return;
                        if (namejob != GetJobName(job) ||
                            data->MemberData(index).JobId != party->JobId[index])
                        {
                            nameTextNode->SetText(lvl+" "+ GetJobName(job));
                            //*((byte*)data + 0x1C + index * 0x9C) = 1; //Changed
                        }
#endif
                    }
                    else //改HP
                    {
                        if (Config.HpPercent)
                        {

                            //SimpleLog.Information("111111111111->" + index);
                            //var textNode = GetNodeById(party->Member(index).hpComponentBase, 2);
                            //for (uint i = 0; i < party->Member(index).hpComponentBase->UldManager.NodeListCount; i++)
                            //{
                             var textNode = party->Member(index).HPGaugeComponent->UldManager.SearchNodeById(2)->GetAsAtkTextNode();
                            //party->Member(index).HPGaugeComponent->UldManager.SearchNodeById(2)->Color.A =0xff; //hpvalue
                            //party->Member(index).HPGaugeComponent->UldManager.SearchNodeById(3)->Color.A = 0xff;// unk
                            //party->Member(index).HPGaugeComponent->UldManager.SearchNodeById(4)->Color.A = 0xff;//hpbar
                            if (textNode != null)
                            {
                                SetHp(textNode, data->MemberData(index));
                            }
                            //}//SetHp(textNode, data->MemberData(index));
                        }
                        if (Config.MpShield) ShieldOnMp(index);
                    }
                }
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
                throw;
            }
        }

        private void UpdateTarget()
        {
            try
            {
                if (tTextNode == null || ttTextNode == null) return;
                var tname = Common.ReadSeString(tTextNode->NodeText.StringPtr).TextValue.Trim();
                var ttname = Common.ReadSeString(ttTextNode->NodeText.StringPtr).TextValue.Trim();
                if (tname.Length >= 1)
                {
                    var number = tname.Substring(0, 1);
                    if (PartyNumber.Contains(number)) tname = tname.Substring(1);
                    else SplitString(tname, true, out tname, out _);
                    var index = GetIndex(tname);
                    if (index != -1)
                    {
                        var jobId = data->MemberData(index).JobId;
                        jobId = jobId == 0 ? 0 : jobId - 0xF294;
                        var job = GetJobName(jobId);
                        SetName(tTextNode, PartyNumber.Contains(number) ? number + job : job);
                    }
                }

                if (ttname.Length >= 1)
                {
                    var number = ttname.Substring(0, 1);
                    if (PartyNumber.Contains(number)) ttname = ttname.Substring(1);
                    var index = GetIndex(ttname);
                    if (index != -1)
                    {
                        var jobid = data->MemberData(index).JobId;
                        jobid = jobid == 0 ? 0 : jobid - 0xF294;
                        var job = GetJobName(jobid);
                        SetName(ttTextNode,
                            PartyNumber.Contains(number) ? number + job : job);
                    }
                }
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
            }
        }

        private void UpdateFocus()
        {
            try
            {
                SplitString(Common.ReadSeString(focusTextNode->NodeText.StringPtr).ToString().Trim(), true,
                    out var part1,
                    out var part2);
                if (part2 != "")
                {
                    var number = part2.Substring(0, 1);
                    if (PartyNumber.Contains(number)) part2 = part2.Substring(1);
                    var index = GetIndex(part2);

                    if (index != -1)
                    {
                        var jobId = data->MemberData(index).JobId;
                        jobId = jobId == 0 ? 0 : jobId - 0xF294;
                        var job = GetJobName(jobId);
                        SetName(focusTextNode,
                            PartyNumber.Contains(number)
                                ? part1 + " " + number + job
                                : part1 + " " + job);
                    }
                }
                else if (part1.Length >= 1)
                {
                    if (PartyNumber.Contains(part1.Substring(0, 1)))
                    {
                        var number = part1.Substring(0, 1);
                        part1 = part1.Substring(1);
                        var index = GetIndex(part1);
                        if (index != -1)
                        {
                            var jobId = data->MemberData(index).JobId;
                            jobId = jobId == 0 ? 0 : jobId - 0xF294;
                            var job = GetJobName(jobId);
                            SetName(focusTextNode,
                                number + " " + job);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
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
#endif