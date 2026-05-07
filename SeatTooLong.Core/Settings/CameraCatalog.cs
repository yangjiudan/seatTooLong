namespace SeatTooLong.Core.Settings;

public sealed class CameraCatalog
{
    private CameraOption[] _cameras = [];

    public IReadOnlyList<CameraOption> Cameras => _cameras;

    public void UpdateKnownCameras(IReadOnlyList<CameraOption> cameras)
    {
        if (cameras.Count == 0)
            return;

        _cameras = cameras.ToArray();
    }

    public int ResolveCameraIndex(string? cameraName, int fallback)
    {
        return CameraSelection.ResolveCameraIndex(_cameras, cameraName, fallback);
    }
}