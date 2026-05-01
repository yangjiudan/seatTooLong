namespace SeatTooLong.Core.Vision;

public static class SeatedFaceRule
{
    private const int SeatedMinFaceSize = 85;
    private const double MinSeatedFaceCenterYRatio = 0.18;
    private const double MinHorizontalFaceMarginRatio = 0.03;
    private const double MaxSeatedFaceWidthRatio = 0.32;
    private const double MaxSeatedFaceHeightRatio = 0.65;

    public static bool IsSeatedFace(int faceWidth, int faceHeight, int faceX, int faceY, int frameWidth, int frameHeight)
    {
        if (frameWidth <= 0 || frameHeight <= 0)
            return false;

        var faceSize = Math.Max(faceWidth, faceHeight);
        if (faceSize < SeatedMinFaceSize)
            return false;

        var faceWidthRatio = (double)faceWidth / frameWidth;
        if (faceWidthRatio > MaxSeatedFaceWidthRatio)
            return false;

        var faceHeightRatio = (double)faceHeight / frameHeight;
        if (faceHeightRatio > MaxSeatedFaceHeightRatio)
            return false;

        var minHorizontalMargin = frameWidth * MinHorizontalFaceMarginRatio;
        if (faceX <= minHorizontalMargin || faceX + faceWidth >= frameWidth - minHorizontalMargin)
            return false;

        var faceCenterYRatio = (faceY + faceHeight / 2.0) / frameHeight;
        return faceCenterYRatio >= MinSeatedFaceCenterYRatio;
    }
}