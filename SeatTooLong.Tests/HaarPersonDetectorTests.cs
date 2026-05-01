using OpenCvSharp;
using SeatTooLong.Core;
using SeatTooLong.Core.Vision;

namespace SeatTooLong.Tests;

public class HaarPersonDetectorTests
{
    private const string RecordedSeatedSessionPath = "TestSamples/recordings/Seated/20260501-140930";

    private static DnnDeskPresenceDetector CreateDetector()
    {
        var faceModelPath = GetOutputPath("Assets/Models/ultraface-version-RFB-320.onnx");
        var profileFaceCascadePath = GetOutputPath("haarcascade_profileface.xml");
        var upperBodyCascadePath = GetOutputPath("haarcascade_upperbody.xml");

        Assert.True(File.Exists(faceModelPath), $"UltraFace model not found: {faceModelPath}");
        Assert.True(File.Exists(profileFaceCascadePath), $"Profile face cascade not found: {profileFaceCascadePath}");
        Assert.True(File.Exists(upperBodyCascadePath), $"Upper body cascade not found: {upperBodyCascadePath}");

        return new DnnDeskPresenceDetector(faceModelPath, profileFaceCascadePath, upperBodyCascadePath);
    }

    private static bool DetectSeatedPerson(DnnDeskPresenceDetector detector, byte[] frameData)
    {
        using var grayscale = Cv2.ImDecode(frameData, ImreadModes.Grayscale);
        if (grayscale.Empty())
            return false;

        return detector.DetectPerson(frameData, grayscale.Width, grayscale.Height);
    }

    private static bool DetectSeatedPerson(byte[] frameData)
    {
        using var detector = CreateDetector();
        return DetectSeatedPerson(detector, frameData);
    }

    private static bool DetectSeatedPerson(string imagePath)
    {
        return DetectSeatedPerson(File.ReadAllBytes(imagePath));
    }

    private static byte[] EncodeAsBitmap(string imagePath)
    {
        using var mat = Cv2.ImRead(imagePath, ImreadModes.Color);
        if (mat.Empty())
            return [];

        Cv2.ImEncode(".bmp", mat, out var bytes);
        return bytes;
    }

    private static string GetOutputPath(string relativePath)
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
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
        using var detector = CreateDetector();
        Assert.True(DetectSeatedPerson(detector, fullPath), $"Should detect seated person in {relativePath}");
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
        using var detector = CreateDetector();
        Assert.False(DetectSeatedPerson(detector, fullPath), $"Should NOT detect seated person in {relativePath}");
    }

    [Theory]
    [InlineData(RecordedSeatedSessionPath + "/000001-140934202.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000120-141935781.jpg")]
    public void RecordedSeatedRegressionFrames_ShouldDetectPerson(string relativePath)
    {
        var fullPath = GetOutputPath(relativePath);
        if (!File.Exists(fullPath))
            return;

        using var detector = CreateDetector();
        Assert.True(DetectSeatedPerson(detector, fullPath), $"Should detect seated person in recorded frame {relativePath}");
    }

    [Theory]
    [InlineData(RecordedSeatedSessionPath + "/000001-140934202.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000120-141935781.jpg")]
    public void RecordedSeatedRegressionFrames_WhenEncodedAsBitmap_ShouldDetectPerson(string relativePath)
    {
        var fullPath = GetOutputPath(relativePath);
        if (!File.Exists(fullPath))
            return;

        var bitmapBytes = EncodeAsBitmap(fullPath);
        Assert.NotEmpty(bitmapBytes);
        using var detector = CreateDetector();
        Assert.True(DetectSeatedPerson(detector, bitmapBytes), $"Should detect seated person in runtime-style BMP frame {relativePath}");
    }

    [Theory]
    [InlineData(RecordedSeatedSessionPath + "/000430-144543401.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000431-144548445.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000432-144553496.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000534-145429462.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000535-145434518.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000536-145439552.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000537-145444581.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000540-145459709.jpg")]
    public void RecordedSeatedHistoricalMisses_ShouldDetectPerson(string relativePath)
    {
        var fullPath = GetOutputPath(relativePath);
        if (!File.Exists(fullPath))
            return;

        using var detector = CreateDetector();
        Assert.True(DetectSeatedPerson(detector, fullPath), $"Should detect seated person in historical miss frame {relativePath}");
    }

    [Theory]
    [InlineData(RecordedSeatedSessionPath + "/000371-144045085.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000416-144432713.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000417-144437761.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000420-144452903.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000422-144503012.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000423-144508057.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000424-144513109.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000428-144533305.jpg")]
    [InlineData(RecordedSeatedSessionPath + "/000429-144538357.jpg")]
    public void RecordedSeatedCurrentMisses_ShouldDetectPerson(string relativePath)
    {
        var fullPath = GetOutputPath(relativePath);
        if (!File.Exists(fullPath))
            return;

        using var detector = CreateDetector();
        Assert.True(DetectSeatedPerson(detector, fullPath), $"Should detect seated person in current miss frame {relativePath}");
    }

    [Fact]
    public void RecordedSeatedCurrentMisses_WhenReusingDetector_ShouldMatchFreshDetector()
    {
        var sessionPath = GetOutputPath(RecordedSeatedSessionPath);
        if (!Directory.Exists(sessionPath))
            return;

        var imagePaths = Directory
            .EnumerateFiles(sessionPath, "*.jpg")
            .Where(path => string.CompareOrdinal(Path.GetFileName(path), "000360-143989591.jpg") >= 0)
            .Where(path => string.CompareOrdinal(Path.GetFileName(path), "000429-144538357.jpg") <= 0)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        using var reusedDetector = CreateDetector();
        var mismatches = new List<string>();
        foreach (var imagePath in imagePaths)
        {
            using var freshDetector = CreateDetector();
            var freshDetected = DetectSeatedPerson(freshDetector, imagePath);
            var reusedDetected = DetectSeatedPerson(reusedDetector, imagePath);
            if (freshDetected != reusedDetected)
                mismatches.Add($"{Path.GetFileName(imagePath)} fresh={freshDetected} reused={reusedDetected}");
        }

        Assert.True(mismatches.Count == 0, string.Join(", ", mismatches));
    }

    [Fact]
    public void RecordedSeatedSession_ShouldMeetRecallFloor()
    {
        var sessionPath = GetOutputPath(RecordedSeatedSessionPath);
        if (!Directory.Exists(sessionPath))
            return;

        var imagePaths = Directory
            .EnumerateFiles(sessionPath, "*.jpg")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(imagePaths);

        using var detector = CreateDetector();
        var missedPaths = imagePaths
            .Where(path => !DetectSeatedPerson(detector, path))
            .Select(Path.GetFileName)
            .ToArray();
        var detectedCount = imagePaths.Length - missedPaths.Length;
        var recall = (double)detectedCount / imagePaths.Length;

        Assert.True(
            recall >= 1.0,
            $"Expected seated recording recall = 100 %, actual {recall:P1} ({detectedCount}/{imagePaths.Length}). Missed: {string.Join(", ", missedPaths.Take(20))}");
    }

    private static bool DetectSeatedPerson(DnnDeskPresenceDetector detector, string imagePath)
    {
        return DetectSeatedPerson(detector, File.ReadAllBytes(imagePath));
    }
}
