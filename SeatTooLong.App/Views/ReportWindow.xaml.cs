using System.Windows;
using System.Windows.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SeatTooLong.Core;
using SeatTooLong.Core.Localization;
using SeatTooLong.Core.Statistics;

namespace SeatTooLong.App.Views;

public partial class ReportWindow : Window
{
    private readonly IStatisticsRepository _repo;
    private readonly ILocalizationService _localization;
    private readonly Action? _flushActiveSessions;
    private readonly DispatcherTimer _refreshTimer;
    private int _currentRangeDays = 7;

    public ReportWindow(
        IStatisticsRepository repo,
        ILocalizationService localization,
        Action? flushActiveSessions = null)
    {
        InitializeComponent();
        _repo = repo;
        _localization = localization;
        _flushActiveSessions = flushActiveSessions;
        ApplyLocalization();
        RefreshReport();

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _refreshTimer.Tick += (_, _) => RefreshReport();
        _refreshTimer.Start();
        Closed += (_, _) => _refreshTimer.Stop();
    }

    private void ApplyLocalization()
    {
        Title = _localization.Get("report.title");
        GrpToday.Header = _localization.Get("report.today");
        LblTotalSitting.Text = _localization.Get("report.total_sitting");
        LblStandCount.Text = _localization.Get("report.stand_count");
        LblLongest.Text = _localization.Get("report.longest_sitting");
        Btn7Days.Content = _localization.Get("report.last7days");
        Btn30Days.Content = _localization.Get("report.last30days");
        TxtRefreshHint.Text = _localization.Get("report.refresh_hint");
    }

    private void RefreshReport()
    {
        _flushActiveSessions?.Invoke();
        ApplyLocalization();
        LoadTodaySummary();
        LoadChart(_currentRangeDays);
    }

    private void LoadTodaySummary()
    {
        var summary = _repo.GetDailySummary(DateTime.Today);
        ValTotalSitting.Text = FormatDuration(summary.TotalSittingDuration);
        ValStandCount.Text = summary.StandUpCount.ToString();
        ValLongest.Text = FormatDuration(summary.LongestContinuousSitting);
    }

    private void LoadChart(int days)
    {
        _currentRangeDays = days;
        var from = DateTime.Today.AddDays(-(days - 1));
        var to = DateTime.Today;
        var summaries = _repo.GetDailySummaries(from, to);

        var sittingValues = summaries.Select(summary => summary.TotalSittingDuration.TotalMinutes).ToArray();
        var standUpValues = summaries.Select(summary => (double)summary.StandUpCount).ToArray();
        var labels = summaries.Select(summary => summary.Date.ToString("MM/dd")).ToArray();

        Chart.Series = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = _localization.Get("report.total_sitting") + " (min)",
                Values = sittingValues,
                Fill = new SolidColorPaint(SKColors.SteelBlue)
            },
            new LineSeries<double>
            {
                Name = _localization.Get("report.stand_count"),
                Values = standUpValues,
                Stroke = new SolidColorPaint(SKColors.OrangeRed) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 8
            }
        };

        Chart.XAxes = new Axis[]
        {
            new Axis { Labels = labels, LabelsRotation = 45 }
        };
    }

    private void Btn7Days_Click(object sender, RoutedEventArgs args) => RefreshRange(7);
    private void Btn30Days_Click(object sender, RoutedEventArgs args) => RefreshRange(30);

    private void RefreshRange(int days)
    {
        _currentRangeDays = days;
        RefreshReport();
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{(int)ts.TotalMinutes}m";
    }
}
