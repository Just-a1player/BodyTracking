// =============================================================================
//  BodyTrackingPipeline.cs
//  The central orchestrator.  Owns one of each subsystem and wires them
//  together.  Call Run() to start the loop; press 'Q' or Escape to stop.
//
//  Data flow every frame:
//    CameraCapture → PoseDetector → PersonTracker → SkeletonRenderer → Window
// =============================================================================

using OpenCvSharp;
using BodyTracker.Camera;
using BodyTracker.Detection;
using BodyTracker.Models;
using BodyTracker.Rendering;
using BodyTracker.Utils;

namespace BodyTracker;

public sealed class BodyTrackingPipeline : IDisposable
{
    // ── Sub-systems ────────────────────────────────────────────────────────────

    private readonly CameraCapture     _camera;
    private readonly PoseDetector      _detector;
    private readonly PersonTracker     _tracker;
    private readonly SkeletonRenderer  _renderer;
    private readonly FpsCounter        _fps;

    private bool _disposed;

    // ── Constructor ────────────────────────────────────────────────────────────

    public BodyTrackingPipeline(string modelPath, int cameraIndex = 0)
    {
        _camera   = new CameraCapture(cameraIndex, desiredWidth: 640, desiredHeight: 480);
        _detector = new PoseDetector(modelPath);
        _tracker  = new PersonTracker();
        _renderer = new SkeletonRenderer();
        _fps      = new FpsCounter(windowSize: 30);
    }

    // ── Main loop ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the camera window and runs until the user presses 'Q' / Escape
    /// or no more frames arrive.
    /// </summary>
    public void Run()
    {
        _camera.Start();

        const string windowName = "Body Tracker  –  Press Q to quit";
        Cv2.NamedWindow(windowName, WindowFlags.AutoSize);

        Console.WriteLine("[Pipeline] Starting loop. Press Q or Escape in the window to quit.");

        while (true)
        {
            // ── 1.  Grab a frame ──────────────────────────────────────────────
            using var frame = _camera.ReadFrame();
            if (frame is null)
            {
                Console.WriteLine("[Pipeline] No frame received – stopping.");
                break;
            }

            // ── 2.  Detect pose ───────────────────────────────────────────────
            var pose = _detector.Detect(frame);   // may be null if frame is bad

            // ── 3.  Build the frame result object ─────────────────────────────
            var tracking = new TrackingFrame
            {
                TimestampMs = _fps.ElapsedMs,
                Fps         = _fps.Tick(),
                Bodies      = pose is not null ? new List<BodyPose> { pose } : new()
            };

            // ── 4.  Assign stable person IDs ─────────────────────────────────
            _tracker.Update(tracking.Bodies);

            // ── 5.  Draw overlays ─────────────────────────────────────────────
            using var display = _renderer.Render(frame, tracking);
            Cv2.ImShow(windowName, display);

            // ── 6.  Log to console every 30 frames ───────────────────────────
            if ((int)(_fps.ElapsedMs / 33) % 30 == 0)
                LogFrameInfo(tracking);

            // ── 7.  Check for quit key ────────────────────────────────────────
            int key = Cv2.WaitKey(1);
            if (key == 'q' || key == 'Q' || key == 27 /* Escape */)
            {
                Console.WriteLine("[Pipeline] Quit key pressed.");
                break;
            }
        }

        Cv2.DestroyAllWindows();
    }

    // ── Console logging ────────────────────────────────────────────────────────

    private static void LogFrameInfo(TrackingFrame tracking)
    {
        Console.WriteLine($"[Frame] t={tracking.TimestampMs / 1000.0:F1}s  " +
                          $"FPS={tracking.Fps:F1}  " +
                          $"Bodies={tracking.Bodies.Count}");

        foreach (var body in tracking.Bodies)
        {
            Console.WriteLine($"  Person #{body.PersonId}  bbox={body.BoundingBox}");
            foreach (var kp in body.Keypoints)
                if (kp.Confidence >= 0.4f)
                    Console.WriteLine($"    {kp}");
        }
    }

    // ── IDisposable ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _camera.Dispose();
        _detector.Dispose();
    }
}
