using SeatTooLong.Core;

namespace SeatTooLong.Core.Statistics;

public static class DailySummaryProjection
{
    public static DailySummary IncludeActiveSitting(DailySummary summary, SittingState currentState, TimeSpan activeSittingDuration)
    {
        if (currentState is not (SittingState.Sitting or SittingState.Alerting) || activeSittingDuration <= TimeSpan.Zero)
            return summary;

        var totalSittingDuration = summary.TotalSittingDuration + activeSittingDuration;
        var longestContinuousSitting = activeSittingDuration > summary.LongestContinuousSitting
            ? activeSittingDuration
            : summary.LongestContinuousSitting;

        return summary with
        {
            TotalSittingDuration = totalSittingDuration,
            LongestContinuousSitting = longestContinuousSitting
        };
    }
}