// =============================================================================
//  Rendering/SkeletonRenderer.cs
//  Draws everything onto a copy of the camera frame:
//    • Colour-coded limb lines
//    • Keypoint circles
//    • Bounding box + person ID
//    • FPS / detection count HUD
// =============================================================================

using OpenCvSharp;
using BodyTracker.Models;

namespace BodyTracker.Rendering;

public sealed class SkeletonRenderer
{
    // ── Drawing parameters (feel free to tweak) ────────────────────────────────

    private const int   BoneThickness      = 3;
    private const int   KeypointRadius     = 6;
    private const int   KeypointThickness  = -1;   // -1 = filled circle
    private const float BoxOpacity         = 0.25f;

    private static readonly Scalar KeypointColor  = new(255, 255, 255);   // white
    private static readonly Scalar BboxColor      = new( 60, 200,  60);   // green
    private static readonly Scalar HudBackground  = new( 20,  20,  20);   // dark
    private static readonly Scalar HudText        = new(255, 255, 200);   // cream

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a new frame with the skeleton overlay drawn on top.
    /// The original <paramref name="frame"/> is not modified.
    /// Caller owns the returned Mat and must dispose it.
    /// </summary>
    public Mat Render(Mat frame, TrackingFrame tracking)
    {
        var canvas = frame.Clone();   // work on a copy

        foreach (var body in tracking.Bodies)
        {
            DrawBoundingBox(canvas, body);
            DrawBones(canvas, body);
            DrawKeypoints(canvas, body);
        }

        DrawHud(canvas, tracking);

        return canvas;
    }

    // ── Bounding box ───────────────────────────────────────────────────────────

    private static void DrawBoundingBox(Mat canvas, BodyPose body)
    {
        var box = body.BoundingBox;
        if (box.Width <= 0 || box.Height <= 0) return;

        var topLeft     = new Point((int)box.X,     (int)box.Y);
        var bottomRight = new Point((int)box.Right, (int)box.Bottom);

        // Semi-transparent fill
        using var overlay = canvas.Clone();
        Cv2.Rectangle(overlay, topLeft, bottomRight, BboxColor, -1);
        Cv2.AddWeighted(overlay, BoxOpacity, canvas, 1 - BoxOpacity, 0, canvas);

        // Solid outline
        Cv2.Rectangle(canvas, topLeft, bottomRight, BboxColor, 2);

        // Person ID label
        string label = $"Person #{body.PersonId}";
        Cv2.PutText(canvas, label,
            new Point((int)box.X + 4, (int)box.Y - 6),
            HersheyFonts.HersheySimplex, 0.6, BboxColor, 2);
    }

    // ── Limb lines ─────────────────────────────────────────────────────────────

    private void DrawBones(Mat canvas, BodyPose body)
    {
        foreach (var (bonePair, colour) in SkeletonMap.Bones)
        {
            var kpFrom = body.Keypoints[bonePair.From];
            var kpTo   = body.Keypoints[bonePair.To];

            // Skip the bone if either endpoint is unreliable
            if (!IsVisible(kpFrom) || !IsVisible(kpTo)) continue;

            var ptFrom = new Point((int)kpFrom.X, (int)kpFrom.Y);
            var ptTo   = new Point((int)kpTo.X,   (int)kpTo.Y);
            var scalar = new Scalar(colour.B, colour.G, colour.R);

            Cv2.Line(canvas, ptFrom, ptTo, scalar, BoneThickness, LineTypes.AntiAlias);
        }
    }

    // ── Keypoint dots ──────────────────────────────────────────────────────────

    private void DrawKeypoints(Mat canvas, BodyPose body)
    {
        foreach (var kp in body.Keypoints)
        {
            if (!IsVisible(kp)) continue;

            var center = new Point((int)kp.X, (int)kp.Y);
            Cv2.Circle(canvas, center, KeypointRadius,
                KeypointColor, KeypointThickness, LineTypes.AntiAlias);
        }
    }

    // ── HUD overlay ────────────────────────────────────────────────────────────

    private static void DrawHud(Mat canvas, TrackingFrame tracking)
    {
        // Semi-transparent black bar at the top
        using var bar = canvas.Clone();
        Cv2.Rectangle(bar, new Point(0, 0), new Point(canvas.Width, 36),
            HudBackground, -1);
        Cv2.AddWeighted(bar, 0.6, canvas, 0.4, 0, canvas);

        string text = $"FPS: {tracking.Fps:F1}   Bodies: {tracking.Bodies.Count}" +
                      $"   Time: {tracking.TimestampMs / 1000.0:F1}s";

        Cv2.PutText(canvas, text,
            new Point(8, 24),
            HersheyFonts.HersheySimplex, 0.6, HudText, 1);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private bool IsVisible(Keypoint kp) =>
        kp.Confidence >= 0.25f;   // hardcoded here; could be injected as a param
}
