using System.Globalization;

namespace SeatTooLong.Core.Localization;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    void SetLanguage(string lang);
    string Get(string key);
}

public class LocalizationService : ILocalizationService
{
    private Dictionary<string, string> _strings = new();
    public string CurrentLanguage { get; private set; } = "zh";

    private static readonly Dictionary<string, Dictionary<string, string>> AllStrings = new()
    {
        ["zh"] = new()
        {
            ["app.name"] = "SeatTooLong",
            ["tray.tooltip"] = "SeatTooLong - 久坐提醒",
            ["tray.open"] = "打开主界面",
            ["tray.pause"] = "暂停监测",
            ["tray.resume"] = "恢复监测",
            ["tray.record_start"] = "开始录制素材",
            ["tray.record_stop"] = "停止录制素材",
            ["tray.today"] = "查看今日统计",
            ["tray.settings"] = "设置",
            ["tray.exit"] = "退出",
            ["overlay.idle"] = "空闲",
            ["overlay.sitting"] = "已坐",
            ["overlay.absent"] = "离开",
            ["overlay.resting"] = "休息中",
            ["overlay.alert"] = "该起来活动了！",
            ["notify.sit_too_long.title"] = "您已连续坐了 {0} 分钟！",
            ["notify.sit_too_long.body"] = "建议站起来活动 {1} 分钟。",
            ["notify.rest_complete.title"] = "休息完成！",
            ["notify.rest_complete.body"] = "做得好！可以回到座位继续工作了。",
            ["notify.rest_insufficient.title"] = "休息不足",
            ["notify.rest_insufficient.body"] = "建议再休息 {0} 分钟。",
            ["notify.dismiss"] = "知道了",
            ["notify.snooze"] = "稍后提醒",
            ["settings.title"] = "设置",
            ["settings.sit_threshold"] = "久坐阈值（分钟）",
            ["settings.rest_duration"] = "建议休息时长（分钟）",
            ["settings.detection_interval"] = "检测间隔（秒）",
            ["settings.absence_grace_period"] = "离开宽限期（秒）",
            ["settings.camera"] = "摄像头",
            ["settings.autostart"] = "开机自启",
            ["settings.language"] = "语言",
            ["settings.language_auto"] = "跟随系统",
            ["settings.language_zh"] = "中文",
            ["settings.language_en"] = "English",
            ["settings.overlay"] = "显示悬浮窗",
            ["settings.overlay_opacity"] = "悬浮窗透明度",
            ["settings.save"] = "保存",
            ["settings.apply"] = "应用",
            ["settings.reset_defaults"] = "重置默认值",
            ["settings.applied"] = "设置已应用",
            ["settings.defaults_applied"] = "已重置为默认设置",
            ["unit.minutes"] = "分钟",
            ["unit.seconds"] = "秒",
            ["unit.percent"] = "%",
            ["report.title"] = "统计报表",
            ["report.today"] = "今日概览",
            ["report.total_sitting"] = "总久坐时长",
            ["report.stand_count"] = "起身次数",
            ["report.longest_sitting"] = "最长连续久坐",
            ["report.history"] = "历史趋势",
            ["report.last7days"] = "近7天",
            ["report.last30days"] = "近30天",
            ["report.refresh_hint"] = "每 5 秒写入 SQLite 并刷新，包含当前正在进行的久坐",
        },
        ["en"] = new()
        {
            ["app.name"] = "SeatTooLong",
            ["tray.tooltip"] = "SeatTooLong - Sitting Reminder",
            ["tray.open"] = "Open",
            ["tray.pause"] = "Pause Monitoring",
            ["tray.resume"] = "Resume Monitoring",
            ["tray.record_start"] = "Start Recording Samples",
            ["tray.record_stop"] = "Stop Recording Samples",
            ["tray.today"] = "Today's Stats",
            ["tray.settings"] = "Settings",
            ["tray.exit"] = "Exit",
            ["overlay.idle"] = "Idle",
            ["overlay.sitting"] = "Sitting",
            ["overlay.absent"] = "Away",
            ["overlay.resting"] = "Resting",
            ["overlay.alert"] = "Time to stand up!",
            ["notify.sit_too_long.title"] = "You've been sitting for {0} minutes!",
            ["notify.sit_too_long.body"] = "Time to stand up and move for {1} minutes.",
            ["notify.rest_complete.title"] = "Rest complete!",
            ["notify.rest_complete.body"] = "Great job! You can sit back down now.",
            ["notify.rest_insufficient.title"] = "Rest not enough",
            ["notify.rest_insufficient.body"] = "You still need about {0} minutes of rest.",
            ["notify.dismiss"] = "Got it",
            ["notify.snooze"] = "Snooze",
            ["settings.title"] = "Settings",
            ["settings.sit_threshold"] = "Sit Threshold (min)",
            ["settings.rest_duration"] = "Rest Duration (min)",
            ["settings.detection_interval"] = "Detection Interval (sec)",
            ["settings.absence_grace_period"] = "Away Grace Period (sec)",
            ["settings.camera"] = "Camera",
            ["settings.autostart"] = "Auto Start",
            ["settings.language"] = "Language",
            ["settings.language_auto"] = "System",
            ["settings.language_zh"] = "中文",
            ["settings.language_en"] = "English",
            ["settings.overlay"] = "Show Overlay",
            ["settings.overlay_opacity"] = "Overlay Opacity",
            ["settings.save"] = "Save",
            ["settings.apply"] = "Apply",
            ["settings.reset_defaults"] = "Reset Defaults",
            ["settings.applied"] = "Settings applied",
            ["settings.defaults_applied"] = "Defaults restored",
            ["unit.minutes"] = "min",
            ["unit.seconds"] = "sec",
            ["unit.percent"] = "%",
            ["report.title"] = "Statistics",
            ["report.today"] = "Today",
            ["report.total_sitting"] = "Total Sitting",
            ["report.stand_count"] = "Stand Up Count",
            ["report.longest_sitting"] = "Longest Sitting",
            ["report.history"] = "History",
            ["report.last7days"] = "Last 7 days",
            ["report.last30days"] = "Last 30 days",
            ["report.refresh_hint"] = "Persists to SQLite and refreshes every 5 seconds, including current sitting time",
        }
    };

    public LocalizationService(string language = "auto")
    {
        SetLanguage(language);
    }

    public void SetLanguage(string lang)
    {
        if (lang == "auto")
        {
            lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh" : "en";
        }
        CurrentLanguage = lang;
        _strings = AllStrings.ContainsKey(lang) ? AllStrings[lang] : AllStrings["en"];
    }

    public string Get(string key)
    {
        return _strings.TryGetValue(key, out var value) ? value : key;
    }
}
