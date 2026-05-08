using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Chat;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using SimpleTweaksPlugin.TweakSystem;
using SimpleTweaksPlugin.Utility;

namespace SimpleTweaksPlugin.Tweaks;

[TweakCategory(TweakCategory.Command)]
[TweakReleaseVersion("1.8.3.0")]
[TweakName("SystemConfig in Group Pose")]
[TweakDescription("Allows the use of the /systemconfig command while in gpose.")]
public unsafe class SystemConfigInGroupPose : Tweak {
    private IEnumerable<string> Commands => field ??= Service.Data.GetExcelSheet<TextCommand>().GetRow(168).GetCommands();
    
    [LogMessage(726)]
    private void OnLogMessage(ILogMessage message) {
        if (!Service.ClientState.IsGPosing) return;
        if (message.TryGetStringParameter(0, out var command)) {
            if (Commands.Contains(command.ExtractText())) {
                var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.Config);
                agent->Show();
                message.PreventOriginal();
            }
        }
    }
}
