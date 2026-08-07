# YoloObjectDetectorProfile

**Namespace:** `Dplus_Desktop.SettingsManager`

## Purpose

Represents YOLO object detection settings as applied to a node. Contains the profile name, enabled flags, model path, classes filter, and all threshold values.

## Constructors

### `YoloObjectDetectorProfile()`

Default constructor initializes with model disabled and default thresholds.

```csharp
public YoloObjectDetectorProfile()
{
    name = string.Empty;
    useModel = false;
    saveWholeDetectionImage = false;
    savePartialDetectionImage = false;
    modelPath = string.Empty;
    classes = string.Empty;
    objectConfidence = 0.0;
    iouThreshold = 0.0;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string` | Profile name (matches a profile in YoloObjectDetProfiles) |
| `useModel` | `bool` | Enable object model |
| `saveWholeDetectionImage` | `bool` | Save full images with detections |
| `savePartialDetectionImage` | `bool` | Save cropped detection images |
| `modelPath` | `string` | Path to ONNX model file |
| `classes` | `string` | Comma-separated class IDs or names for filtering |
| `objectConfidence` | `double` | Object confidence threshold |
| `iouThreshold` | `double` | NMS IoU threshold |

## Usage Example

```csharp
// Create object detection settings
YoloObjectDetectorProfile settings = new YoloObjectDetectorProfile
{
    name = "Standard",
    useModel = true,
    modelPath = "/models/yolov8x.pt",
    classes = "0,1,2,3",  // Filter to specific classes
    objectConfidence = 0.5,
    iouThreshold = 0.4,
    saveWholeDetectionImage = true
};

// Access from node settings
var nodeSettings = Settings.All.Nodes.FirstOrDefault(n => n.name == "Node1");
if (nodeSettings != null)
{
    var objSettings = nodeSettings.yoloObjectDetSettings;
    Console.WriteLine($"Using model: {objSettings.useModel}");
    Console.WriteLine($"Model path: {objSettings.modelPath}");
    Console.WriteLine($"Confidence threshold: {objSettings.objectConfidence:F2}");
    
    // Parse classes list
    string[] classList = objSettings.classes.Split(',');
    foreach (var cls in classList)
    {
        Console.WriteLine($"Class: {cls.Trim()}");
    }
}

// Modify settings
objSettings.useModel = false;
```

## Related Types

- `AppSettings` - YoloObjectDetProfiles collection and accessor methods
- `NodeSettings` - yoloObjectDetSettings property
- `YoloObjectDetectorProfileForHubSetting` - Similar structure for hub
- `YoloPoseDetectorProfile` - Similar structure for pose
