# YoloPoseDetectorProfileForHubSetting

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Represents YOLO pose detection settings as applied to a hub. Contains the profile name, enabled flags, model path, and all threshold values.

## Constructors

### `YoloPoseDetectorProfileForHubSetting()`

Default constructor initializes with model disabled and default thresholds.

```csharp
public YoloPoseDetectorProfileForHubSetting()
{
    name = string.Empty;
    useModel = false;
    saveWholeDetectionImage = false;
    savePartialDetectionImage = false;
    modelPath = string.Empty;
    cocoPKcount = 17;
    detectConfThreshold = 0.0;
    kpDetectThreshold = 0.0;
    nmsThreshold = 0.0;
    iouThreshold = 0.0;
    ReconstructionThreshold = 0.0;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string` | Profile name (matches a profile in YoloPoseDetProfiles) |
| `useModel` | `bool` | Enable pose model |
| `saveWholeDetectionImage` | `bool` | Save full images with detections |
| `savePartialDetectionImage` | `bool` | Save cropped detection images |
| `modelPath` | `string` | Path to ONNX model file |
| `cocoPKcount` | `int` | COCO keypoints count (default: 17) |
| `detectConfThreshold` | `double` | Detection confidence threshold |
| `kpDetectThreshold` | `double` | Keypoint detection threshold |
| `nmsThreshold` | `double` | NMS IoU threshold |
| `iouThreshold` | `double` | Detection IoU threshold |
| `ReconstructionThreshold` | `double` | Reconstruction quality threshold |

## Usage Example

```csharp
// Create pose detection settings
YoloPoseDetectorProfileForHubSetting settings = new YoloPoseDetectorProfileForHubSetting
{
    name = "Standard",
    useModel = true,
    modelPath = "/models/yolov8x-pose.onnx",
    detectConfThreshold = 0.5,
    kpDetectThreshold = 0.7,
    nmsThreshold = 0.4,
    iouThreshold = 0.5,
    saveWholeDetectionImage = true
};

// Access from hub settings
var hubSettings = Settings.All.Hubs.FirstOrDefault(h => h.name == "Hub1");
if (hubSettings != null)
{
    var poseSettings = hubSettings.yoloPoseDetSettings;
    Console.WriteLine($"Using model: {poseSettings.useModel}");
    Console.WriteLine($"Model path: {poseSettings.modelPath}");
    Console.WriteLine($"Detection threshold: {poseSettings.detectConfThreshold:F2}");
    Console.WriteLine($"Keypoint threshold: {poseSettings.kpDetectThreshold:F2}");
}
```

## Related Types

- `AppSettings` - YoloPoseDetProfiles collection and accessor methods
- `HubSettings` - yoloPoseDetSettings property
- `YoloPoseDetectorProfile` - Source profile definition
- `YoloObjectDetectorProfileForHubSetting` - Similar structure for objects
