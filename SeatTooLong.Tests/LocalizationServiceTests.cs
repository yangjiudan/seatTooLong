using SeatTooLong.Core.Localization;

namespace SeatTooLong.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void SetLanguage_Zh_ReturnsChineseStrings()
    {
        var svc = new LocalizationService("zh");
        Assert.Equal("zh", svc.CurrentLanguage);
        Assert.Equal("设置", svc.Get("settings.title"));
        Assert.Equal("退出", svc.Get("tray.exit"));
        Assert.Equal("开始录制素材", svc.Get("tray.record_start"));
    }

    [Fact]
    public void SetLanguage_En_ReturnsEnglishStrings()
    {
        var svc = new LocalizationService("en");
        Assert.Equal("en", svc.CurrentLanguage);
        Assert.Equal("Settings", svc.Get("settings.title"));
        Assert.Equal("Exit", svc.Get("tray.exit"));
        Assert.Equal("Start Recording Samples", svc.Get("tray.record_start"));
    }

    [Fact]
    public void Get_UnknownKey_ReturnsKeyItself()
    {
        var svc = new LocalizationService("zh");
        Assert.Equal("unknown.key", svc.Get("unknown.key"));
    }

    [Fact]
    public void SetLanguage_CanSwitchAtRuntime()
    {
        var svc = new LocalizationService("zh");
        Assert.Equal("设置", svc.Get("settings.title"));

        svc.SetLanguage("en");
        Assert.Equal("Settings", svc.Get("settings.title"));
    }

    [Fact]
    public void NotifyStrings_ContainPlaceholders()
    {
        var svc = new LocalizationService("zh");
        var template = svc.Get("notify.sit_too_long.title");
        Assert.Contains("{0}", template);
    }

    [Fact]
    public void CameraIssueStrings_AreLocalized()
    {
        var zh = new LocalizationService("zh");
        var en = new LocalizationService("en");

        Assert.Equal("摄像头异常", zh.Get("overlay.camera_issue"));
        Assert.Equal("Camera issue", en.Get("overlay.camera_issue"));
        Assert.Contains("摄像头", zh.Get("tray.tooltip.camera_issue"));
        Assert.Contains("Camera", en.Get("tray.tooltip.camera_issue"));
    }

    [Fact]
    public void PreviewStrings_AreLocalized()
    {
        var zh = new LocalizationService("zh");
        var en = new LocalizationService("en");

        Assert.Equal("预览当前摄像头", zh.Get("tray.preview"));
        Assert.Equal("Preview Camera", en.Get("tray.preview"));
        Assert.Equal("当前摄像头预览", zh.Get("camera.preview.title"));
        Assert.Equal("Current Camera Preview", en.Get("camera.preview.title"));
    }

    [Fact]
    public void AboutStrings_AreLocalized()
    {
        var zh = new LocalizationService("zh");
        var en = new LocalizationService("en");

        Assert.Equal("关于", zh.Get("tray.about"));
        Assert.Equal("About", en.Get("tray.about"));
        Assert.Contains("{0}", zh.Get("about.version"));
        Assert.Contains("{1}", zh.Get("about.version"));
        Assert.Contains("{0}", en.Get("about.version"));
        Assert.Contains("{1}", en.Get("about.version"));
        Assert.Equal("关闭", zh.Get("about.close"));
        Assert.Equal("Close", en.Get("about.close"));
    }
}
