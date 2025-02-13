﻿using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoSkip.Assets;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.View.Drawable;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Threading;
using Vanara.PInvoke;
using WindowsInput;

namespace BetterGenshinImpact.GameTask.AutoSkip;

/// <summary>
/// 自动剧情有选项点击
/// </summary>
public class AutoSkipTrigger : ITaskTrigger
{
    private readonly ILogger<AutoSkipTrigger> _logger = App.GetLogger<AutoSkipTrigger>();

    public string Name => "自动剧情";
    public bool IsEnabled { get; set; }
    public int Priority => 20;
    public bool IsExclusive => false;

    private readonly AutoSkipAssets _autoSkipAssets;

    public AutoSkipTrigger()
    {
        _autoSkipAssets = new AutoSkipAssets();
    }

    public void Init()
    {
        IsEnabled = TaskContext.Instance().Config.AutoSkipConfig.Enabled;
    }

    /// <summary>
    /// 用于日志只输出一次
    /// frame最好取模,应对极端场景
    /// </summary>
    private int _prevClickFrameIndex = -1;

    private int _prevOtherClickFrameIndex = -1;

    /// <summary>
    /// 上一次播放中的帧
    /// </summary>
    private DateTime _prevPlayingTime = DateTime.MinValue;

    private DateTime _prevExecute = DateTime.MinValue;

    private DateTime _prevGetDailyRewards = DateTime.MinValue;

    public void OnCapture(CaptureContent content)
    {
        if ((DateTime.Now - _prevExecute).TotalMilliseconds <= 200)
        {
            return;
        }

        _prevExecute = DateTime.Now;

        var config = TaskContext.Instance().Config.AutoSkipConfig;
        var assetScale = TaskContext.Instance().SystemInfo.AssetScale;

        GetDailyRewardsEsc(config, content);

        // 找左上角剧情自动的按钮
        using var foundRectArea = content.CaptureRectArea.Find(_autoSkipAssets.StopAutoButtonRo);

        var isPlaying = !foundRectArea.IsEmpty(); // 播放中

        // 播放中图标消失3s内OCR判断文字
        if (!isPlaying && (DateTime.Now - _prevPlayingTime).TotalSeconds <= 3)
        {
            // 找播放中的文字
            content.CaptureRectArea.Find(_autoSkipAssets.PlayingTextRo, _ => { isPlaying = true; });
            if (!isPlaying)
            {
                var textRa = content.CaptureRectArea.Crop(_autoSkipAssets.PlayingTextRo.RegionOfInterest);
                // 过滤出白色
                var hsvFilterMat = OpenCvCommonHelper.InRangeHsv(textRa.SrcMat, new Scalar(0, 0, 170), new Scalar(255, 80, 245));
                var result = OcrFactory.Paddle.Ocr(hsvFilterMat);
                if (result.Contains("播") || result.Contains("番") || result.Contains("放") || result.Contains("中") || result.Contains("潘") || result.Contains("故"))
                {
                    VisionContext.Instance().DrawContent.PutRect("PlayingText", textRa.ConvertRelativePositionToCaptureArea().ToRectDrawable());
                    isPlaying = true;
                }
            }


            if (!isPlaying)
            {
                // 关闭弹出页
                content.CaptureRectArea.Find(_autoSkipAssets.PageCloseRo, pageCloseRoRa =>
                {
                    pageCloseRoRa.ClickCenter();

                    if (Math.Abs(content.FrameIndex - _prevClickFrameIndex) >= 8)
                    {
                        _logger.LogInformation("自动剧情：{Text}", "关闭弹出页");
                    }

                    _prevClickFrameIndex = content.FrameIndex;
                    isPlaying = true;
                    pageCloseRoRa.Dispose();
                });
            }
        }
        else
        {
            VisionContext.Instance().DrawContent.RemoveRect("PlayingText");
        }

        if (isPlaying)
        {
            _prevPlayingTime = DateTime.Now;
            if (TaskContext.Instance().Config.AutoSkipConfig.QuicklySkipConversationsEnabled)
            {
                Simulation.SendInput.Keyboard.KeyPress(VirtualKeyCode.SPACE);
            }

            // 领取每日委托奖励
            if (config.AutoGetDailyRewardsEnabled)
            {
                var dailyRewardIconRa = content.CaptureRectArea.Find(_autoSkipAssets.DailyRewardIconRo);
                if (!dailyRewardIconRa.IsEmpty())
                {
                    var text = GetOrangeOptionText(content.CaptureRectArea.SrcMat, dailyRewardIconRa, (int)(config.ChatOptionTextWidth * assetScale));

                    if (text.Contains("每日委托"))
                    {
                        if (Math.Abs(content.FrameIndex - _prevOtherClickFrameIndex) >= 8)
                        {
                            _logger.LogInformation("自动选择：{Text}", text);
                        }

                        dailyRewardIconRa.ClickCenter();
                        dailyRewardIconRa.Dispose();
                        _prevGetDailyRewards = DateTime.Now; // 记录领取时间
                        return;
                    }

                    _prevOtherClickFrameIndex = content.FrameIndex;
                    dailyRewardIconRa.Dispose();
                }
            }

            // 领取探索派遣奖励
            if (config.AutoReExploreEnabled)
            {
                var exploreIconRa = content.CaptureRectArea.Find(_autoSkipAssets.ExploreIconRo);
                if (!exploreIconRa.IsEmpty())
                {
                    var text = GetOrangeOptionText(content.CaptureRectArea.SrcMat, exploreIconRa, (int)(config.ExpeditionOptionTextWidth * assetScale));
                    if (text.Contains("探索派遣"))
                    {
                        if (Math.Abs(content.FrameIndex - _prevOtherClickFrameIndex) >= 8)
                        {
                            _logger.LogInformation("自动选择：{Text}", text);
                        }

                        exploreIconRa.ClickCenter();

                        // 等待探索派遣界面打开
                        Thread.Sleep(800);
                        new OneKeyExpeditionTask().Run(_autoSkipAssets);
                        exploreIconRa.Dispose();
                        return;
                    }

                    _prevOtherClickFrameIndex = content.FrameIndex;
                    exploreIconRa.Dispose();
                    return;
                }
            }

            // 找右下的对话选项按钮
            content.CaptureRectArea.Find(_autoSkipAssets.OptionIconRo, (optionButtonRectArea) =>
            {
                TaskControl.Sleep(config.AfterChooseOptionSleepDelay);
                optionButtonRectArea.ClickCenter();

                if (Math.Abs(content.FrameIndex - _prevClickFrameIndex) >= 8)
                {
                    _logger.LogInformation("自动剧情：{Text}", "点击选项");
                }

                _prevClickFrameIndex = content.FrameIndex;
                optionButtonRectArea.Dispose();
            });
        }
        else
        {
            // 黑屏剧情要点击鼠标（多次） 几乎全黑的时候不用点击
            using var grayMat = new Mat(content.CaptureRectArea.SrcGreyMat, new Rect(0, content.CaptureRectArea.SrcGreyMat.Height / 3, content.CaptureRectArea.SrcGreyMat.Width, content.CaptureRectArea.SrcGreyMat.Height / 3));
            var blackCount = OpenCvCommonHelper.CountGrayMatColor(grayMat, 0);
            var rate = blackCount * 1d / (grayMat.Width * grayMat.Height);
            if (rate is >= 0.5 and < 0.999)
            {
                Simulation.SendInput.Mouse.LeftButtonClick();
                if (Math.Abs(content.FrameIndex - _prevClickFrameIndex) >= 8)
                {
                    _logger.LogInformation("自动剧情：{Text} 比例 {Rate}", "点击黑屏", rate.ToString("F"));
                }

                _prevClickFrameIndex = content.FrameIndex;
            }

            // TODO 自动交付材料
        }
    }

