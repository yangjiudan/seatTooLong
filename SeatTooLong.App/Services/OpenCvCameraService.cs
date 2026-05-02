using OpenCvSharp;
using SeatTooLong.Core;
using SeatTooLong.Core.Settings;
using Windows.Devices.Enumeration;

namespace SeatTooLong.App.Services;

public enum CameraFailureKind
{
    None,
    OpenFailed,
    ReadFailed
}

public class OpenCvCameraService : ICameraService
{
    private const int MaxCameraProbeCount = 5;
    private static readonly VideoCaptureAPIs[] CandidateBackends =
    [
        VideoCaptureAPIs.DSHOW,
        VideoCaptureAPIs.MSMF,
        VideoCaptureAPIs.ANY
    ];

    private readonly object _gate = new();
    private VideoCapture? _capture;
    private int _preferredCameraIndex;

    public CameraFailureKind LastFailure { get; private set; }

    public bool IsAvailable
    {
        get
        {
            lock (_gate)
                return _capture != null && _capture.IsOpened();
        }
    }

    public bool Open(int cameraIndex = 0)
    {
        lock (_gate)
        {
            _preferredCameraIndex = cameraIndex;
            return ReopenCore();
        }
    }

    public void Close()
    {
        lock (_gate)
            CloseCore();
    }

    public CapturedFrame? CaptureFrame()
    {
        lock (_gate)
        {
            if (!EnsureCaptureAvailable())
                return null;

            if (TryCaptureFrame(out var frame))
                return frame;

            if (!ReopenCore())
                return null;

            return TryCaptureFrame(out frame)
                ? frame
                : null;
        }
    }

    public IReadOnlyList<CameraOption> EnumerateCameras()
    {
        var deviceNames = GetVideoCaptureDeviceNames();

        foreach (var backend in CandidateBackends)
        {
            var cameraIndices = GetAvailableCameraIndices(backend);
            if (cameraIndices.Count > 0)
                return BuildCameraOptions(cameraIndices, deviceNames);
        }

        return [];
    }

    public void Dispose()
    {
        Close();
    }

    private void CloseCore()
    {
        _capture?.Release();
        _capture?.Dispose();
        _capture = null;
    }

    private bool EnsureCaptureAvailable()
    {
        return (_capture != null && _capture.IsOpened()) || ReopenCore();
    }

    private bool ReopenCore()
    {
        foreach (var cameraIndex in GetCandidateCameraIndices(_preferredCameraIndex))
        {
            foreach (var backend in CandidateBackends)
            {
                if (TryOpenCapture(cameraIndex, backend))
                    return true;
            }
        }

        CloseCore();
        LastFailure = CameraFailureKind.OpenFailed;
        return false;
    }

    private bool TryCaptureFrame(out CapturedFrame? frame)
    {
        frame = null;
        if (_capture == null || !_capture.IsOpened())
            return false;

        using var mat = new Mat();
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (_capture.Read(mat) && !mat.Empty())
            {
                var bytes = mat.ToBytes(".bmp");
                LastFailure = CameraFailureKind.None;
                frame = new CapturedFrame(bytes, mat.Width, mat.Height);
                return true;
            }
        }

        LastFailure = CameraFailureKind.ReadFailed;
        return false;
    }

    private bool TryOpenCapture(int cameraIndex, VideoCaptureAPIs backend)
    {
        CloseCore();

        var capture = backend == VideoCaptureAPIs.ANY
            ? new VideoCapture(cameraIndex)
            : new VideoCapture(cameraIndex, backend);

        if (!capture.IsOpened())
        {
            capture.Release();
            capture.Dispose();
            return false;
        }

        _capture = capture;
        LastFailure = CameraFailureKind.None;
        return true;
    }

    private static bool CanOpenCamera(int cameraIndex, VideoCaptureAPIs backend)
    {
        using var capture = backend == VideoCaptureAPIs.ANY
            ? new VideoCapture(cameraIndex)
            : new VideoCapture(cameraIndex, backend);

        if (!capture.IsOpened())
            return false;

        capture.Release();
        return true;
    }

    private static List<int> GetAvailableCameraIndices(VideoCaptureAPIs backend)
    {
        var cameraIndices = new List<int>();
        for (int cameraIndex = 0; cameraIndex < MaxCameraProbeCount; cameraIndex++)
        {
            if (CanOpenCamera(cameraIndex, backend))
                cameraIndices.Add(cameraIndex);
        }

        return cameraIndices;
    }

    private static string GetCameraName(int cameraIndex, IReadOnlyList<string> deviceNames)
    {
        if (cameraIndex >= 0 && cameraIndex < deviceNames.Count && !string.IsNullOrWhiteSpace(deviceNames[cameraIndex]))
            return deviceNames[cameraIndex];

        return $"Camera {cameraIndex}";
    }

    private static IReadOnlyList<string> GetVideoCaptureDeviceNames()
    {
        try
        {
            var devices = DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return devices
                .Select(static device => string.IsNullOrWhiteSpace(device.Name) ? "Unknown Camera" : device.Name)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<CameraOption> BuildCameraOptions(IReadOnlyList<int> cameraIndices, IReadOnlyList<string> deviceNames)
    {
        if (deviceNames.Count == 0)
            return cameraIndices.Select(index => new CameraOption($"Camera {index}", index)).ToList();

        var alignedIndices = cameraIndices
            .Where(index => index >= 0 && index < deviceNames.Count)
            .ToList();

        // OpenCV can expose alias indices for one physical device (e.g. 0 and 4).
        // When device names are available, prefer one item per known device.
        if (alignedIndices.Count > 0)
        {
            return alignedIndices
                .Select(index => new CameraOption(GetCameraName(index, deviceNames), index))
                .ToList();
        }

        var mappedCount = Math.Min(cameraIndices.Count, deviceNames.Count);
        var mapped = new List<CameraOption>(mappedCount);
        for (int index = 0; index < mappedCount; index++)
        {
            var cameraIndex = cameraIndices[index];
            var cameraName = string.IsNullOrWhiteSpace(deviceNames[index])
                ? $"Camera {cameraIndex}"
                : deviceNames[index];
            mapped.Add(new CameraOption(cameraName, cameraIndex));
        }

        return mapped;
    }

    private static IEnumerable<int> GetCandidateCameraIndices(int preferredIndex)
    {
        if (preferredIndex >= 0 && preferredIndex < MaxCameraProbeCount)
            yield return preferredIndex;

        for (int cameraIndex = 0; cameraIndex < MaxCameraProbeCount; cameraIndex++)
        {
            if (cameraIndex != preferredIndex)
                yield return cameraIndex;
        }
    }
}
