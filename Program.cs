// =============================================================================
//  Program.cs  –  Entry point
//  Usage:
//    dotnet run -- --model path/to/movenet.onnx [--camera 0]
//
//  Quick-start:
//    1. Download the model:
//       https://www.kaggle.com/models/google/movenet/  (Lightning ONNX)
//    2. dotnet run -- --model movenet_singlepose_lightning.onnx
// =============================================================================

using BodyTracker;

// ── Parse command-line arguments ──────────────────────────────────────────────

string? modelPath    = null;
int     cameraIndex  = 0;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--model"  && i + 1 < args.Length) modelPath   = args[++i];
    if (args[i] == "--camera" && i + 1 < args.Length) cameraIndex = int.Parse(args[++i]);
}

if (modelPath is null)
{
    Console.WriteLine("""
    ╔══════════════════════════════════════════════════════════════╗
    ║                 Body Tracker – Usage                         ║
    ╠══════════════════════════════════════════════════════════════╣
    ║  dotnet run -- --model <path_to_model.onnx> [--camera N]     ║
    ║                                                              ║
    ║  Model download:                                             ║
    ║    https://www.kaggle.com/models/google/movenet/             ║
    ║    → Pick "TFLite/ONNX" → movenet_singlepose_lightning.onnx  ║
    ║                                                              ║
    ║  --camera N   Use camera device N (default: 0)               ║
    ╚══════════════════════════════════════════════════════════════╝
    """);
    return 1;
}

// ── Run the tracker ────────────────────────────────────────────────────────────

Console.WriteLine($"[Main] Model : {modelPath}");
Console.WriteLine($"[Main] Camera: device #{cameraIndex}");

try
{
    using var pipeline = new BodyTrackingPipeline(modelPath, cameraIndex);
    pipeline.Run();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[Error] {ex.Message}");
    Console.ResetColor();
    return 1;
}

return 0;
