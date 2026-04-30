using OpenCvSharp;
using SeatTooLong.Core;
using SeatTooLong.Core.Vision;
using System.IO;

namespace SeatTooLong.App.Services;

public class HaarPersonDetector : IPersonDetector
{
    private readonly CascadeClassifier _faceCascade;

    public HaarPersonDetector()
    {
        var cascadePath = Path.Combine(AppContext.BaseDirectory, "haarcascade_frontalface_default.xml");
        _faceCascade = new CascadeClassifier(cascadePath);
    }

    public bool DetectPerson(byte[] frameData, int width, int height)
    {
        using var mat = Cv2.ImDecode(frameData, ImreadModes.Grayscale);
        if (mat.Empty())
            return false;

        Cv2.EqualizeHist(mat, mat);

        var faces = _faceCascade.DetectMultiScale(
            mat,
            scaleFactor: 1.05,
            minNeighbors: 3,
            flags: HaarDetectionTypes.ScaleImage,
            minSize: new Size(30, 30));

        return faces.Any(face => SeatedFaceRule.IsSeatedFace(
            face.Width,
            face.Height,
            face.X,
            face.Y,
            mat.Height));
    }
}
