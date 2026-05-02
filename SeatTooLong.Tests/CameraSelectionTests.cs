using SeatTooLong.Core.Settings;

namespace SeatTooLong.Tests;

public class CameraSelectionTests
{
    [Fact]
    public void GetSelectedOptionIndex_ReturnsMatchingEntryForCameraName()
    {
        var cameras = new[]
        {
            new CameraOption("Integrated Camera", 0),
            new CameraOption("USB Camera", 4)
        };

        var selectedIndex = CameraSelection.GetSelectedOptionIndex(cameras, "USB Camera", fallbackCameraIndex: 0);

        Assert.Equal(1, selectedIndex);
    }

    [Fact]
    public void GetSelectedOptionIndex_WhenCameraNameMissing_UsesFallbackIndex()
    {
        var cameras = new[]
        {
            new CameraOption("Integrated Camera", 0),
            new CameraOption("USB Camera", 4)
        };

        var selectedIndex = CameraSelection.GetSelectedOptionIndex(cameras, "Missing Camera", fallbackCameraIndex: 4);

        Assert.Equal(1, selectedIndex);
    }

    [Fact]
    public void ResolveCameraIndex_ReturnsActualCameraIndexFromCameraName()
    {
        var cameras = new[]
        {
            new CameraOption("Integrated Camera", 0),
            new CameraOption("USB Camera", 4)
        };

        var cameraIndex = CameraSelection.ResolveCameraIndex(cameras, "USB Camera", fallback: 0);

        Assert.Equal(4, cameraIndex);
    }

    [Fact]
    public void ResolveSelectedCameraName_ReturnsSelectedCameraName()
    {
        var cameras = new[]
        {
            new CameraOption("Integrated Camera", 0),
            new CameraOption("USB Camera", 4)
        };

        var selectedName = CameraSelection.ResolveSelectedCameraName(cameras, selectedIndex: 1);

        Assert.Equal("USB Camera", selectedName);
    }

    [Fact]
    public void ResolveSelectedCameraName_WhenIndexOutOfRange_ReturnsFallback()
    {
        var cameras = new[]
        {
            new CameraOption("Integrated Camera", 0)
        };

        var selectedName = CameraSelection.ResolveSelectedCameraName(cameras, selectedIndex: 99, fallbackName: "Integrated Camera");

        Assert.Equal("Integrated Camera", selectedName);
    }
}