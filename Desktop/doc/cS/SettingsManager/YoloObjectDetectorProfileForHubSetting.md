# YoloObjectDetectorProfileForHubSetting

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Represents YOLO object detection settings as applied to a hub. Contains the profile name, enabled flags, model path, classes filter, and all threshold values.

## Constructors

### `YoloObjectDetectorProfileForHubSetting()`

Default constructor initializes with model disabled and default thresholds.

```csharp
public YoloObjectDetectorProfileForHubSetting()
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
YoloObjectDetectorProfileForHubSetting settings = new YoloObjectDetectorProfileForHubSetting
{
    name = "Standard",
    useModel = true,
    modelPath = "/models/yolov8x.pt",
    classes = "0,1,2,3",  // Filter to specific classes
    objectConfidence = 0.5,
    iouThreshold = 0.4,
    saveWholeDetectionImage = true
};

// Access from hub settings
var hubSettings = Settings.All.Hubs.FirstOrDefault(h => h.name == "Hub1");
if (hubSettings != null)
{
    var objSettings = hubSettings.yoloObjectDetSettings;
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
```

## Related Types

- `AppSettings` - YoloObjectDetProfiles collection and accessor methods
- `HubSettings` - yoloObjectDetSettings property
- `YoloObjectDetectorProfile` - Source profile definition
- `YoloPoseDetectorProfileForHubSetting` - Similar structure for pose
