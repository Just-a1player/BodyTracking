// =============================================================================
//  Models/BodyModels.cs
//  Pure data classes – no logic, no dependencies.
//  Everything the rest of the app passes around lives here.
// =============================================================================

namespace BodyTracker.Models;

// ---------------------------------------------------------------------------
//  Single 2-D point on the body (e.g. left elbow, right knee …)
// ---------------------------------------------------------------------------
public sealed class Keypoint
{
    /// <summary>Name label shown on screen (e.g. "nose", "left_knee").</summary>
    public string Name { get; }

    /// <summary>Pixel X coordinate in the camera frame.</summary>
    public float X { get; set; }

    /// <summary>Pixel Y coordinate in the camera frame.</summary>
    public float Y { get; set; }

    /// <summary>
    /// How confident the model is about this point (0 = unsure, 1 = certain).
    /// We skip drawing points below a threshold to reduce noise.
    /// </summary>
    public float Confidence { get; set; }

    public Keypoint(string name, float x, float y, float confidence)
    {
        Name       = name;
        X          = x;
        Y          = y;
        Confidence = confidence;
    }

    public override string ToString() =>
        $"{Name} ({X:F0},{Y:F0}) conf={Confidence:P0}";
}

// ---------------------------------------------------------------------------
//  A full-body pose: 17 keypoints from the MoveNet skeleton definition
// ---------------------------------------------------------------------------
public sealed class BodyPose
{
    // MoveNet / COCO 17-keypoint order:
    // 0  nose          1  left_eye       2  right_eye
    // 3  left_ear      4  right_ear      5  left_shoulder
    // 6  right_shoulder 7 left_elbow    8  right_elbow
    // 9  left_wrist    10 right_wrist   11 left_hip
    // 12 right_hip     13 left_knee     14 right_knee
    // 15 left_ankle    16 right_ankle
    public Keypoint[] Keypoints { get; }

    /// <summary>Axis-aligned bounding box inferred from the keypoints.</summary>
    public BoundingBox BoundingBox { get; set; } = BoundingBox.Empty;

    /// <summary>Arbitrary integer so the UI can track the same person frame-to-frame.</summary>
    public int PersonId { get; set; }

    public BodyPose(Keypoint[] keypoints)
    {
        if (keypoints.Length != 17)
            throw new ArgumentException("MoveNet expects exactly 17 keypoints.", nameof(keypoints));
        Keypoints = keypoints;
    }
}

// ---------------------------------------------------------------------------
//  Axis-aligned bounding box
// ---------------------------------------------------------------------------
public sealed class BoundingBox
{
    public float X      { get; set; }   // left
    public float Y      { get; set; }   // top
    public float Width  { get; set; }
    public float Height { get; set; }

    public float Right  => X + Width;
    public float Bottom => Y + Height;

    public static readonly BoundingBox Empty = new(0, 0, 0, 0);

    public BoundingBox(float x, float y, float width, float height)
    {
        X = x; Y = y; Width = width; Height = height;
    }

    public override string ToString() =>
        $"[{X:F0},{Y:F0} – {Width:F0}×{Height:F0}]";
}

// ---------------------------------------------------------------------------
//  One processed frame handed back to the main loop
// ---------------------------------------------------------------------------
public sealed class TrackingFrame
{
    /// <summary>Timestamp in milliseconds since the tracker started.</summary>
    public long TimestampMs { get; set; }

    /// <summary>All bodies detected in this frame.</summary>
    public List<BodyPose> Bodies { get; set; } = new();

    /// <summary>Frames-per-second at the moment this frame was produced.</summary>
    public double Fps { get; set; }
}
