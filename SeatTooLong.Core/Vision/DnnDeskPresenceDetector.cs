using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace SeatTooLong.Core.Vision;

public sealed class DnnDeskPresenceDetector : IPersonDetector, IDisposable
{
    private static readonly Size FaceInputSize = new(320, 240);

    private readonly object _gate = new();
    private readonly string _faceModelPath;
    private readonly float _faceScoreThreshold;
    private readonly float _faceNmsThreshold;
    private readonly CascadeClassifier _profileFaceCascade;
    private readonly CascadeClassifier _upperBodyCascade;
    private Net? _faceDetector;
    private bool _disposed;

    public DnnDeskPresenceDetector(
        string faceModelPath,
        string profileFaceCascadePath,
        string upperBodyCascadePath,
        float faceScoreThreshold = 0.65f,
        float faceNmsThreshold = 0.3f)
    {
        _faceModelPath = faceModelPath;
        _faceScoreThreshold = faceScoreThreshold;
        _faceNmsThreshold = faceNmsThreshold;
        _profileFaceCascade = new CascadeClassifier(profileFaceCascadePath);
        _upperBodyCascade = new CascadeClassifier(upperBodyCascadePath);
    }

    public bool DetectPerson(byte[] frameData, int width, int height)
    {
        lock (_gate)
        {
            ThrowIfDisposed();

            using var color = Cv2.ImDecode(frameData, ImreadModes.Color);
            if (color.Empty())
                return false;

            using var equalizedColor = CreateEqualizedColorFrame(color);
            using var grayscale = new Mat();
            Cv2.CvtColor(color, grayscale, ColorConversionCodes.BGR2GRAY);

            using var equalizedGrayscale = new Mat();
            Cv2.EqualizeHist(grayscale, equalizedGrayscale);

            return HasSeatedFace(color)
                || HasSeatedFace(equalizedColor)
                || HasSeatedFaceAtAngle(equalizedColor, -12)
                || HasSeatedFaceAtAngle(equalizedColor, 12)
                || HasSeatedProfileFace(grayscale)
                || HasSeatedProfileFace(equalizedGrayscale)
                || HasSeatedUpperBody(grayscale)
                || HasSeatedUpperBody(equalizedGrayscale);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _faceDetector?.Dispose();
            _profileFaceCascade.Dispose();
            _upperBodyCascade.Dispose();
            _disposed = true;
        }
    }

    private bool HasSeatedFace(Mat image)
    {
        var faceDetector = EnsureFaceDetector();

        using var rgb = new Mat();
        Cv2.CvtColor(image, rgb, ColorConversionCodes.BGR2RGB);

        using var blob = CvDnn.BlobFromImage(
            rgb,
            scaleFactor: 1.0 / 128.0,
            size: FaceInputSize,
            mean: new Scalar(127, 127, 127),
            swapRB: false,
            crop: false);

        faceDetector.SetInput(blob, string.Empty);

        using var scores = new Mat();
        using var boxes = new Mat();
        faceDetector.Forward(new[] { scores, boxes }, new[] { "scores", "boxes" });
        if (scores.Empty() || boxes.Empty())
            return false;

        using var scoreRows = scores.Reshape(1, (int)(scores.Total() / 2));
        using var boxRows = boxes.Reshape(1, (int)(boxes.Total() / 4));
        var candidates = new List<Rect2d>();
        var candidateScores = new List<float>();

        for (var row = 0; row < Math.Min(scoreRows.Rows, boxRows.Rows); row++)
        {
            var confidence = scoreRows.At<float>(row, 1);
            if (confidence < _faceScoreThreshold)
                continue;

            var xMin = Math.Clamp((int)Math.Round(boxRows.At<float>(row, 0) * image.Width), 0, image.Width);
            var yMin = Math.Clamp((int)Math.Round(boxRows.At<float>(row, 1) * image.Height), 0, image.Height);
            var xMax = Math.Clamp((int)Math.Round(boxRows.At<float>(row, 2) * image.Width), 0, image.Width);
            var yMax = Math.Clamp((int)Math.Round(boxRows.At<float>(row, 3) * image.Height), 0, image.Height);

            if (xMax <= xMin || yMax <= yMin)
                continue;

            candidates.Add(new Rect2d(xMin, yMin, xMax - xMin, yMax - yMin));
            candidateScores.Add(confidence);
        }

        if (candidates.Count == 0)
            return false;

        CvDnn.NMSBoxes(candidates, candidateScores, _faceScoreThreshold, _faceNmsThreshold, out var keptIndices);
        foreach (var index in keptIndices)
        {
            var candidate = candidates[index];
            var faceX = (int)Math.Round(candidate.X);
            var faceY = (int)Math.Round(candidate.Y);
            var faceWidth = Math.Max(1, (int)Math.Round(candidate.Width));
            var faceHeight = Math.Max(1, (int)Math.Round(candidate.Height));

            if (SeatedFaceRule.IsSeatedFace(faceWidth, faceHeight, faceX, faceY, image.Width, image.Height)
                || SeatedProfileFaceRule.IsSeatedProfileFace(faceWidth, faceHeight, faceX, faceY, image.Width, image.Height))
            {
                return true;
            }
        }

        return false;
    }

