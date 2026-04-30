using SeatTooLong.Core;
using SeatTooLong.Core.Statistics;

namespace SeatTooLong.Tests;

public class DailySummaryProjectionTests
{
    [Fact]
    public void IncludeActiveSitting_WhenSitting_AddsActiveDurationToTotal()
    {
        var summary = new DailySummary
        {
            Date = new DateTime(2026, 4, 30),
            TotalSittingDuration = TimeSpan.FromMinutes(20),
            StandUpCount = 1,
            LongestContinuousSitting = TimeSpan.FromMinutes(20)
        };

        var projected = DailySummaryProjection.IncludeActiveSitting(
            summary,
            SittingState.Sitting,
            TimeSpan.FromMinutes(11));

        Assert.Equal(TimeSpan.FromMinutes(31), projected.TotalSittingDuration);
        Assert.Equal(TimeSpan.FromMinutes(20), projected.LongestContinuousSitting);
        Assert.Equal(1, projected.StandUpCount);
    }

    [Fact]
    public void IncludeActiveSitting_WhenAlerting_UpdatesLongestContinuousSitting()
    {
        var summary = new DailySummary
        {
            Date = new DateTime(2026, 4, 30),
            TotalSittingDuration = TimeSpan.FromMinutes(10),
            StandUpCount = 0,
            LongestContinuousSitting = TimeSpan.FromMinutes(10)
        };

        var projected = DailySummaryProjection.IncludeActiveSitting(
            summary,
            SittingState.Alerting,
            TimeSpan.FromMinutes(45));

        Assert.Equal(TimeSpan.FromMinutes(55), projected.TotalSittingDuration);
        Assert.Equal(TimeSpan.FromMinutes(45), projected.LongestContinuousSitting);
    }

    [Theory]
    [InlineData(SittingState.Idle)]
    [InlineData(SittingState.Resting)]
    public void IncludeActiveSitting_WhenNotCurrentlySitting_ReturnsOriginalSummary(SittingState currentState)
    {
        var summary = new DailySummary
        {
            Date = new DateTime(2026, 4, 30),
            TotalSittingDuration = TimeSpan.FromMinutes(20),
            StandUpCount = 1,
            LongestContinuousSitting = TimeSpan.FromMinutes(20)
        };

        var projected = DailySummaryProjection.IncludeActiveSitting(
            summary,
            currentState,
            TimeSpan.FromMinutes(11));

        Assert.Same(summary, projected);
    }
}