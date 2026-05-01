using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin.Tweaks;

[TweakName("Customize Group Pose Camera Control")]
[TweakDescription("Allows you to customize the camera control in group pose")]
[TweakAutoConfig]
[TweakReleaseVersion("1.10.4.0")]
[TweakCategory(TweakCategory.QoL)]
public class CustomizeGroupPoseCameraControl : IDisabledTweak {
    public string DisabledMessage => "The primary feature of this tweak has been implemented in the base game. Toggle the 'Enable Roll Angle Correction' option in GPose settings window.";
}
