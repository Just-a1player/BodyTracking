# Body Tracker — C# / .NET 8

A clean, well-commented body-tracking application that uses:

| Component | Technology |
|---|---|
| Camera input | **OpenCvSharp4** (OpenCV wrapper) |
| Pose estimation | **MoveNet Lightning** (ONNX via Microsoft.ML.OnnxRuntime) |
| Person re-ID | IoU-based bounding-box tracker |
| Visualisation | OpenCV drawing + overlay |

---

## Project Structure

```
BodyTracker/
├── Program.cs                   ← Entry point, argument parsing
├── BodyTrackingPipeline.cs      ← Central orchestrator (the "glue")
│
├── Models/
│   ├── BodyModels.cs            ← Keypoint, BodyPose, BoundingBox, TrackingFrame
│   └── SkeletonMap.cs           ← Which joints connect + colours
│
├── Camera/
│   └── CameraCapture.cs         ← Webcam input via OpenCV
│
├── Detection/
│   ├── PoseDetector.cs          ← MoveNet ONNX inference
│   └── PersonTracker.cs         ← Stable IDs using IoU matching
│
├── Rendering/
│   └── SkeletonRenderer.cs      ← Draws bones, keypoints, HUD
│
└── Utils/
    └── FpsCounter.cs            ← Rolling-window FPS meter
```

---

## Quick Start

### 1 · Get the ONNX model

1. Go to **https://www.kaggle.com/models/google/movenet**
2. Select **TFLite/ONNX** → **movenet_singlepose_lightning**
3. Download and place the `.onnx` file next to the project

### 2 · Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A USB or built-in webcam
- Windows / Linux / macOS

### 3 · Run

```bash
cd BodyTracker
dotnet run -- --model movenet_singlepose_lightning.onnx
# Use a different camera (e.g. USB cam):
dotnet run -- --model movenet_singlepose_lightning.onnx --camera 1
```

Press **Q** or **Escape** in the video window to quit.

---

## How It Works — Data Flow

```
Camera frame (BGR Mat)
        │
        ▼
  PoseDetector.Detect()
   • Resize to 192×192
   • BGR → RGB, normalise to [0,1]
   • Run MoveNet ONNX
   • Decode 17 keypoints (x, y, confidence)
   • Compute bounding box
        │
        ▼
  PersonTracker.Update()
   • Match new boxes to previous tracks via IoU
   • Assign stable integer Person IDs
        │
        ▼
  SkeletonRenderer.Render()
   • Draw bounding boxes (semi-transparent fill)
   • Draw colour-coded limb lines (bones)
   • Draw keypoint dots (only if confidence ≥ 0.25)
   • Draw FPS / body count HUD
        │
        ▼
  Cv2.ImShow()  →  screen
```

---

## Keypoint Layout (COCO 17)

```
        0 (nose)
       / \
      1   2  (eyes)
      |   |
      3   4  (ears)

      5 ─── 6   (shoulders)
      |     |
      7     8   (elbows)
      |     |
      9    10   (wrists)

     11 ─── 12  (hips)
      |     |
     13    14  (knees)
      |     |
     15    16  (ankles)
```

---

## Extending the Project

| Goal | Where to edit |
|---|---|
| Different colours for limbs | `Models/SkeletonMap.cs` — change `HeadColor`, `ArmColor` … |
| Lower/raise detection sensitivity | `Detection/PoseDetector.cs` — `ConfidenceThreshold` |
| Multi-person support | Replace `PoseDetector` with MoveNet MultiPose ONNX |
| Save output video | Add `VideoWriter` calls in `BodyTrackingPipeline.cs` |
| Export keypoints to CSV/JSON | Add a logger class and call it in `BodyTrackingPipeline.LogFrameInfo()` |
