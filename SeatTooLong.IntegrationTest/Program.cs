using OpenCvSharp;
using System;
using System.IO;

Console.WriteLine("=== SeatTooLong Live Camera Test (Face-Size Strategy) ===");
Console.WriteLine();

using var capture = new VideoCapture(0);
if (!capture.IsOpened()) { Console.WriteLine("ERROR: Cannot open camera"); return 1; }
Console.WriteLine($"Camera: {capture.FrameWidth}x{capture.FrameHeight}");

var cascadePath = Path.Combine(AppContext.BaseDirectory, "haarcascade_frontalface_default.xml");
var faceCascade = new CascadeClassifier(cascadePath);
const int SeatedMinFaceSize = 85;

bool DetectSeated()
{
    using var frame = new Mat();
    capture.Read(frame);
    if (frame.Empty()) return false;
    using var gray = new Mat();
    Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
    Cv2.EqualizeHist(gray, gray);
    var faces = faceCascade.DetectMultiScale(gray, 1.05, 3, HaarDetectionTypes.ScaleImage, new Size(30, 30));
    int maxSize = faces.Length > 0 ? faces.Max(f => Math.Max(f.Width, f.Height)) : 0;
    return maxSize >= SeatedMinFaceSize;
}

// Phase 1: Please stay seated (10s)
Console.WriteLine("Phase 1: Please STAY SEATED (10 seconds)...");
int seated = 0;
for (int i = 0; i < 10; i++)
{
    bool det = DetectSeated();
    if (det) seated++;
    Console.WriteLine($"  [{i+1:D2}/10] {(det ? "SEATED" : "---")}");
    if (i < 9) System.Threading.Thread.Sleep(1000);
}
Console.WriteLine($"  => {seated}/10 detected as seated");
Console.WriteLine();

// Phase 2: Leave
Console.WriteLine("=============================================");
Console.WriteLine("  PLEASE LEAVE YOUR DESK NOW!");
Console.WriteLine("  (Stand up and step back from camera)");
Console.WriteLine("  Detection starts in 10 seconds...");
Console.WriteLine("=============================================");
System.Threading.Thread.Sleep(10000);

Console.WriteLine();
Console.WriteLine("Phase 2: Detecting absence (10 seconds)...");
int absent = 0;
for (int i = 0; i < 10; i++)
{
    bool det = DetectSeated();
    if (!det) absent++;
    Console.WriteLine($"  [{i+1:D2}/10] {(det ? "SEATED (still?)" : "NOT SEATED")}");
    if (i < 9) System.Threading.Thread.Sleep(1000);
}
Console.WriteLine($"  => {absent}/10 detected as not-seated");
Console.WriteLine();

Console.WriteLine("=== RESULT ===");
Console.WriteLine($"  Seated rate:  {seated}/10 ({seated*10}%)");
Console.WriteLine($"  Absence rate: {absent}/10 ({absent*10}%)");
bool pass = seated >= 7 && absent >= 7;
Console.WriteLine(pass ? "PASS!" : "NEEDS WORK");
return pass ? 0 : 1;
