using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SimpleTweaksPlugin.Events;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment;

[TweakName("蝰蛇身位提示")]
[TweakDescription("以剑的发光颜色提示蝰蛇的身位,红蓝色块提示下一个要打的3技能(仅限普通量谱模式).")]
[TweakAuthor("wozaiha")]
public unsafe class ViperHint : UiAdjustments.SubTweak
{
    private AddonJobHudRDB0* jobHud;
    private AtkComponentBase* LeftBlade;
    private AtkComponentBase* RightBlade;
    private AtkImageNode* LeftBladeImage;
    private AtkImageNode* RightBladeImage;
    private AtkImageNode* LeftBladeImage2;
    private AtkImageNode* RightBladeImage2;

    private IPlayerCharacter player => Service.ClientState.LocalPlayer;
    
    [AddonPostSetup(["JobHudRDB0"])]
    private void PostSetup()
    {
        jobHud = (AddonJobHudRDB0*)Service.GameGui.GetAddonByName("JobHudRDB0").Address;
        if (jobHud == null) return;
        LeftBlade = jobHud->GaugeStandard.ViperBlades->LeftBlade;
        RightBlade = jobHud->GaugeStandard.ViperBlades->RightBlade;
        LeftBladeImage = LeftBlade->GetImageNodeById(4)->GetAsAtkImageNode();
        RightBladeImage = RightBlade->GetImageNodeById(4)->GetAsAtkImageNode();
        LeftBladeImage2 = LeftBlade->GetImageNodeById(5)->GetAsAtkImageNode();
        RightBladeImage2 = RightBlade->GetImageNodeById(5)->GetAsAtkImageNode();
        SimpleLog.Debug($"Viper JobHud:{(nint)jobHud:X8}");
    }

    [AddonPreDraw(["JobHudRDB0"])]
    private void Update()
    {
        if (jobHud is null || LeftBladeImage is null || RightBladeImage is null || LeftBladeImage2 is null ||
            RightBladeImage2 is null)
        {
            PostSetup();
            return;
        }

        if (player is null) return;

        if (LeftBladeImage->IsVisible())
        {
            UpdateColor(LeftBladeImage);
            UpdateColor(LeftBladeImage2);
        }

        if (RightBladeImage->IsVisible())
        {
            UpdateColor(RightBladeImage);
            UpdateColor(RightBladeImage2);
        }

        if (player!.StatusList.Any(x => x.StatusId is 3645 or 3647 or 3649))
        {
            HideNode(RightBlade);
        }

        if (player!.StatusList.Any(x => x.StatusId is 3646 or 3648 or 3650))
        {
            HideNode(LeftBlade);
        }
    }

    private List<uint> side = [3645, 3646, 3649];
    private List<uint> back = [3647, 3648, 3650];
    // 3668 侧 3669 背


    private void UpdateColor(AtkImageNode* imageNode)
    {
        //Yellow 200 200 50
        if (imageNode->AddRed == 200) return;

        //Red 0 0 0
        if (imageNode->AddRed == 0)
            if (player!.StatusList.Any(x => side.Contains(x.StatusId)) &&
                imageNode->AddRed is not -255)
            {
                imageNode->AddRed = -255;
                imageNode->AddGreen = 150;
                imageNode->AddBlue = 255;
                imageNode->AddRed_2 = -255;
                imageNode->AddGreen_2 = 150;
                imageNode->AddBlue_2 = 255;
            }

        //Blue -255 150 255
        if (imageNode->AddRed == -255)
            if ((player!.StatusList.Any(x => back.Contains(x.StatusId)) //背
                 || player!.StatusList.All(x => !side.Contains(x.StatusId) && !back.Contains(x.StatusId)) //无buff
                 || player!.StatusList.All(x => x.StatusId != 3669))
                && imageNode->AddRed is not 0)
            {
                imageNode->AddRed = 0;
                imageNode->AddGreen = 0;
                imageNode->AddBlue = 0;
                imageNode->AddRed_2 = 0;
                imageNode->AddGreen_2 = 0;
                imageNode->AddBlue_2 = 0;
            }
    }

    private void HideNode(AtkComponentBase* node)
    {
        for (uint i = 6; i < 11; i++)
        {
            var hide = node->UldManager.SearchNodeById(i)->GetAsAtkImageNode();
            hide->Alpha_2 = 0;
        }
    }
}