using SeatTooLong.Core;

namespace SeatTooLong.Tests;

public class NotificationMessageBuilderTests
{
    [Fact]
    public void BuildSitTooLongMessage_ShouldContainDurationAndRestTime()
    {
        var msg = NotificationMessageBuilder.BuildSitTooLongMessage(
            TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(5), "zh");

        Assert.Contains("45", msg.Title);
        Assert.Contains("5", msg.Body);
    }

    [Fact]
    public void BuildSitTooLongMessage_English_ShouldContainDuration()
    {
        var msg = NotificationMessageBuilder.BuildSitTooLongMessage(
            TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(5), "en");

        Assert.Contains("60", msg.Title);
        Assert.Contains("5", msg.Body);
    }

    [Fact]
    public void BuildRestCompleteMessage_ShouldReturnMessage()
    {
        var msg = NotificationMessageBuilder.BuildRestCompleteMessage("zh");
        Assert.False(string.IsNullOrEmpty(msg.Title));
    }

    [Fact]
    public void BuildRestInsufficientMessage_ShouldContainRemaining()
    {
        var msg = NotificationMessageBuilder.BuildRestInsufficientMessage(
            TimeSpan.FromMinutes(3), "zh");

        Assert.Contains("3", msg.Body);
    }
}
