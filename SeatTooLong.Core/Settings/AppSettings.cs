namespace SeatTooLong.Core.Settings;

public class AppSettings
{
    public int SitThresholdMinutes { get; set; } = 45;
    public int RestDurationMinutes { get; set; } = 5;
    public int DetectionIntervalSeconds { get; set; } = 2;
    public int AbsenceGracePeriodSeconds { get; set; } = 2;
    public int CameraIndex { get; set; } = 0;
    public string CameraDeviceName { get; set; } = string.Empty;
    public bool AutoStart { get; set; } = true;
    public string Language { get; set; } = "auto";
    public bool ShowOverlay { get; set; } = true;
    public double OverlayOpacity { get; set; } = 0.8;
}
