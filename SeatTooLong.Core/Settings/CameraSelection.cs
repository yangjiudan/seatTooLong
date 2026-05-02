namespace SeatTooLong.Core.Settings;

public sealed record CameraOption(string DeviceName, int DeviceIndex);

public static class CameraSelection
{
    public static int GetSelectedOptionIndex(IReadOnlyList<CameraOption> cameras, string? selectedCameraName, int fallbackCameraIndex = 0)
    {
        if (cameras.Count == 0)
            return -1;

        if (!string.IsNullOrWhiteSpace(selectedCameraName))
        {
            for (int index = 0; index < cameras.Count; index++)
            {
                if (string.Equals(cameras[index].DeviceName, selectedCameraName, StringComparison.Ordinal))
                    return index;
            }
        }

        for (int index = 0; index < cameras.Count; index++)
        {
            if (cameras[index].DeviceIndex == fallbackCameraIndex)
                return index;
        }

        return 0;
    }

    public static string ResolveSelectedCameraName(IReadOnlyList<CameraOption> cameras, int selectedIndex, string fallbackName = "")
    {
        if (selectedIndex < 0 || selectedIndex >= cameras.Count)
            return fallbackName;

        return cameras[selectedIndex].DeviceName;
    }

    public static int ResolveCameraIndex(IReadOnlyList<CameraOption> cameras, string? cameraName, int fallback = 0)
    {
        if (!string.IsNullOrWhiteSpace(cameraName))
        {
            for (int index = 0; index < cameras.Count; index++)
            {
                if (string.Equals(cameras[index].DeviceName, cameraName, StringComparison.Ordinal))
                    return cameras[index].DeviceIndex;
            }
        }

        return fallback;
    }
}