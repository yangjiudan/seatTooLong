namespace SeatTooLong.Core;

public record CapturedFrame(byte[] Data, int Width, int Height);

public interface ICameraService : IDisposable
{
    bool IsAvailable { get; }
    bool Open(int cameraIndex = 0);
    void Close();
    CapturedFrame? CaptureFrame();
    IReadOnlyList<string> EnumerateCameras();
}
