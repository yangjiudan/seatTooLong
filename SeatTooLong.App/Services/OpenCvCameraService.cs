using OpenCvSharp;
using SeatTooLong.Core;

namespace SeatTooLong.App.Services;

public class OpenCvCameraService : ICameraService
{
    private VideoCapture? _capture;

    public bool IsAvailable => _capture != null && _capture.IsOpened();

    public bool Open(int cameraIndex = 0)
    {
        Close();
        _capture = new VideoCapture(cameraIndex);
        return _capture.IsOpened();
    }

    public void Close()
    {
        _capture?.Release();
        _capture?.Dispose();
        _capture = null;
    }

    public CapturedFrame? CaptureFrame()
    {
        if (_capture == null || !_capture.IsOpened())
            return null;

        using var mat = new Mat();
        if (!_capture.Read(mat) || mat.Empty())
            return null;

        var bytes = mat.ToBytes(".bmp");
        return new CapturedFrame(bytes, mat.Width, mat.Height);
    }

    public IReadOnlyList<string> EnumerateCameras()
    {
        var cameras = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            using var cap = new VideoCapture(i);
            if (cap.IsOpened())
            {
                cameras.Add($"Camera {i}");
                cap.Release();
            }
            else
            {
                break;
            }
        }
        return cameras;
    }

    public void Dispose()
    {
        Close();
    }
}
