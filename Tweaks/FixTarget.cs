using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using SimpleTweaksPlugin.TweakSystem;
using SimpleTweaksPlugin.Utility;

namespace SimpleTweaksPlugin.Tweaks;

[TweakCategory(TweakCategory.Command)]
[TweakName("Fix '/target' command")]
[TweakDescription("Allows using the default '/target' command for targeting players or NPCs by their names.")]
[Changelog("1.8.3.0", "Fixed tweak not working in french.", Author = "Aireil")]
[Changelog("1.10.12.6", "Add ability to clear target by providing no name.")]
public class FixTarget : Tweak {
    private ReadOnlySeString? TargetNameString => field ??= Service.Data.GetExcelSheet<Addon>().GetRow(3621).Text;
    private IEnumerable<string> TargetCommands => field ??= Service.Data.GetExcelSheet<TextCommand>().GetRow(253).GetCommands();
    
    [LogMessage(3802, 3803)]
    private void OnLogMessage(ILogMessage message) {
        if (!TargetCommands.Contains(Common.LastCommand.FirstWord())) return;
        if (!message.TryGetStringParameter(1, out var type)) return;
        if (!type.Equals(TargetNameString)) return;
        if (message.LogMessageId == 3802) {
            Service.Targets.Target = null;
            Service.Targets.SoftTarget = null;
            message.PreventOriginal();
            return;
        }
        
        if (!message.TryGetStringParameter(2, out var searchNameSeString)) return;
        var searchName = searchNameSeString.ExtractText().Trim();
        IGameObject? closestMatch = null;
        var closestDistance = float.MaxValue;
        var player = Service.Objects.LocalPlayer;
        if (player == null) return;
        foreach (var actor in Service.Objects) {
            if (!actor.IsTargetable || !actor.Name.TextValue.Contains(searchName, System.StringComparison.InvariantCultureIgnoreCase)) continue;
            var distance = Vector3.Distance(player.Position, actor.Position);
            if (closestMatch == null) {
                closestMatch = actor;
                closestDistance = distance;
                continue;
            }

            if (!(closestDistance > distance)) continue;
            closestMatch = actor;
            closestDistance = distance;
        }

        if (closestMatch == null) return;
        message.PreventOriginal();
        Service.Targets.Target = closestMatch;
    }
}
