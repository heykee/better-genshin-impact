﻿using System;
using System.Collections.Generic;
using BetterGenshinImpact.Core.Recognition;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.AutoSkip.Assets;

public class AutoSkipAssets
{
    public RecognitionObject StopAutoButtonRo;
    public RecognitionObject PlayingTextRo;
    public RecognitionObject MenuRo;

    public RecognitionObject OptionIconRo;
    public RecognitionObject DailyRewardIconRo;
    public RecognitionObject ExploreIconRo;

    public RecognitionObject PageCloseRo;

    public RecognitionObject CollectRo;
    public RecognitionObject ReRo;

    public RecognitionObject PrimogemRo;

    //public Mat BinaryStopAutoButtonMat;

    public AutoSkipAssets()
    {
        var info = TaskContext.Instance().SystemInfo;
        StopAutoButtonRo = new RecognitionObject
        {
            Name = "StopAutoButton",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoSkip", "stop_auto.png"),
            RegionOfInterest = new Rect(0, 0, info.CaptureAreaRect.Width / 5, info.CaptureAreaRect.Height / 8),
            DrawOnWindow = true
        }.InitTemplate();

        // 二值化的跳过剧情按钮
        //if (StopAutoButtonRo.TemplateImageGreyMat == null)
        //{
        //    throw new Exception("StopAutoButtonRo.TemplateImageGreyMat == null");
        //}
        //BinaryStopAutoButtonMat = StopAutoButtonRo.TemplateImageGreyMat.Clone();


        //Cv2.Threshold(BinaryStopAutoButtonMat, BinaryStopAutoButtonMat, 0, 255, ThresholdTypes.BinaryInv);

        PlayingTextRo = new RecognitionObject
        {
            Name = "PlayingText",
            RecognitionType = RecognitionTypes.Ocr,
            RegionOfInterest = new Rect((int)(100 * info.AssetScale), (int)(35 * info.AssetScale), (int)(85 * info.AssetScale), (int)(35 * info.AssetScale)),
            OneContainMatchText = new List<string>
            {
                "播", "番", "放", "中"
            },
            DrawOnWindow = true
        }.InitTemplate();

        OptionIconRo = new RecognitionObject
        {
            Name = "OptionIcon",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoSkip", "icon_option.png"),
            RegionOfInterest = new Rect(info.CaptureAreaRect.Width / 2, 0, info.CaptureAreaRect.Width - info.CaptureAreaRect.Width / 2, info.CaptureAreaRect.Height),
            DrawOnWindow = false
        }.InitTemplate();
        DailyRewardIconRo = new RecognitionObject
        {
            Name = "DailyRewardIcon",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoSkip", "icon_daily_reward.png"),
            RegionOfInterest = new Rect(info.CaptureAreaRect.Width / 2, 0, info.CaptureAreaRect.Width - info.CaptureAreaRect.Width / 2, info.CaptureAreaRect.Height),
            DrawOnWindow = false
        }.InitTemplate();
        ExploreIconRo = new RecognitionObject
        {
            Name = "ExploreIcon",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoSkip", "icon_explore.png"),
            RegionOfInterest = new Rect(info.CaptureAreaRect.Width / 2, 0, info.CaptureAreaRect.Width - info.CaptureAreaRect.Width / 2, info.CaptureAreaRect.Height),
            DrawOnWindow = false
        }.InitTemplate();

        MenuRo = new RecognitionObject
        {
            Name = "Menu",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoSkip", "menu.png"),
            RegionOfInterest = new Rect(0, 0, info.CaptureAreaRect.Width / 4, info.CaptureAreaRect.Height / 4),
            DrawOnWindow = false
        }.InitTemplate();

        PageCloseRo = new RecognitionObject
        {
            Name = "PageClose",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoSkip", "page_close.png"),
            RegionOfInterest = new Rect(info.CaptureAreaRect.Width - info.CaptureAreaRect.Width / 8 , 0, info.CaptureAreaRect.Width / 8, info.CaptureAreaRect.Height / 8),
            DrawOnWindow = true
        }.InitTemplate();

        // 一键派遣
        CollectRo = new RecognitionObject
        {
            Name = "Collect",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoSkip", "collect.png"),
            RegionOfInterest = new Rect(0, info.CaptureAreaRect.Height - info.CaptureAreaRect.Height / 3, info.CaptureAreaRect.Width / 4, info.CaptureAreaRect.Height / 3),
            DrawOnWindow = false
        }.InitTemplate();
        ReRo = new RecognitionObject
        {
            Name = "Re",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoSkip", "re.png"),
            RegionOfInterest = new Rect(info.CaptureAreaRect.Width/2, info.CaptureAreaRect.Height - info.CaptureAreaRect.Height / 4, info.CaptureAreaRect.Width / 4, info.CaptureAreaRect.Height / 4),
            DrawOnWindow = false
        }.InitTemplate();

        // 每日奖励
        PrimogemRo = new RecognitionObject
        {
            Name = "Primogem",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoSkip", "primogem.png"),
            RegionOfInterest = new Rect(0, info.CaptureAreaRect.Height / 3, info.CaptureAreaRect.Width, info.CaptureAreaRect.Height / 3),
            DrawOnWindow = false
        }.InitTemplate();

        // 更多对话要素
    }
}