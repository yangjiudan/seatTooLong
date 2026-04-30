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
}
