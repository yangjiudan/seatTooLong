using SeatTooLong.Core.Settings;

namespace SeatTooLong.Tests;

public class CameraSelectionTests
{
    [Fact]
    public void GetSelectedOptionIndex_ReturnsMatchingEntryForActualCameraIndex()
    {
        var cameras = new[] { "Camera 0", "Camera 4" };

        var selectedIndex = CameraSelection.GetSelectedOptionIndex(cameras, 4);

        Assert.Equal(1, selectedIndex);
    }

    [Fact]
    public void ResolveCameraIndex_ReturnsActualCameraIndexFromSelectedEntry()
    {
        var cameras = new[] { "Camera 0", "Camera 4" };

        var cameraIndex = CameraSelection.ResolveCameraIndex(cameras, 1);

        Assert.Equal(4, cameraIndex);
    }
}