# AppSettings

**Namespace:** `Dplus_Desktop.SettingsManager`

## Purpose

Central configuration settings container for the Dplus Desktop application. Manages all hub/node cluster settings including device configurations, calibration data, detector profiles, MQTT topics, and directory paths. Settings are persisted to `managerSettings.json`.

## Constructors

### `AppSettings()`

Default constructor initializes all property collections with empty defaults.

```csharp
public AppSettings()
```

## Properties

### Directory Paths

| Property | Type | Description |
|----------|------|-------------|
| `RemoteHomePath` | `string` | Path to remote home directory on hub device |
| `RemoteCapturesPath` | `string` | Path to remote captures directory |
| `RemoteReconstructionsPath` | `string` | Path to remote reconstructions directory |
| `RemoteLogPath` | `string` | Path to remote logs directory |
| `RemoteModelsPath` | `string` | Path to remote models directory |
| `UploadDirectory` | `string` | Local upload directory for files |
| `LocalLogPath` | `string` | Local path for downloaded logs and reconstructions |
| `LocalModelsPath` | `string` | Local path for model files |
| `SourceFilesDirectory` | `string` | Directory containing source files for upload |

### Viewer Settings

| Property | Type | Description |
|----------|------|-------------|
| `NoImagePath` | `string` | Path to image to display when no content available |
| `LiveFrameBufferLimit` | `int` | Maximum number of live frames to buffer (default: 1) |
| `SavedFrameBufferLimit` | `int` | Maximum number of saved frames to keep in UI (default: 1) |

### MQTT Topics

| Property | Type | Description |
|----------|------|-------------|
| `HubTelemetryTopic` | `string` | MQTT topic for hub telemetry data |
| `HubCommandTopic` | `string` | MQTT topic for sending commands to hub |
| `NodeTelemetryTopic` | `string` | MQTT topic for node telemetry data |
| `NodeCommandTopic` | `string` | MQTT topic for sending commands to nodes |

### Cluster Behavior

| Property | Type | Description |
|----------|------|-------------|
| `ForceExtrinsicsRecalibration` | `bool` | Force recalculation of extrinsics on startup |
| `RepropuctionErrThreshAtExtrinsicsCalcualtion` | `float` | Reproduction error threshold for extrinsics calc |
| `MaxMQTTQueueSize` | `int` | Maximum MQTT message queue size (0 = unlimited) |
| `SaveReconstructions` | `bool` | Save reconstruction data locally |
| `BroadcastReconstructions` | `bool` | Broadcast reconstructions via MQTT |

### Node Behavior

| Property | Type | Description |
|----------|------|-------------|
| `MaxFrameLatenessMs` | `int` | Maximum acceptable frame latency in ms |
| `ForceIntrinsicsRecalibration` | `bool` | Force recalculation of intrinsics on startup |

### Detector Profiles Collections

| Property | Type | Description |
|----------|------|-------------|
| `YoloPoseDetProfiles` | `List<YoloPoseDetectorProfile>` | Pose detection profiles per cluster |
| `YoloObjectDetProfiles` | `List<YoloObjectDetectorProfile>` | Object detection profiles per cluster |
| `FaceIDDetProfiles` | `List<FaceIDDetectorProfile>` | Face recognition profiles per cluster |
| `ChArUcoBoardDetProfiles` | `List<ChArUcoBoardDetectorProfile>` | ChArUco board detection profiles |
| `ChessboardDetProfiles` | `List<ChessboardDetectorProfile>` | Chessboard detection profiles |

### Device Collections

| Property | Type | Description |
|----------|------|-------------|
| `Hubs` | `List<Device>` | List of hub devices |
| `Nodes` | `List<Device>` | List of node devices |
| `Intrinsics` | `List<Intrinsics>` | Camera intrinsics calibration data |
| `Extrinsics` | `List<Extrinsics>` | Node-to-node extrinsics calibration data |
| `SourceFiles` | `List<SourceFile>` | Files to upload to devices |
| `RuntimeFiles` | `List<RuntimeFile>` | Runtime files for device distribution |
| `Models` | `List<ModelFile>` | Model files for distribution |

### Cluster Profiles

| Property | Type | Description |
|----------|------|-------------|
| `ClusterProfileToUse` | `string` | Name of active cluster profile |
| `ClusterProfiles` | `List<ClusterProfile>` | All available cluster profiles |

### Chessboards and ChArUco Boards

| Property | Type | Description |
|----------|------|-------------|
| `Chessboards` | `List<ChessboardParameters>` | Defined chessboard parameters |
| `chArUcoBoards` | `List<ChArUcoBoardParameters>` | Defined ChArUco board parameters |

## Methods

### `GetClusterProfile()`

Returns the currently active cluster profile. Throws `ArgumentException` if no profile is selected or the selected profile doesn't exist.

