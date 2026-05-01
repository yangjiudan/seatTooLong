namespace SeatTooLong.Core.Vision;

public static class SeatedUpperBodyRule
{
    private const double MinUpperBodyWidthRatio = 0.28;
    private const double MinUpperBodyHeightRatio = 0.48;
    private const double MinUpperBodyBottomRatio = 0.60;
    private const double MinUpperBodyCenterXRatio = 0.22;
    private const double MaxUpperBodyCenterXRatio = 0.72;

    public static bool IsSeatedUpperBody(int bodyWidth, int bodyHeight, int bodyX, int bodyY, int frameWidth, int frameHeight)
    {
        if (frameWidth <= 0 || frameHeight <= 0)
            return false;

        var bodyWidthRatio = (double)bodyWidth / frameWidth;
        if (bodyWidthRatio < MinUpperBodyWidthRatio)
            return false;

        var bodyHeightRatio = (double)bodyHeight / frameHeight;
        if (bodyHeightRatio < MinUpperBodyHeightRatio)
            return false;

        var bodyBottomRatio = (double)(bodyY + bodyHeight) / frameHeight;
        if (bodyBottomRatio < MinUpperBodyBottomRatio)
            return false;

        var bodyCenterXRatio = (bodyX + bodyWidth / 2.0) / frameWidth;
        return bodyCenterXRatio >= MinUpperBodyCenterXRatio
            && bodyCenterXRatio <= MaxUpperBodyCenterXRatio;
    }
}