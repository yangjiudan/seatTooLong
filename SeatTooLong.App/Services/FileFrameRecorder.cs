using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using OpenCvSharp;
using SeatTooLong.Core;

namespace SeatTooLong.App.Services;

public sealed class FileFrameRecorder : IFrameRecorder, IDisposable
{
    private const int DefaultQueueCapacity = 120;
    private const int DefaultJpegQuality = 90;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly BlockingCollection<QueuedFrame> _queue;
    private readonly Task _worker;
    private readonly object _gate = new();
    private readonly int _jpegQuality;
    private string? _currentSessionDirectory;
    private long _frameSequence;
    private bool _disposed;

    public FileFrameRecorder(string recordingRootDirectory, int queueCapacity = DefaultQueueCapacity, int jpegQuality = DefaultJpegQuality)
    {
        RecordingRootDirectory = recordingRootDirectory;
        _jpegQuality = jpegQuality;
        _queue = new BlockingCollection<QueuedFrame>(queueCapacity);
        Directory.CreateDirectory(RecordingRootDirectory);
        _worker = Task.Run(ProcessQueue);
    }

    public string RecordingRootDirectory { get; }
    public string? CurrentSessionDirectory
    {
        get
        {
            lock (_gate)
                return _currentSessionDirectory;
        }
    }

    public bool IsRecording
    {
        get
        {
            lock (_gate)
                return _currentSessionDirectory != null;
        }
    }

    public string StartRecording()
    {
        ThrowIfDisposed();

        lock (_gate)
        {
            if (_currentSessionDirectory != null)
                return _currentSessionDirectory;

            var sessionName = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var sessionDirectory = Path.Combine(RecordingRootDirectory, sessionName);
            Directory.CreateDirectory(sessionDirectory);
            _frameSequence = 0;
            _currentSessionDirectory = sessionDirectory;
            return sessionDirectory;
        }
    }

    public void StopRecording()
    {
        lock (_gate)
            _currentSessionDirectory = null;
    }

    public void RecordFrame(CapturedFrame frame, FrameRecordingMetadata metadata)
    {
        string sessionDirectory;
        long sequence;

        lock (_gate)
        {
            if (_disposed || _currentSessionDirectory == null)
                return;

            sessionDirectory = _currentSessionDirectory;
            sequence = ++_frameSequence;
        }

        var queuedFrame = new QueuedFrame(
            sessionDirectory,
            sequence,
            frame.Data.ToArray(),
            frame.Width,
            frame.Height,
            metadata);

        try
        {
            _queue.TryAdd(queuedFrame);
        }
        catch (InvalidOperationException)
        {
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _currentSessionDirectory = null;
        }

        _queue.CompleteAdding();

        try
        {
            _worker.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }

        _queue.Dispose();
    }

    private void ProcessQueue()
    {
        foreach (var queuedFrame in _queue.GetConsumingEnumerable())
        {
            try
            {
                WriteFrame(queuedFrame);
            }
            catch
            {
            }
        }
    }

    private void WriteFrame(QueuedFrame queuedFrame)
    {
        using var mat = Cv2.ImDecode(queuedFrame.FrameData, ImreadModes.Color);
        if (mat.Empty())
            return;

        var baseName = $"{queuedFrame.Sequence:000000}-{queuedFrame.Metadata.Timestamp:HHmmssfff}";
        var imageFileName = baseName + ".jpg";
        var metadataFileName = baseName + ".json";
        var imagePath = Path.Combine(queuedFrame.SessionDirectory, imageFileName);
        var metadataPath = Path.Combine(queuedFrame.SessionDirectory, metadataFileName);

        Cv2.ImEncode(".jpg", mat, out var jpegBytes, new[] { (int)ImwriteFlags.JpegQuality, _jpegQuality });
        WriteAllBytesAtomically(imagePath, jpegBytes);

        var document = new FrameRecordingDocument(
            imageFileName,
            queuedFrame.Width,
            queuedFrame.Height,
            queuedFrame.Metadata.Timestamp,
            queuedFrame.Metadata.PersonDetected,
            queuedFrame.Metadata.State.ToString(),
            queuedFrame.Metadata.CurrentStateDuration,
            queuedFrame.Metadata.CurrentSittingDuration,
            queuedFrame.Metadata.IsInAbsenceGracePeriod,
            queuedFrame.Metadata.CurrentAbsenceDuration);

        WriteAllTextAtomically(metadataPath, JsonSerializer.Serialize(document, JsonOptions));
    }

    private static void WriteAllBytesAtomically(string path, byte[] bytes)
    {
        var tempPath = path + ".tmp";
        File.WriteAllBytes(tempPath, bytes);
        File.Move(tempPath, path, true);
    }

    private static void WriteAllTextAtomically(string path, string text)
    {
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, text);
        File.Move(tempPath, path, true);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileFrameRecorder));
    }

    private sealed record QueuedFrame(
        string SessionDirectory,
        long Sequence,
        byte[] FrameData,
        int Width,
        int Height,
        FrameRecordingMetadata Metadata);

    private sealed record FrameRecordingDocument(
        string ImageFileName,
        int Width,
        int Height,
        DateTime Timestamp,
        bool PersonDetected,
        string State,
        TimeSpan CurrentStateDuration,
        TimeSpan CurrentSittingDuration,
        bool IsInAbsenceGracePeriod,
        TimeSpan CurrentAbsenceDuration);
}