```csharp
public ClusterProfile GetClusterProfile()
```

### `GetDeviceByName(string name)`

Retrieves a device by name from either hubs or nodes list. Throws `ArgumentException` if no device with the given name exists.

```csharp
public Device? GetDeviceByName(string name)
```

**Parameters:**
- `name`: The device name to search for

**Returns:** `Device?` - The matching device, or null if not found (though this is unlikely given validation)

### `GetNodesByClusterID(string clusterID, bool getHub = false, bool onlyActive = true)`

Retrieves all nodes belonging to a specific cluster.

```csharp
public List<Device> GetNodesByClusterID(string clusterID, bool getHub = false, bool onlyActive = true)
```

**Parameters:**
- `clusterID`: The cluster identifier
- `getHub`: Include hub in results (default: false)
- `onlyActive`: Only return active devices (default: true)

**Returns:** `List<Device>` - Nodes in the cluster, with optional hub at start

### `GetIntrinsicsForCameraID(int cameraID)`

Retrieves intrinsics calibration for a specific camera ID. Returns best RMS match or empty intrinsics if not found.

```csharp
public Intrinsics GetIntrinsicsForCameraID(int cameraID)
```

**Returns:** `Intrinsics` - Camera intrinsic parameters

### `GetIntrinsicsForNode(string nodeName)`

Retrieves intrinsics for a specific node by name.

```csharp
public Intrinsics GetIntrinsicsForNode(string nodeName)
```

**Returns:** `Intrinsics` - Node's camera intrinsics

### `GetExtrinsicsForNode(string baseNodeName, string targetNodeName)`

Retrieves extrinsic transformation from one node to another.

```csharp
public Extrinsics GetExtrinsicsForNode(string baseNodeName, string targetNodeName)
```

**Returns:** `Extrinsics` - Transformation data

### Detector Profile Accessors

All methods throw `ArgumentException` if the requested profile doesn't exist:

```csharp
public YoloPoseDetectorProfile getYoloPoseDetectorProfileByClusterID(string clusterID)
public YoloObjectDetectorProfile getYoloObjectDetectorProfileByClusterID(string clusterID)
public FaceIDDetectorProfile getFaceIDDetectorProfileByClusterID(string clusterID)
public ChArUcoBoardDetectorProfile getChArUcoBoardDetectorProfileByClusterID(string clusterID)
public ChArUcoBoardDetectorProfileForHubSetting getChArUcoBoardDetectorProfileForHubSettingByClusterID(string clusterID)
public ChessboardDetectorProfile getChessboardDetectorProfileByClusterID(string clusterID)
public ChessboardDetectorProfileForHubSetting getChessboardDetectorProfileForHubSettingByClusterID(string clusterID)
```

### Cluster Profile Accessors

```csharp
public ClusterProfile GetClusterProfileByName(string profileName)
```

### Board Parameter Accessors

```csharp
public ChessboardParameters GetChessboardParametersByName(string name)
public ChArUcoBoardParameters GetChArUcoBoardParametersByName(string name)
public ChessboardParameters GetChessboardParametersForClusterProfile(ClusterProfile profile)
public ChArUcoBoardParameters GetChArUcoBoardParametersForClusterProfile(ClusterProfile profile)
```

### Node Index Helper

```csharp
public int GetNodeIndex(string name)
public int GetNodeIndex(Device device)
```

**Returns:** Index of node in the `Nodes` list, or -1 if not found

## Usage Example

```csharp
// Load settings
Settings.LoadSettings();

// Access cluster profile
ClusterProfile profile = Settings.All.GetClusterProfile();

// Get nodes for a cluster
List<Device> nodes = Settings.All.GetNodesByClusterID("CLUSTER001");

// Get intrinsics for a specific node
Intrinsics intrinsics = Settings.All.GetIntrinsicsForNode("Node1");

// Access detector profile thresholds
YoloPoseDetectorProfile poseProfile = Settings.All.getYoloPoseDetectorProfileByClusterID("CLUSTER001");
double detectThreshold = poseProfile.detectConfThreshold;
double kpThreshold = poseProfile.kpDetectThreshold;

// Get chessboard parameters
ChessboardParameters board = Settings.All.GetChessboardParametersByName("Standard3x3");

// Get node index in list
int nodeIndex = Settings.All.GetNodeIndex("Node2");
```

## Related Types

- `Device` - Hub and node device configuration
- `Intrinsics` - Camera calibration parameters
- `Extrinsics` - Node-to-node transformation
- `SourceFile`, `RuntimeFile`, `ModelFile` - File tracking structures
- `YoloPoseDetectorProfile`, `YoloObjectDetectorProfile`, `FaceIDDetectorProfile`
- `ChArUcoBoardDetectorProfile`, `ChessboardDetectorProfile`
- `ClusterProfile`, `OrthographicViewerSettings`
