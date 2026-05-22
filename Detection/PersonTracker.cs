// =============================================================================
//  Detection/PersonTracker.cs
//  Assigns a stable integer ID to each detected body across frames.
//
//  Algorithm:
//   - Keep a list of "tracked" bounding boxes from the previous frame.
//   - For each new detection, compute IoU (Intersection-over-Union) with every
//     tracked box.  Match the pair with the highest IoU (if IoU > threshold).
//   - Unmatched detections get a fresh ID.
//   - Tracks unseen for too many frames are removed.
//
//  IoU = (area of overlap) / (area of union)
//  A value of 1.0 = perfect overlap; 0.0 = no overlap at all.
// =============================================================================

using BodyTracker.Models;

namespace BodyTracker.Detection;

public sealed class PersonTracker
{
    // ── Configuration ──────────────────────────────────────────────────────────

    /// <summary>
    /// Minimum IoU required to consider two boxes "the same person".
    /// Raise for stricter matching; lower if people move very fast.
    /// </summary>
    public float IouThreshold { get; set; } = 0.3f;

    /// <summary>Frames a track can go unmatched before it's deleted.</summary>
    public int MaxMissingFrames { get; set; } = 15;

    // ── Internal state ─────────────────────────────────────────────────────────

    private readonly List<Track> _tracks  = new();
    private int _nextId = 1;

    private sealed class Track
    {
        public int         Id             { get; }
        public BoundingBox LastBox        { get; set; }
        public int         FramesMissing  { get; set; }

        public Track(int id, BoundingBox box)
        { Id = id; LastBox = box; }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the tracker with the new frame's detections.
    /// Sets the <see cref="BodyPose.PersonId"/> field on each pose in place.
    /// </summary>
    public void Update(List<BodyPose> detections)
    {
        // Mark all existing tracks as "not yet matched this frame"
        var matched = new HashSet<int>();

        foreach (var pose in detections)
        {
            var box  = pose.BoundingBox;
            int bestId    = -1;
            float bestIou = IouThreshold;   // Only match if IoU exceeds threshold

            // Find the best matching track
            foreach (var track in _tracks)
            {
                float iou = ComputeIou(box, track.LastBox);
                if (iou > bestIou)
                {
                    bestIou = iou;
                    bestId  = track.Id;
                }
            }

            if (bestId >= 0)
            {
                // Found a match – update the existing track
                var track        = _tracks.First(t => t.Id == bestId);
                track.LastBox    = box;
                track.FramesMissing = 0;
                pose.PersonId    = bestId;
                matched.Add(bestId);
            }
            else
            {
                // No match – start a new track
                var newTrack = new Track(_nextId++, box);
                _tracks.Add(newTrack);
                pose.PersonId = newTrack.Id;
                matched.Add(newTrack.Id);
            }
        }

        // Age unmatched tracks; remove stale ones
        foreach (var track in _tracks)
            if (!matched.Contains(track.Id))
                track.FramesMissing++;

        _tracks.RemoveAll(t => t.FramesMissing > MaxMissingFrames);
    }

    // ── IoU maths ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes Intersection-over-Union for two axis-aligned bounding boxes.
    /// Returns a value between 0 (no overlap) and 1 (identical boxes).
    /// </summary>
    private static float ComputeIou(BoundingBox a, BoundingBox b)
    {
        // Intersection rectangle
        float interLeft   = Math.Max(a.X,      b.X);
        float interTop    = Math.Max(a.Y,      b.Y);
        float interRight  = Math.Min(a.Right,  b.Right);
        float interBottom = Math.Min(a.Bottom, b.Bottom);

        float interW = interRight  - interLeft;
        float interH = interBottom - interTop;

        if (interW <= 0 || interH <= 0) return 0f;

        float interArea = interW * interH;
        float areaA     = a.Width * a.Height;
        float areaB     = b.Width * b.Height;
        float unionArea = areaA + areaB - interArea;

        return unionArea > 0 ? interArea / unionArea : 0f;
    }
}
