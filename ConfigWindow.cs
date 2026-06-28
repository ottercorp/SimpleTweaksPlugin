using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using SimpleTweaksPlugin.Utility;

namespace SimpleTweaksPlugin;

public class ConfigWindow : SimpleWindow {
    public ConfigWindow() : base("Simple Tweaks Config") {
        Size = new Vector2(600, 400);
        SizeConstraints = new WindowSizeConstraints() { MinimumSize = new Vector2(600, 200), MaximumSize = new Vector2(float.MaxValue, float.MaxValue), };
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    private DateTime? easterDate;

    private DateTime EasterDate {
        get {
            if (easterDate != null) return easterDate.Value;
            var year = DateTime.Now.Year + 1;
            var a = year % 19;
            var b = year / 100;
            var c = (b - (b / 4) - ((8 * b + 13) / 25) + (19 * a) + 15) % 30;
            var d = c - (c / 28) * (1 - (c / 28) * (29 / (c + 1)) * ((21 - a) / 11));
            var e = d - ((year + (year / 4) + d + 2 - b + (b / 4)) % 7);
            var month = 3 + ((e + 40) / 44);
            var day = e + 28 - (31 * (month / 4));
            easterDate = new DateTime(year, month, day);
            return easterDate.Value;
        }
    }

    private DecorationType? randomDecorationType;

    public void FestiveDecorations() {
        if (SimpleTweaksPlugin.Plugin.PluginConfig.FestiveDecorationType == DecorationType.None) return;
        var dl = ImGui.GetWindowDrawList();
        var currentDate = DateTime.Now;
        var textures = new List<IDalamudTextureWrap?>();
        var decorationType = SimpleTweaksPlugin.Plugin.PluginConfig.FestiveDecorationType;
        if (decorationType == DecorationType.Random) {
            randomDecorationType ??= (DecorationType)new Random().Next(0, Enum.GetValues<DecorationType>().Max(v => (int)v) + 1);
            decorationType = randomDecorationType.Value;
        }

        switch (decorationType) {
            case DecorationType.Easter:
            case DecorationType.Auto when currentDate > EasterDate.AddDays(-4) && EasterDate < EasterDate.AddDays(4):
                // Easter
                textures.Add(Service.TextureProvider.GetFromGame("ui/icon/080000/080110_hr1.tex").GetWrapOrDefault());
                textures.Add(Service.TextureProvider.GetFromGame("ui/icon/080000/080131_hr1.tex").GetWrapOrDefault());
                break;
            case DecorationType.Christmas:
            case DecorationType.Auto when currentDate is { Month: 12, Day: >= 20 and <= 28 }:
                textures.Add(Service.TextureProvider.GetFromGame("ui/icon/080000/080106_hr1.tex").GetWrapOrDefault());
                var hat = Service.TextureProvider.GetFromFile(new FileInfo(Path.Join(Service.PluginInterface.AssemblyLocation.Directory!.FullName, "Decorations", "xmashat.png"))).GetWrapOrDefault();
                if (hat != null) {
                    var dl2 = IsFocused ? ImGui.GetForegroundDrawList() : ImGui.GetBackgroundDrawList();
                    var hatPos = ImGui.GetWindowPos() - hat.Size * new Vector2(0.35f, 0.35f);
                    dl.AddImage(hat.Handle, hatPos, hatPos + hat.Size, Vector2.Zero, Vector2.One, 0xAAFFFFFF);
                    dl2.AddImage(hat.Handle, hatPos, hatPos + hat.Size, Vector2.Zero, Vector2.One, 0xAAFFFFFF);
                }

                break;
            case DecorationType.Valentines:
            case DecorationType.Auto when currentDate is { Month: 2, Day: >= 13 and <= 15 }:
                textures.Add(Service.TextureProvider.GetFromGame("ui/icon/080000/080108_hr1.tex").GetWrapOrDefault());
                textures.Add(Service.TextureProvider.GetFromGame("ui/icon/080000/080126_hr1.tex").GetWrapOrDefault());
                break;
            case DecorationType.Halloween:
            case DecorationType.Auto when currentDate is { Month: 10, Day: >= 30 }:
                textures.Add(Service.TextureProvider.GetFromGame("ui/icon/080000/080103_hr1.tex").GetWrapOrDefault());
                break;
            case DecorationType.None:
            case DecorationType.Random:
            default:
                return;
        }

        textures.RemoveAll(t => t == null);
        if (textures.Count == 0) return;

        var width = textures.Max(s => s?.Size.X ?? 0);
        var height = textures.Max(s => s?.Size.Y ?? 0);
        var size = new Vector2(width, height) / 3 * ImGuiHelpers.GlobalScale;
        var center = ImGui.GetWindowPos() + ((ImGui.GetWindowSize() / 2) * Vector2.UnitX) + (ImGui.GetWindowSize() * Vector2.UnitY);
        var p = center - (size * Vector2.UnitX / 2) - (size * Vector2.UnitY * 0.85f);

        for (var i = 0; i < Math.Ceiling((ImGui.GetWindowSize() / 2).X) + 1; i++) {
            var texture = textures[i % textures.Count];
            if (texture == null || texture.Handle == IntPtr.Zero) continue;
            if (i != 0) {
                var p1 = p - (size * Vector2.UnitX) * i;
                dl.AddImage(texture.Handle, p1, p1 + size, Vector2.Zero, Vector2.One, 0x40FFFFFF);

                var p2 = p + (size * Vector2.UnitX) * i;
                dl.AddImage(texture.Handle, p2, p2 + size, Vector2.Zero, Vector2.One, 0x40FFFFFF);
            } else {
                dl.AddImage(texture.Handle, p, p + size, Vector2.Zero, Vector2.One, 0x40FFFFFF);
            }
        }
    }

    public override void Draw() {
        base.Draw();
#if !TEST
        FestiveDecorations();
        SimpleTweaksPlugin.Plugin.PluginConfig.DrawConfigUI();
#else
        if (ImGui.BeginTabBar("testBar")) {
            if (ImGui.BeginTabItem("Test Runner")) {
                TestUtil.Draw();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Config")) {
                SimpleTweaksPlugin.Plugin.PluginConfig.DrawConfigUI();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
        
#endif
    }

    public override void OnClose() {
        base.OnClose();
        randomDecorationType = null;
        SimpleTweaksPlugin.Plugin.SaveAllConfig();
        SimpleTweaksPlugin.Plugin.PluginConfig.ClearSearch();
    }
}
