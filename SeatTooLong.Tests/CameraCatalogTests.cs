using SeatTooLong.Core.Settings;

namespace SeatTooLong.Tests;

public class CameraCatalogTests
{
    [Fact]
    public void UpdateKnownCameras_WhenSnapshotHasEntries_ReplacesCatalog()
    {
        var catalog = new CameraCatalog();
        var cameras = new[]
        {
            new CameraOption("Integrated Camera", 0),
            new CameraOption("USB Camera", 4)
        };

        catalog.UpdateKnownCameras(cameras);

        Assert.Equal(cameras, catalog.Cameras);
    }

    [Fact]
    public void UpdateKnownCameras_WhenSnapshotIsEmpty_KeepsPreviousKnownCameras()
    {
        var catalog = new CameraCatalog();
        var knownCameras = new[]
        {
            new CameraOption("Integrated Camera", 0),
            new CameraOption("USB Camera", 4)
        };

        catalog.UpdateKnownCameras(knownCameras);
        catalog.UpdateKnownCameras([]);

        Assert.Equal(knownCameras, catalog.Cameras);
    }

    [Fact]
    public void ResolveCameraIndex_UsesKnownCameras()
    {
        var catalog = new CameraCatalog();
        catalog.UpdateKnownCameras(
        [
            new CameraOption("Integrated Camera", 0),
            new CameraOption("USB Camera", 4)
        ]);

        var cameraIndex = catalog.ResolveCameraIndex("USB Camera", fallback: 0);

        Assert.Equal(4, cameraIndex);
    }
}