namespace SeatTooLong.Core;

public record FrameRecordingMetadata(
    DateTime Timestamp,
    bool PersonDetected,
    SittingState State,
    TimeSpan CurrentStateDuration,
    TimeSpan CurrentSittingDuration,
    bool IsInAbsenceGracePeriod,
    TimeSpan CurrentAbsenceDuration);

public interface IFrameRecorder
{
    bool IsRecording { get; }
    void RecordFrame(CapturedFrame frame, FrameRecordingMetadata metadata);
}