    private Net EnsureFaceDetector()
    {
        if (_faceDetector != null)
            return _faceDetector;

        _faceDetector = Net.ReadNetFromONNX(_faceModelPath);
        return _faceDetector!;
    }

    private bool HasSeatedFaceAtAngle(Mat image, double angle)
    {
        using var rotated = new Mat();
        var center = new Point2f(image.Width / 2f, image.Height / 2f);
        using var rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);
        Cv2.WarpAffine(
            image,
            rotated,
            rotationMatrix,
            new Size(image.Width, image.Height),
            flags: InterpolationFlags.Linear,
            borderMode: BorderTypes.Replicate);

        return HasSeatedFace(rotated);
    }

    private bool HasSeatedUpperBody(Mat image)
    {
        var upperBodies = _upperBodyCascade.DetectMultiScale(
            image,
            scaleFactor: 1.02,
            minNeighbors: 2,
            flags: HaarDetectionTypes.ScaleImage,
            minSize: new Size(90, 120));

        return upperBodies.Any(body => SeatedUpperBodyRule.IsSeatedUpperBody(
            body.Width,
            body.Height,
            body.X,
            body.Y,
            image.Width,
            image.Height));
    }

    private bool HasSeatedProfileFace(Mat image)
    {
        if (HasSeatedProfileFace(_profileFaceCascade, image))
            return true;

        using var flipped = new Mat();
        Cv2.Flip(image, flipped, FlipMode.Y);
        return HasSeatedProfileFace(_profileFaceCascade, flipped);
    }

    private static bool HasSeatedProfileFace(CascadeClassifier cascade, Mat image)
    {
        var faces = cascade.DetectMultiScale(
            image,
            scaleFactor: 1.05,
            minNeighbors: 3,
            flags: HaarDetectionTypes.ScaleImage,
            minSize: new Size(50, 50));

        return faces.Any(face => SeatedProfileFaceRule.IsSeatedProfileFace(
            face.Width,
            face.Height,
            face.X,
            face.Y,
            image.Width,
            image.Height));
    }

    private static Mat CreateEqualizedColorFrame(Mat color)
    {
        using var yCrCb = new Mat();
        Cv2.CvtColor(color, yCrCb, ColorConversionCodes.BGR2YCrCb);

        var channels = Cv2.Split(yCrCb);
        try
        {
            Cv2.EqualizeHist(channels[0], channels[0]);
            Cv2.Merge(channels, yCrCb);

            var result = new Mat();
            Cv2.CvtColor(yCrCb, result, ColorConversionCodes.YCrCb2BGR);
            return result;
        }
        finally
        {
            foreach (var channel in channels)
                channel.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DnnDeskPresenceDetector));
    }
}