// =============================================================================
//  Models/SkeletonMap.cs
//  Defines which keypoints are connected to form the visible skeleton.
//  Edit this file if you want more or fewer limb lines drawn.
// =============================================================================

namespace BodyTracker.Models;

/// <summary>
/// A pair of keypoint indices that should be drawn as a line ("bone").
/// Both indices refer to the 17-keypoint COCO layout (see BodyPose).
/// </summary>
public readonly record struct BonePair(int From, int To);

public static class SkeletonMap
{
    // ---------------------------------------------------------------------------
    //  Limb colours (BGR for OpenCV)  – grouped by body region
    // ---------------------------------------------------------------------------

    // Head
    public static readonly (byte B, byte G, byte R) HeadColor      = (255, 200,  50);
    // Torso
    public static readonly (byte B, byte G, byte R) TorsoColor     = ( 50, 255,  50);
    // Arms
    public static readonly (byte B, byte G, byte R) ArmColor       = ( 50, 150, 255);
    // Legs
    public static readonly (byte B, byte G, byte R) LegColor       = (200,  50, 255);

    // ---------------------------------------------------------------------------
    //  Bones: pairs of keypoint indices that form a limb segment
    // ---------------------------------------------------------------------------
    public static readonly (BonePair Pair, (byte B, byte G, byte R) Color)[] Bones =
    {
        // ── Head ──────────────────────────────────────────────────────────────
        ( new BonePair( 0,  1), HeadColor ),   // nose  → left_eye
        ( new BonePair( 0,  2), HeadColor ),   // nose  → right_eye
        ( new BonePair( 1,  3), HeadColor ),   // left_eye  → left_ear
        ( new BonePair( 2,  4), HeadColor ),   // right_eye → right_ear

        // ── Torso ─────────────────────────────────────────────────────────────
        ( new BonePair( 5,  6), TorsoColor ),  // left_shoulder  → right_shoulder
        ( new BonePair( 5, 11), TorsoColor ),  // left_shoulder  → left_hip
        ( new BonePair( 6, 12), TorsoColor ),  // right_shoulder → right_hip
        ( new BonePair(11, 12), TorsoColor ),  // left_hip       → right_hip

        // ── Left arm ──────────────────────────────────────────────────────────
        ( new BonePair( 5,  7), ArmColor ),    // left_shoulder → left_elbow
        ( new BonePair( 7,  9), ArmColor ),    // left_elbow    → left_wrist

        // ── Right arm ─────────────────────────────────────────────────────────
        ( new BonePair( 6,  8), ArmColor ),    // right_shoulder → right_elbow
        ( new BonePair( 8, 10), ArmColor ),    // right_elbow    → right_wrist

        // ── Left leg ──────────────────────────────────────────────────────────
        ( new BonePair(11, 13), LegColor ),    // left_hip   → left_knee
        ( new BonePair(13, 15), LegColor ),    // left_knee  → left_ankle

        // ── Right leg ─────────────────────────────────────────────────────────
        ( new BonePair(12, 14), LegColor ),    // right_hip  → right_knee
        ( new BonePair(14, 16), LegColor ),    // right_knee → right_ankle
    };

    // Human-readable names in COCO order (index = keypoint id)
    public static readonly string[] KeypointNames =
    {
        "nose",           //  0
        "left_eye",       //  1
        "right_eye",      //  2
        "left_ear",       //  3
        "right_ear",      //  4
        "left_shoulder",  //  5
        "right_shoulder", //  6
        "left_elbow",     //  7
        "right_elbow",    //  8
        "left_wrist",     //  9
        "right_wrist",    // 10
        "left_hip",       // 11
        "right_hip",      // 12
        "left_knee",      // 13
        "right_knee",     // 14
        "left_ankle",     // 15
        "right_ankle",    // 16
    };
}
