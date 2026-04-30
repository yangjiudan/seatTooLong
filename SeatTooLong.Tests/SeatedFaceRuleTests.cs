using SeatTooLong.Core.Vision;

namespace SeatTooLong.Tests;

public class SeatedFaceRuleTests
{
    [Fact]
    public void IsSeated_WhenFaceLargeEnoughAndInSeatedBand_ReturnsTrue()
    {
        var result = SeatedFaceRule.IsSeatedFace(
            faceWidth: 110,
            faceHeight: 105,
            faceX: 200,
            faceY: 110,
            frameHeight: 480);

        Assert.True(result);
    }

    [Fact]
    public void IsSeated_WhenFaceTooSmall_ReturnsFalse()
    {
        var result = SeatedFaceRule.IsSeatedFace(
            faceWidth: 82,
            faceHeight: 82,
            faceX: 260,
            faceY: 120,
            frameHeight: 480);

        Assert.False(result);
    }

    [Fact]
    public void IsSeated_WhenFaceIsHighInFrame_ReturnsFalse()
    {
        var result = SeatedFaceRule.IsSeatedFace(
            faceWidth: 120,
            faceHeight: 120,
            faceX: 260,
            faceY: 0,
            frameHeight: 480);

        Assert.False(result);
    }
}