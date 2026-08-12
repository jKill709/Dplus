# HubSettings

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Represents complete settings for a Dplus cluster hub device, derived from AppSettings and a ClusterProfile. Used when saving hub configuration to `hubSettings.json`.

## Constructors

### `HubSettings()`

Default constructor initializes with empty values and default flags.

```csharp
public HubSettings()
{
    name = string.Empty;
    role = string.Empty;
    clusterID = string.Empty;
    hubName = string.Empty;
    hubIPaddress = string.Empty;
    hubTelemetryTopic = string.Empty;
    hubCommandTopic = string.Empty;
    nodeTelemetryTopic = string.Empty;
    nodeCommandTopic = string.Empty;
    nodes = new List<NodeInfoForHubSettings>();
    intrinsics = new List<Intrinsics>();
    extrinsics = new List<Extrinsics>();
    rootDir = string.Empty;
    captureDir = string.Empty;
    srcDir = string.Empty;
    logDir = string.Empty;
    modelDir = string.Empty;
    forceExtrinsicsRecalibration = false;
    repropuctionErrThreshAtExtrinsicsCalcualtion = 0.0f;
    maxMQTTQueueSize = 0;
    saveReconstructions = false;
    broadcastReconstructions = false;
    extrinsicsCaptureCount = 0;
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
| `name` | `string` | Hub device name (e.g., "Hub1") |
| `role` | `string` | Always "Hub" for this type |

### Cluster Membership

| Property | Type | Description |
|----------|------|-------------|
| `clusterID` | `string` | Cluster identifier |
| `hubName` | `string` | Hub name (same as name) |
| `hubIPaddress` | `string` | Hub IP address |

### MQTT Topics

| Property | Type | Description |
|----------|------|-------------|
| `hubTelemetryTopic` | `string` | Hub telemetry MQTT topic |
| `hubCommandTopic` | `string` | Hub command MQTT topic |
| `nodeTelemetryTopic` | `string` | Node telemetry MQTT topic |
| `nodeCommandTopic` | `string` | Node command MQTT topic |

### Node Information

| Property | Type | Description |
|----------|------|-------------|
| `nodes` | `List<NodeInfoForHubSettings>` | List of node devices in cluster |

### Calibration Data

| Property | Type | Description |
|----------|------|-------------|
| `intrinsics` | `List<Intrinsics>` | Camera intrinsics for all cameras |
| `extrinsics` | `List<Extrinsics>` | Extrinsic transforms between nodes |

### Directories

| Property | Type | Description |
|----------|------|-------------|
| `rootDir` | `string` | Home directory path |
| `captureDir` | `string` | Captures directory path |
| `srcDir` | `string` | Source files directory path |
| `logDir` | `string` | Logs directory path |
| `modelDir` | `string` | Models directory path |

### Calibration Behavior

| Property | Type | Description |
|----------|------|-------------|
| `forceExtrinsicsRecalibration` | `bool` | Force extrinsics recalculation on startup |
| `repropuctionErrThreshAtExtrinsicsCalcualtion` | `float` | Reproduction error threshold |
| `maxMQTTQueueSize` | `int` | Maximum MQTT queue size |
| `saveReconstructions` | `bool` | Save reconstruction data |
| `broadcastReconstructions` | `bool` | Broadcast reconstructions via MQTT |

### Capture Settings

| Property | Type | Description |
|----------|------|-------------|
| `extrinsicsCaptureCount` | `int` | Number of captures needed for extrinsics |

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
// SettingsManager creates HubSettings from AppSettings and ClusterProfile
HubSettings hubSettings = new HubSettings
{
    name = "Hub1",
    clusterID = "CLUSTER001",
    hubIPaddress = "192.168.1.10",
    hubTelemetryTopic = "dplus/CLUSTER001/hubTelemetry",
    saveReconstructions = true,
    yoloPoseDetSettings.useModel = true,
    yoloPoseDetSettings.modelPath = "/models/yolov8x-pose.onnx"
};

// Save to file
string hubSettingsFile = Path.Combine(Settings.All.SourceFilesDirectory, "hubSettings.json");
string json = JsonSerializer.Serialize(hubSettings, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(hubSettingsFile, json);

// Load from file
string jsonContent = File.ReadAllText(hubSettingsFile);
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
HubSettings loaded = JsonSerializer.Deserialize<HubSettings>(jsonContent, options);

Console.WriteLine($"Loaded hub: {loaded.name}");
Console.WriteLine($"Using pose model: {loaded.yoloPoseDetSettings.modelPath}");
```

## Related Types

- `AppSettings` - Source of settings data
- `ClusterProfile` - Configuration referenced by profile names
- `NodeInfoForHubSettings`, `Intrinsics`, `Extrinsics` - Node and calibration data
- `YoloPoseDetectorProfile`, `YoloObjectDetectorProfile`, `FaceIDDetectorProfile`
- `ChArUcoBoardDetectorProfileForHubSetting`, `ChessboardDetectorProfileForHubSetting`
