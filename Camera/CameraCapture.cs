// =============================================================================
//  Camera/CameraCapture.cs
//  Wraps an OpenCV VideoCapture so the rest of the app never touches it
//  directly.  Call Start() once, then poll ReadFrame() in your loop.
// =============================================================================

using OpenCvSharp;

namespace BodyTracker.Camera;

public sealed class CameraCapture : IDisposable
{
    // ── Fields ────────────────────────────────────────────────────────────────

    private readonly VideoCapture _capture;
    private bool _disposed;

    // ── Public properties ──────────────────────────────────────────────────────

    /// <summary>Width of each frame in pixels.</summary>
    public int FrameWidth  { get; private set; }

    /// <summary>Height of each frame in pixels.</summary>
    public int FrameHeight { get; private set; }

    /// <summary>Hardware-reported FPS of the camera (may not match actual FPS).</summary>
    public double NominalFps { get; private set; }

    /// <summary>True once Start() has succeeded.</summary>
    public bool IsRunning { get; private set; }

    // ── Constructor ────────────────────────────────────────────────────────────

    /// <param name="deviceIndex">
    ///   Camera device index.  0 is usually the built-in webcam; 1 is an
    ///   external USB camera, etc.
    /// </param>
    /// <param name="desiredWidth">Requested capture width (camera may override).</param>
    /// <param name="desiredHeight">Requested capture height (camera may override).</param>
    public CameraCapture(int deviceIndex = 0, int desiredWidth = 640, int desiredHeight = 480)
    {
        _capture = new VideoCapture(deviceIndex, VideoCaptureAPIs.ANY);

        // Ask the camera for a specific resolution.
        // The driver will pick the closest supported mode.
        _capture.Set(VideoCaptureProperties.FrameWidth,  desiredWidth);
        _capture.Set(VideoCaptureProperties.FrameHeight, desiredHeight);
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the camera and reads back the actual resolution the driver chose.
    /// Throws <see cref="InvalidOperationException"/> if the camera cannot be opened.
    /// </summary>
    public void Start()
    {
        if (!_capture.IsOpened())
            throw new InvalidOperationException(
                "Could not open the camera.  Make sure a webcam is connected.");

        // Read back the resolution the driver actually gave us
        FrameWidth  = (int)_capture.Get(VideoCaptureProperties.FrameWidth);
        FrameHeight = (int)_capture.Get(VideoCaptureProperties.FrameHeight);
        NominalFps  =      _capture.Get(VideoCaptureProperties.Fps);

        IsRunning = true;
        Console.WriteLine($"[Camera] Opened at {FrameWidth}×{FrameHeight} @ {NominalFps:F1} fps");
    }

    // ── Frame reading ──────────────────────────────────────────────────────────

    /// <summary>
    /// Grabs the next frame from the camera.
    /// Returns <c>null</c> if the frame could not be read (end-of-stream, error).
    /// The caller is responsible for disposing the returned Mat.
    /// </summary>
    public Mat? ReadFrame()
    {
        if (!IsRunning)
            throw new InvalidOperationException("Call Start() before ReadFrame().");

        var frame = new Mat();
        bool ok = _capture.Read(frame);

        if (!ok || frame.Empty())
        {
            frame.Dispose();
            return null;
        }

        return frame;   // BGR, 8-bit per channel
    }

    // ── IDisposable ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed  = true;
        IsRunning  = false;
        _capture.Release();
        _capture.Dispose();
    }
}
