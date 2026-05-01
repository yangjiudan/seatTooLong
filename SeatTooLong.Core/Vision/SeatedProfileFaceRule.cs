namespace SeatTooLong.Core.Vision;

public static class SeatedProfileFaceRule
{
    private const int SeatedMinProfileFaceSize = 72;
    private const double MinSeatedProfileFaceCenterYRatio = 0.18;
    private const double MaxSeatedProfileFaceCenterYRatio = 0.75;
    private const double MaxProfileFaceEdgeDistanceRatio = 0.12;
    private const double MinProfileFaceTallnessRatio = 1.45;

    public static bool IsSeatedProfileFace(int faceWidth, int faceHeight, int faceX, int faceY, int frameWidth, int frameHeight)
    {
        if (frameWidth <= 0 || frameHeight <= 0)
            return false;

        var faceSize = Math.Max(faceWidth, faceHeight);
        if (faceSize < SeatedMinProfileFaceSize)
            return false;

        var faceTallnessRatio = (double)faceHeight / faceWidth;
        if (faceTallnessRatio < MinProfileFaceTallnessRatio)
            return false;

        var faceCenterYRatio = (faceY + faceHeight / 2.0) / frameHeight;
        if (faceCenterYRatio < MinSeatedProfileFaceCenterYRatio)
            return false;
        if (faceCenterYRatio > MaxSeatedProfileFaceCenterYRatio)
            return false;

        var maxEdgeDistance = frameWidth * MaxProfileFaceEdgeDistanceRatio;
        return faceX <= maxEdgeDistance || faceX + faceWidth >= frameWidth - maxEdgeDistance;
    }
}