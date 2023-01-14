using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Dalamud.Hooking;
using Dalamud.Logging;
using ImGuiNET;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin.Tweaks; 

public unsafe class AutoACT : Tweak {

    public override string Name => "ACT自启动";
    public override string Description => "听说自关闭会炸配置,不是很敢.";
    public class Configs : TweakConfig
    {
        [TweakConfigOption("ACT路径.")]
        public string Path = "";

    }
    public Configs Config { get; private set; }

    protected override DrawConfigDelegate DrawConfigTree => (ref bool changed) =>
    {
        ImGui.Text("请输入ACT目录,如: E:\\ACT ");
        changed |= ImGui.InputText("###Path", ref Config.Path, 200);
        if (changed) SaveConfig(Config);
    };

    public override void Enable()
    {
        Config = LoadConfig<Configs>() ?? new Configs();
        Run();
        base.Enable();
    }

    void Run()
    {
        var startInfo = new ProcessStartInfo();
        startInfo.UseShellExecute = true;
        startInfo.WorkingDirectory = Config.Path;
        if (File.Exists(Config.Path+"\\Advanced Combat Tracker.exe")) startInfo.FileName = Config.Path+"\\Advanced Combat Tracker.exe";
        else if (File.Exists(Config.Path+"\\CafeACT.exe")) startInfo.FileName = Config.Path+"\\CafeACT.exe";
        //startInfo.Verb = "runas";
        try
        {
            if (Process.GetProcessesByName("Advanced Combat Tracker").Length == 0 && Process.GetProcessesByName("CafeACT").Length == 0) Process.Start(startInfo);
        }
        catch
        {
            return;
        }
    }

    void Close()
    {
        //foreach (var pro in Process.GetProcessesByName("Advanced Combat Tracker"))
        //{
        //    pro.CloseMainWindow();
        //    pro.Close();
        //}
    }

    public override void Disable()
    {
        Close();
        base.Disable();
    }

    public override void Dispose() {
        Close();
        base.Dispose();
    }
        
   
}