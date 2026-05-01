using SeatTooLong.Core.Settings;

namespace SeatTooLong.Tests;

public class JsonSettingsServiceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly JsonSettingsService _service;

    public JsonSettingsServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"seattoolong_test_{Guid.NewGuid()}.json");
        _service = new JsonSettingsService(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    [Fact]
    public void Load_WhenNoFile_ReturnsDefaults()
    {
        var settings = _service.Load();

        Assert.Equal(45, settings.SitThresholdMinutes);
        Assert.Equal(5, settings.RestDurationMinutes);
        Assert.Equal(2, settings.DetectionIntervalSeconds);
        Assert.Equal(2, settings.AbsenceGracePeriodSeconds);
        Assert.True(settings.AutoStart);
        Assert.Equal("auto", settings.Language);
        Assert.True(settings.ShowOverlay);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var settings = new AppSettings
        {
            SitThresholdMinutes = 30,
            RestDurationMinutes = 10,
            DetectionIntervalSeconds = 3,
            AbsenceGracePeriodSeconds = 12,
            CameraIndex = 2,
            AutoStart = false,
            Language = "en",
            ShowOverlay = false,
            OverlayOpacity = 0.5
        };

        _service.Save(settings);
        var loaded = _service.Load();

        Assert.Equal(30, loaded.SitThresholdMinutes);
        Assert.Equal(10, loaded.RestDurationMinutes);
        Assert.Equal(3, loaded.DetectionIntervalSeconds);
        Assert.Equal(12, loaded.AbsenceGracePeriodSeconds);
        Assert.Equal(2, loaded.CameraIndex);
        Assert.False(loaded.AutoStart);
        Assert.Equal("en", loaded.Language);
        Assert.False(loaded.ShowOverlay);
        Assert.Equal(0.5, loaded.OverlayOpacity);
    }

    [Fact]
    public void Save_CreatesFile()
    {
        _service.Save(new AppSettings());
        Assert.True(File.Exists(_tempFile));
    }
}
