using System.Windows;
using System.Windows.Controls;
using SeatTooLong.Core.Localization;
using SeatTooLong.Core.Settings;

namespace SeatTooLong.App.Views;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly ILocalizationService _localization;
    private readonly IReadOnlyList<string> _cameras;
    public event Action<AppSettings>? SettingsSaved;

    public SettingsWindow(ISettingsService settingsService, ILocalizationService localization, IReadOnlyList<string> cameras)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _localization = localization;
        _cameras = cameras;
        LoadSettings();
        ApplyLocalization();
        UpdateValueLabels();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        ApplySettingsToControls(settings);
    }

    private void ApplySettingsToControls(AppSettings settings)
    {
        SliderSitThreshold.Value = settings.SitThresholdMinutes;
        SliderRestDuration.Value = settings.RestDurationMinutes;
        SetComboByContent(CmbInterval, settings.DetectionIntervalSeconds.ToString());
        SliderAbsenceGracePeriod.Value = settings.AbsenceGracePeriodSeconds;
        ChkAutoStart.IsChecked = settings.AutoStart;
        SetComboByTag(CmbLanguage, settings.Language);
        ChkOverlay.IsChecked = settings.ShowOverlay;
        SliderOpacity.Value = settings.OverlayOpacity;

        CmbCamera.Items.Clear();
        for (int index = 0; index < _cameras.Count; index++)
            CmbCamera.Items.Add(_cameras[index]);
        if (_cameras.Count > 0)
            CmbCamera.SelectedIndex = CameraSelection.GetSelectedOptionIndex(_cameras, settings.CameraIndex);

        UpdateValueLabels();
    }

    private void ApplyLocalization()
    {
        Title = _localization.Get("settings.title");
        LblSitThreshold.Text = _localization.Get("settings.sit_threshold");
        LblRestDuration.Text = _localization.Get("settings.rest_duration");
        LblInterval.Text = _localization.Get("settings.detection_interval");
        LblAbsenceGracePeriod.Text = _localization.Get("settings.absence_grace_period");
        LblCamera.Text = _localization.Get("settings.camera");
        LblAutoStart.Text = _localization.Get("settings.autostart");
        LblLanguage.Text = _localization.Get("settings.language");
        LblOverlay.Text = _localization.Get("settings.overlay");
        LblOpacity.Text = _localization.Get("settings.overlay_opacity");
        CmbLanguageAuto.Content = _localization.Get("settings.language_auto");
        CmbLanguageZh.Content = _localization.Get("settings.language_zh");
        CmbLanguageEn.Content = _localization.Get("settings.language_en");
        BtnApply.Content = _localization.Get("settings.apply");
        BtnReset.Content = _localization.Get("settings.reset_defaults");
        BtnSave.Content = _localization.Get("settings.save");
    }

    private void BtnApply_Click(object sender, RoutedEventArgs args)
    {
        ApplyCurrentSettings(closeWindow: false, statusKey: "settings.applied");
    }

    private void BtnSave_Click(object sender, RoutedEventArgs args)
    {
        ApplyCurrentSettings(closeWindow: true, statusKey: "settings.applied");
    }

    private void BtnReset_Click(object sender, RoutedEventArgs args)
    {
        ApplySettingsToControls(new AppSettings());
        ApplyCurrentSettings(closeWindow: false, statusKey: "settings.defaults_applied");
    }

    private void ApplyCurrentSettings(bool closeWindow, string statusKey)
    {
        var settings = new AppSettings
        {
            SitThresholdMinutes = (int)SliderSitThreshold.Value,
            RestDurationMinutes = (int)SliderRestDuration.Value,
            DetectionIntervalSeconds = int.Parse(((ComboBoxItem)CmbInterval.SelectedItem).Content.ToString()!),
            AbsenceGracePeriodSeconds = (int)SliderAbsenceGracePeriod.Value,
            CameraIndex = CameraSelection.ResolveCameraIndex(_cameras, CmbCamera.SelectedIndex),
            AutoStart = ChkAutoStart.IsChecked == true,
            Language = ((ComboBoxItem)CmbLanguage.SelectedItem).Tag?.ToString() ?? "zh",
            ShowOverlay = ChkOverlay.IsChecked == true,
            OverlayOpacity = SliderOpacity.Value
        };

        _settingsService.Save(settings);
        SettingsSaved?.Invoke(settings);
        ApplyLocalization();
        UpdateValueLabels();
        TxtStatus.Text = _localization.Get(statusKey);

        if (closeWindow)
            Close();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
    {
        UpdateValueLabels();
    }

    private void UpdateValueLabels()
    {
        if (TxtSitThresholdValue == null || TxtRestDurationValue == null || TxtAbsenceGracePeriodValue == null || TxtOpacityValue == null)
            return;

        var minutesUnit = _localization.Get("unit.minutes");
        var secondsUnit = _localization.Get("unit.seconds");
        TxtSitThresholdValue.Text = $"{(int)SliderSitThreshold.Value} {minutesUnit}";
        TxtRestDurationValue.Text = $"{(int)SliderRestDuration.Value} {minutesUnit}";
        TxtAbsenceGracePeriodValue.Text = $"{(int)SliderAbsenceGracePeriod.Value} {secondsUnit}";
        TxtOpacityValue.Text = $"{Math.Round(SliderOpacity.Value * 100):0}{_localization.Get("unit.percent")}";
    }

    private static void SetComboByContent(ComboBox comboBox, string content)
    {
        for (int index = 0; index < comboBox.Items.Count; index++)
        {
            if (comboBox.Items[index] is ComboBoxItem item && item.Content?.ToString() == content)
            {
                comboBox.SelectedIndex = index;
                return;
            }
        }
    }

    private static void SetComboByTag(ComboBox comboBox, string tag)
    {
        for (int index = 0; index < comboBox.Items.Count; index++)
        {
            if (comboBox.Items[index] is ComboBoxItem item && item.Tag?.ToString() == tag)
            {
                comboBox.SelectedIndex = index;
                return;
            }
        }
    }
}
