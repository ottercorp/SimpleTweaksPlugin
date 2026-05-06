using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Chat;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin.Tweaks;

[TweakCategory(TweakCategory.Command)]
[TweakReleaseVersion("1.8.3.0")]
[TweakName("SystemConfig in Group Pose")]
[TweakDescription("Allows the use of the /systemconfig command while in gpose.")]
public unsafe class SystemConfigInGroupPose : Tweak {
    private const uint CommandIsUnavailableAtThisTime = 726;
    private string[] commands = [];
    
    protected override void Enable() {
        var command = Service.Data.GetExcelSheet<TextCommand>().GetRow(168);
        List<ReadOnlySeString> commandList = [command.Command, command.ShortCommand, command.Alias, command.ShortAlias];
        commands = commandList.Select(s => s.ExtractText().Trim()).Where(s => s is ['/', ..]).ToArray();
        Service.Chat.LogMessage += OnLogMessage;
    }

    private void OnLogMessage(ILogMessage message) {
        if (message.LogMessageId != CommandIsUnavailableAtThisTime) return;
        if (!Service.ClientState.IsGPosing) return;
        if (message.TryGetStringParameter(0, out var command)) {
            if (commands?.Contains(command.ExtractText().Trim()) ?? false) {
                var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.Config);
                agent->Show();
                message.PreventOriginal();
            }
        }
    }
    
    protected override void Disable() => Service.Chat.LogMessage -= OnLogMessage;
}
