using OpenCvSharp;
using SeatTooLong.Core;
using SeatTooLong.Core.Vision;

namespace SeatTooLong.Tests;

public class HaarPersonDetectorTests
{
    private static bool DetectSeatedPerson(string imagePath)
    {
        var cascadePath = Path.Combine(AppContext.BaseDirectory, "haarcascade_frontalface_default.xml");
        var faceCascade = new CascadeClassifier(cascadePath);

        using var mat = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
        if (mat.Empty()) return false;
        Cv2.EqualizeHist(mat, mat);
        var faces = faceCascade.DetectMultiScale(mat, 1.05, 3, HaarDetectionTypes.ScaleImage, new Size(30, 30));
        return faces.Any(face => SeatedFaceRule.IsSeatedFace(face.Width, face.Height, face.X, face.Y, mat.Height));
    }

    [Theory]
    [InlineData("TestSamples/Seated/WIN_20260430_17_52_19_Pro.jpg")]
    [InlineData("TestSamples/Seated/WIN_20260430_17_52_32_Pro.jpg")]
    [InlineData("TestSamples/Seated/WIN_20260430_17_55_25_Pro.jpg")]
    [InlineData("TestSamples/Seated/WIN_20260430_17_55_40_Pro.jpg")]
    public void SeatedImages_ShouldDetectPerson(string relativePath)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        Assert.True(File.Exists(fullPath), $"Test image not found: {fullPath}");
        Assert.True(DetectSeatedPerson(fullPath), $"Should detect seated person in {relativePath}");
    }

    [Theory]
    [InlineData("TestSamples/Not Seated/WIN_20260430_17_52_52_Pro.jpg")]
    [InlineData("TestSamples/Not Seated/WIN_20260430_17_53_35_Pro.jpg")]
    [InlineData("TestSamples/Not Seated/WIN_20260430_17_54_53_Pro.jpg")]
    [InlineData("TestSamples/Not Seated/WIN_20260430_17_55_04_Pro.jpg")]
    [InlineData("TestSamples/Not Seated/WIN_20260430_17_55_09_Pro.jpg")]
    [InlineData("TestSamples/Not Seated/WIN_20260430_17_55_56_Pro.jpg")]
    public void NotSeatedImages_ShouldNotDetectPerson(string relativePath)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        Assert.True(File.Exists(fullPath), $"Test image not found: {fullPath}");
        Assert.False(DetectSeatedPerson(fullPath), $"Should NOT detect seated person in {relativePath}");
    }
}