    /// <summary>
    /// 获取橙色选项的文字
    /// </summary>
    /// <param name="captureMat"></param>
    /// <param name="foundIconRectArea"></param>
    /// <param name="chatOptionTextWidth"></param>
    /// <returns></returns>
    private string GetOrangeOptionText(Mat captureMat, RectArea foundIconRectArea, int chatOptionTextWidth)
    {
        var textRect = new Rect(foundIconRectArea.X + foundIconRectArea.Width, foundIconRectArea.Y, chatOptionTextWidth, foundIconRectArea.Height);
        using var mat = new Mat(captureMat, textRect);
        // 只提取橙色
        using var bMat = OpenCvCommonHelper.Threshold(mat, new Scalar(247, 198, 50), new Scalar(255, 204, 54));
        // Cv2.ImWrite("log/每日委托.png", bMat);
        var whiteCount = OpenCvCommonHelper.CountGrayMatColor(bMat, 255);
        var rate = whiteCount * 1.0 / (bMat.Width * bMat.Height);
        if (rate < 0.06)
        {
            Debug.WriteLine($"识别到橙色文字区域占比:{rate}");
            return string.Empty;
        }

        var text = OcrFactory.Paddle.Ocr(bMat);
        return text;
    }

    /// <summary>
    /// 领取每日委托奖励 后 10s 寻找原石是否出现，出现则按下esc
    /// </summary>
    private void GetDailyRewardsEsc(AutoSkipConfig config, CaptureContent content)
    {
        if (!config.AutoGetDailyRewardsEnabled)
        {
            return;
        }

        if ((DateTime.Now - _prevGetDailyRewards).TotalSeconds > 10)
        {
            return;
        }

        content.CaptureRectArea.Find(_autoSkipAssets.PrimogemRo, primogemRa =>
        {
            Thread.Sleep(100);
            Simulation.SendInputEx.Keyboard.KeyPress(User32.VK.VK_ESCAPE);
            _prevGetDailyRewards = DateTime.MinValue;
            primogemRa.Dispose();
        });
    }
}