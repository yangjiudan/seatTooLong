namespace SeatTooLong.Core.Vision;

public static class SeatedFaceRule
{
    private const int SeatedMinFaceSize = 85;
    private const double MinSeatedFaceCenterYRatio = 0.18;

    public static bool IsSeatedFace(int faceWidth, int faceHeight, int faceX, int faceY, int frameHeight)
    {
        if (frameHeight <= 0)
            return false;

        var faceSize = Math.Max(faceWidth, faceHeight);
        if (faceSize < SeatedMinFaceSize)
            return false;

        var faceCenterYRatio = (faceY + faceHeight / 2.0) / frameHeight;
        return faceCenterYRatio >= MinSeatedFaceCenterYRatio;
    }
}