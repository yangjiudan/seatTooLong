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
            frameWidth: 640,
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
            frameWidth: 640,
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
            frameWidth: 640,
            frameHeight: 480);

        Assert.False(result);
    }

    [Fact]
    public void IsSeated_WhenFaceTouchesFrameEdge_ReturnsFalse()
    {
        var result = SeatedFaceRule.IsSeatedFace(
            faceWidth: 120,
            faceHeight: 120,
            faceX: 0,
            faceY: 120,
            frameWidth: 640,
            frameHeight: 480);

        Assert.False(result);
    }

    [Fact]
    public void IsSeated_WhenFaceOccupiesMostOfFrameHeight_ReturnsFalse()
    {
        var result = SeatedFaceRule.IsSeatedFace(
            faceWidth: 180,
            faceHeight: 360,
            faceX: 180,
            faceY: 20,
            frameWidth: 640,
            frameHeight: 480);

        Assert.False(result);
    }

    [Fact]
    public void IsSeatedUpperBody_WhenCenteredAndTallEnough_ReturnsTrue()
    {
        var result = SeatedUpperBodyRule.IsSeatedUpperBody(
            bodyWidth: 250,
            bodyHeight: 360,
            bodyX: 140,
            bodyY: 60,
            frameWidth: 640,
            frameHeight: 480);

        Assert.True(result);
    }

    [Fact]
    public void IsSeatedUpperBody_WhenHighButLargeEnough_ReturnsTrue()
    {
        var result = SeatedUpperBodyRule.IsSeatedUpperBody(
            bodyWidth: 300,
            bodyHeight: 246,
            bodyX: 229,
            bodyY: 45,
            frameWidth: 640,
            frameHeight: 480);

        Assert.True(result);
    }

    [Fact]
    public void IsSeatedUpperBody_WhenShiftedToEdge_ReturnsFalse()
    {
        var result = SeatedUpperBodyRule.IsSeatedUpperBody(
            bodyWidth: 250,
            bodyHeight: 360,
            bodyX: 10,
            bodyY: 60,
            frameWidth: 640,
            frameHeight: 480);

        Assert.False(result);
    }

    [Fact]
    public void IsSeatedUpperBody_WhenTooShortInFrame_ReturnsFalse()
    {
        var result = SeatedUpperBodyRule.IsSeatedUpperBody(
            bodyWidth: 180,
            bodyHeight: 200,
            bodyX: 180,
            bodyY: 40,
            frameWidth: 640,
            frameHeight: 480);

        Assert.False(result);
    }

    [Fact]
    public void IsSeatedProfileFace_WhenLargeAndNearFrameEdge_ReturnsTrue()
    {
        var result = SeatedProfileFaceRule.IsSeatedProfileFace(
            faceWidth: 82,
            faceHeight: 120,
            faceX: 6,
            faceY: 120,
            frameWidth: 640,
            frameHeight: 480);

        Assert.True(result);
    }

    [Fact]
    public void IsSeatedProfileFace_WhenNotNearFrameEdge_ReturnsFalse()
    {
        var result = SeatedProfileFaceRule.IsSeatedProfileFace(
            faceWidth: 82,
            faceHeight: 120,
            faceX: 180,
            faceY: 120,
            frameWidth: 640,
            frameHeight: 480);

        Assert.False(result);
    }

    [Fact]
    public void IsSeatedProfileFace_WhenTooLowInFrame_ReturnsFalse()
    {
        var result = SeatedProfileFaceRule.IsSeatedProfileFace(
            faceWidth: 82,
            faceHeight: 120,
            faceX: 6,
            faceY: 330,
            frameWidth: 640,
            frameHeight: 480);

        Assert.False(result);
    }

    [Fact]
    public void IsSeatedProfileFace_WhenTooWideForItsHeight_ReturnsFalse()
    {
        var result = SeatedProfileFaceRule.IsSeatedProfileFace(
            faceWidth: 86,
            faceHeight: 108,
            faceX: 6,
            faceY: 120,
            frameWidth: 640,
            frameHeight: 480);

        Assert.False(result);
    }
}