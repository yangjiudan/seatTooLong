namespace SeatTooLong.Core.Settings;

public static class CameraSelection
{
    public static int GetSelectedOptionIndex(IReadOnlyList<string> cameras, int actualCameraIndex)
    {
        if (cameras.Count == 0)
            return -1;

        for (int index = 0; index < cameras.Count; index++)
        {
            if (TryParseCameraIndex(cameras[index], out var cameraIndex) && cameraIndex == actualCameraIndex)
                return index;
        }

        return 0;
    }

    public static int ResolveCameraIndex(IReadOnlyList<string> cameras, int selectedIndex, int fallback = 0)
    {
        if (selectedIndex < 0 || selectedIndex >= cameras.Count)
            return fallback;

        return TryParseCameraIndex(cameras[selectedIndex], out var cameraIndex)
            ? cameraIndex
            : fallback;
    }

    private static bool TryParseCameraIndex(string? cameraLabel, out int cameraIndex)
    {
        cameraIndex = 0;
        if (string.IsNullOrWhiteSpace(cameraLabel))
            return false;

        var lastSpaceIndex = cameraLabel.LastIndexOf(' ');
        var numericPart = lastSpaceIndex >= 0
            ? cameraLabel[(lastSpaceIndex + 1)..]
            : cameraLabel;

        return int.TryParse(numericPart, out cameraIndex);
    }
}