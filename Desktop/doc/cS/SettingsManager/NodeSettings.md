# NodeSettings

**Namespace:** `Dplus_Desktop.SettingsManager`

## Purpose

Represents complete settings for a Dplus cluster node device, derived from AppSettings and a ClusterProfile. Used when saving node configuration to `{nodeName}Settings.json`.

## Constructors

### `NodeSettings()`

Default constructor initializes with empty values and default flags.

```csharp
public NodeSettings()
{
    name = string.Empty;
    role = string.Empty;
    isActive = false;
    clusterID = string.Empty;
    hubName = string.Empty;
    hubIPaddress = string.Empty;
    nodeTelemetryTopic = string.Empty;
    nodeCommandTopic = string.Empty;
    rootDir = string.Empty;
    captureDir = string.Empty;
    srcDir = string.Empty;
    logDir = string.Empty;
    modelDir = string.Empty;
    maxFrameLatenessMs = 0;
    forceIntrinsicsRecalibration = false;
    intrinsics = new Intrinsics();
    intrinsicsCaptureCount = 0;
    yoloPoseDetSettings = new YoloPoseDetectorProfile();
    yoloObjectDetSettings = new YoloObjectDetectorProfile();
    faceIDDetSettings = new FaceIDDetectorProfile();
    chArUcoBoardDetSettings = new ChArUcoBoardDetectorProfileForHubSetting();
    chessboardDetSettings = new ChessboardDetectorProfileForHubSetting();
    captureOnStartup = false;
    captureEachFrame = false;
    captureEachDetection = false;
    captureNewDetection = false;
    targetFrameRate = 0.0;
    indicatorType = string.Empty;
    introSequenceIterations = 0;
    introSequenceDelay = 0;
}
```

## Properties

### Device Identification

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string` | Node device name (e.g., "Node1") |
| `role` | `string` | Always "Node" for this type |
| `isActive` | `bool` | Whether node is currently active |

### Cluster Membership

| Property | Type | Description |
|----------|------|-------------|
| `clusterID` | `string` | Cluster identifier |
| `hubName` | `string` | Hub name |
| `hubIPaddress` | `string` | Hub IP address |

### MQTT Topics

| Property | Type | Description |
|----------|------|-------------|
| `nodeTelemetryTopic` | `string` | Node telemetry MQTT topic |
| `nodeCommandTopic` | `string` | Node command MQTT topic |

### Directories

| Property | Type | Description |
|----------|------|-------------|
| `rootDir` | `string` | Home directory path |
| `captureDir` | `string` | Captures directory path |
| `srcDir` | `string` | Source files directory path |
| `logDir` | `string` | Logs directory path |
| `modelDir` | `string` | Models directory path |

### Latency and Calibration

| Property | Type | Description |
|----------|------|-------------|
| `maxFrameLatenessMs` | `int` | Maximum acceptable frame latency |
| `forceIntrinsicsRecalibration` | `bool` | Force intrinsics recalculation on startup |
| `intrinsics` | `Intrinsics?` | Camera intrinsics (null if none) |
| `intrinsicsCaptureCount` | `int` | Number of captures needed for intrinsics |

### Detector Profiles

Each property is a detector profile with all settings:

| Property | Type | Description |
|----------|------|-------------|
| `yoloPoseDetSettings` | `YoloPoseDetectorProfile` | Pose detection settings |
| `yoloObjectDetSettings` | `YoloObjectDetectorProfile` | Object detection settings |
| `faceIDDetSettings` | `FaceIDDetectorProfile` | Face recognition settings |
| `chArUcoBoardDetSettings` | `ChArUcoBoardDetectorProfileForHubSetting` | ChArUco board detection settings |
| `chessboardDetSettings` | `ChessboardDetectorProfileForHubSetting` | Chessboard detection settings |

### Operational Parameters

| Property | Type | Description |
|----------|------|-------------|
| `captureOnStartup` | `bool` | Capture on startup |
| `captureEachFrame` | `bool` | Capture each frame |
| `captureEachDetection` | `bool` | Capture each detection |
| `captureNewDetection` | `bool` | Capture new detections only |
| `targetFrameRate` | `double` | Target frames per second |
| `indicatorType` | `string` | Visual indicator type |
| `introSequenceIterations` | `int` | Intro sequence iterations |
| `introSequenceDelay` | `int` | Intro delay in ms |

## Usage Example

```csharp
// SettingsManager creates NodeSettings from AppSettings and ClusterProfile
NodeSettings nodeSettings = new NodeSettings
{
    name = "Node1",
    clusterID = "CLUSTER001",
    hubName = "Hub1",
    hubIPaddress = "192.168.1.10",
    nodeTelemetryTopic = "dplus/CLUSTER001/nodeTelemetry",
    maxFrameLatenessMs = 50,
    yoloPoseDetSettings.useModel = true,
    yoloPoseDetSettings.modelPath = "/models/yolov8x-pose.onnx"
};

// Save to file
string nodeSettingsFile = Path.Combine(Settings.All.SourceFilesDirectory, "Node1Settings.json");
string json = JsonSerializer.Serialize(nodeSettings, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(nodeSettingsFile, json);

// Load from file
string jsonContent = File.ReadAllText(nodeSettingsFile);
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
NodeSettings loaded = JsonSerializer.Deserialize<NodeSettings>(jsonContent, options);

Console.WriteLine($"Loaded node: {loaded.name}");
Console.WriteLine($"Max frame lateness: {loaded.maxFrameLatenessMs}ms");
```

## Related Types

- `AppSettings` - Source of settings data
- `ClusterProfile` - Configuration referenced by profile names
- `HubSettings` - Companion hub configuration
- `Intrinsics`, `NodeInfoForHubSettings` - Calibration and hub data
- `YoloPoseDetectorProfile`, `YoloObjectDetectorProfile`, `FaceIDDetectorProfile`
- `ChArUcoBoardDetectorProfileForHubSetting`, `ChessboardDetectorProfileForHubSetting`
