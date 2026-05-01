namespace SeatTooLong.Core;

public record NotificationMessage(string Title, string Body);

public static class NotificationMessageBuilder
{
    public static NotificationMessage BuildSitTooLongMessage(TimeSpan duration, TimeSpan suggestedRest, string lang)
    {
        int durationMin = (int)duration.TotalMinutes;
        int restMin = (int)suggestedRest.TotalMinutes;

        if (lang == "en")
        {
            return new NotificationMessage(
                $"You've been sitting for {durationMin} minutes!",
                $"Time to stand up and move around for {restMin} minutes.");
        }

        return new NotificationMessage(
            $"您已连续坐了 {durationMin} 分钟！",
            $"建议站起来活动 {restMin} 分钟。");
    }

    public static NotificationMessage BuildRestCompleteMessage(string lang)
    {
        if (lang == "en")
        {
            return new NotificationMessage(
                "Rest complete!",
                "Great job! You can sit back down now.");
        }

        return new NotificationMessage(
            "休息完成！",
            "做得好！可以回到座位继续工作了。");
    }

    public static NotificationMessage BuildRestInsufficientMessage(TimeSpan remaining, string lang)
    {
        int remainingMin = (int)Math.Ceiling(remaining.TotalMinutes);

        if (lang == "en")
        {
            return new NotificationMessage(
                "Rest not enough",
                $"You still need about {remainingMin} minutes of rest.");
        }

        return new NotificationMessage(
            "休息不足",
            $"建议再休息 {remainingMin} 分钟。");
    }

    public static NotificationMessage BuildCameraOpenFailedMessage(string lang)
    {
        if (lang == "en")
        {
            return new NotificationMessage(
                "Camera unavailable",
                "SeatTooLong could not access the camera. Check whether it is connected, already in use, or blocked by Windows privacy settings.");
        }

        return new NotificationMessage(
            "摄像头不可用",
            "SeatTooLong 无法访问摄像头。请检查摄像头是否已连接、被占用，或被 Windows 隐私权限拦截。");
    }

    public static NotificationMessage BuildCameraReadFailedMessage(string lang)
    {
        if (lang == "en")
        {
            return new NotificationMessage(
                "Camera frame read failed",
                "SeatTooLong cannot read frames from the camera right now. Monitoring will keep retrying automatically.");
        }

        return new NotificationMessage(
            "摄像头读取失败",
            "SeatTooLong 暂时无法读取摄像头画面，监测会继续自动重试。请检查连接状态或关闭正在占用摄像头的程序。");
    }
}
