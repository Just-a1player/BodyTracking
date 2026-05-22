// =============================================================================
//  Detection/PoseDetector.cs
//  Loads a MoveNet SinglePose Lightning ONNX model and runs inference on a
//  single camera frame to return 17 body keypoints.
//
//  How MoveNet works (simplified):
//   1.  Resize the input image to 192×192 (Lightning) or 256×256 (Thunder).
//   2.  Run the neural network.
//   3.  The output tensor has shape [1, 1, 17, 3]:
//       - dimension 2 = 17 keypoints
//       - dimension 3 = [y_norm, x_norm, confidence]   (values 0–1)
//   4.  Multiply y/x by the original frame size to get pixel coordinates.
// =============================================================================

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using BodyTracker.Models;

namespace BodyTracker.Detection;

public sealed class PoseDetector : IDisposable
{
    // ── Constants ──────────────────────────────────────────────────────────────

    private const int ModelInputSize = 192;     // MoveNet Lightning input edge
    public  const int NumKeypoints   = 17;

    /// <summary>
    /// Keypoints with confidence below this value are treated as "not detected"
    /// and will not be drawn.  Raise it to reduce false positives.
    /// </summary>
    public float ConfidenceThreshold { get; set; } = 0.25f;

    // ── Fields ─────────────────────────────────────────────────────────────────

    private readonly InferenceSession _session;
    private bool _disposed;

    // ── Constructor ────────────────────────────────────────────────────────────

    /// <param name="modelPath">
    ///   Path to the MoveNet Lightning ONNX file.
    ///   Download from: https://www.kaggle.com/models/google/movenet/
    ///   (choose "TFLite/ONNX" → movenet_singlepose_lightning)
    /// </param>
    public PoseDetector(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException(
                $"ONNX model not found at '{modelPath}'.\n" +
                "Download movenet_singlepose_lightning.onnx from Kaggle.", modelPath);

        var opts = new SessionOptions();
        opts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

        _session = new InferenceSession(modelPath, opts);
        Console.WriteLine("[PoseDetector] Model loaded successfully.");
        PrintModelInfo();
    }

    // ── Inference ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs pose estimation on one BGR frame.
    /// Returns a <see cref="BodyPose"/> (or null if the frame is empty).
    /// </summary>
    public BodyPose? Detect(Mat frame)
    {
        if (frame is null || frame.Empty()) return null;

        // ── Step 1: pre-process ───────────────────────────────────────────────
        float[] inputData = PreprocessFrame(frame);

        // ── Step 2: build ONNX input tensor  [1, 192, 192, 3] ────────────────
        var shape   = new int[] { 1, ModelInputSize, ModelInputSize, 3 };
        var tensor  = new DenseTensor<float>(inputData, shape);
        var inputs  = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", tensor)
        };

        // ── Step 3: run the network ───────────────────────────────────────────
        using var results = _session.Run(inputs);
        var outputTensor   = results.First().AsTensor<float>();

        // ── Step 4: decode keypoints  [1, 1, 17, 3] ─────────────────────────
        return DecodeKeypoints(outputTensor, frame.Width, frame.Height);
    }

    // ── Pre-processing ─────────────────────────────────────────────────────────

    /// <summary>
    /// Resizes the frame to 192×192, converts BGR→RGB, and normalises
    /// pixel values from [0, 255] to [0.0, 1.0].
    /// </summary>
    private static float[] PreprocessFrame(Mat bgrFrame)
    {
        using var resized = new Mat();
        Cv2.Resize(bgrFrame, resized, new Size(ModelInputSize, ModelInputSize));

        // OpenCV is BGR; MoveNet expects RGB
        using var rgb = new Mat();
        Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

        int pixelCount = ModelInputSize * ModelInputSize * 3;
        float[] data   = new float[pixelCount];

        // Copy and normalise in one pass
        unsafe
        {
            byte* ptr = (byte*)rgb.DataPointer;
            for (int i = 0; i < pixelCount; i++)
                data[i] = ptr[i] / 255f;
        }

        return data;
    }

    // ── Post-processing ────────────────────────────────────────────────────────

    /// <summary>
    /// Converts the raw network output [1, 1, 17, 3] into a <see cref="BodyPose"/>.
    /// Each keypoint entry is [y_norm, x_norm, score].
    /// </summary>
    private BodyPose DecodeKeypoints(Tensor<float> output, int frameW, int frameH)
    {
        var keypoints = new Keypoint[NumKeypoints];

        for (int k = 0; k < NumKeypoints; k++)
        {
            float yNorm      = output[0, 0, k, 0];   // normalised Y
            float xNorm      = output[0, 0, k, 1];   // normalised X
            float confidence = output[0, 0, k, 2];

            keypoints[k] = new Keypoint(
                name:       SkeletonMap.KeypointNames[k],
                x:          xNorm * frameW,
                y:          yNorm * frameH,
                confidence: confidence
            );
        }

        var pose = new BodyPose(keypoints);
        pose.BoundingBox = ComputeBoundingBox(keypoints, frameW, frameH);
        return pose;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a tight bounding box from all high-confidence keypoints.
    /// </summary>
    private BoundingBox ComputeBoundingBox(Keypoint[] kps, int frameW, int frameH)
    {
        float minX = frameW, minY = frameH, maxX = 0, maxY = 0;
        bool  any  = false;

        foreach (var kp in kps)
        {
            if (kp.Confidence < ConfidenceThreshold) continue;
            any   = true;
            minX  = Math.Min(minX, kp.X);
            minY  = Math.Min(minY, kp.Y);
            maxX  = Math.Max(maxX, kp.X);
            maxY  = Math.Max(maxY, kp.Y);
        }

        if (!any) return BoundingBox.Empty;

        const float pad = 20f;
        return new BoundingBox(
            Math.Max(0, minX - pad),
            Math.Max(0, minY - pad),
            Math.Min(frameW, maxX + pad) - Math.Max(0, minX - pad),
            Math.Min(frameH, maxY + pad) - Math.Max(0, minY - pad)
        );
    }

    /// <summary>Prints input/output tensor names to the console (useful for debugging).</summary>
    private void PrintModelInfo()
    {
        Console.WriteLine("[PoseDetector] Inputs:");
        foreach (var meta in _session.InputMetadata)
            Console.WriteLine($"  {meta.Key}  shape={string.Join("×", meta.Value.Dimensions)}");

        Console.WriteLine("[PoseDetector] Outputs:");
        foreach (var meta in _session.OutputMetadata)
            Console.WriteLine($"  {meta.Key}  shape={string.Join("×", meta.Value.Dimensions)}");
    }

    // ── IDisposable ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.Dispose();
    }
}
