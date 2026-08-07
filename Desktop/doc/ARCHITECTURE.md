# Architecture Documentation

## Repository Structure and Purpose

The Dplus Desktop application is a C# WinForms desktop client that manages distributed camera clusters via SSH connections to hub and node devices. It provides real-time monitoring, visualization, and control capabilities for embedded firmware running on the cluster nodes. The application communicates with the embedded systems primarily through MQTT for telemetry data and file transfer operations via SSH.NET.

```
Desktop/
├── src/cS/                          # Core Source (C# WinForms)
│   ├── Main.cs                      # Main form - orchestrator for other forms
│   ├── Main.Designer.cs             # Main form designer code
│   ├── Program.cs                   # Application entry point
│   ├── Uploader.cs                  # File upload UI and logic
│   ├── Uploader.Designer.cs         # Uploader form designer
│   ├── Viewer.cs                    # Real-time viewer with MQTT subscription
│   ├── Viewer.Designer.cs          # Viewer form designer
│   ├── SettingsManager.cs          # Global settings, cluster configuration
│   └── Network/
│       ├── ClusterManager.cs       # SSH-based cluster management
│       ├── ClusterStatus.cs        # Runtime status data structures
│       └── ViewerReconstructionClient.cs
├── src/cS/WinForms/Controls/        # Reusable WinForms controls
│   └── ClusterStatusDisplay/       # Device status UI components
├── Properties/                      # Resources, settings
├── obj/                             # Build artifacts
└── bin/                             # Compiled outputs
```

## Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| Runtime | .NET 8 (net8.0-windows) | Primary runtime platform |
| UI Framework | WinForms | Desktop user interface |
| SSH Client | SSH.NET v2025.0.0 | Secure shell connections to devices |
| MQTT Client | MQTTnet v4.3.6 | Real-time telemetry communication |
| Computer Vision | OpenCvSharp4 v4.11.0 | Image processing, detection overlays |
| 3D Rendering | HelixToolkit.Wpf v3.1.2 | Perspective views (used via WinForms interop) |
| Logging | mLogger library | Centralized logging infrastructure |
| Communication Abstraction | jCommunicator library | SSH tunneling and file I/O primitives |

## Architectural Overview

The application follows a layered architecture pattern with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────────┐
│                         UI Layer                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │   Main Form  │  │    Viewer    │  │   Uploader   │           │
│  │              │  │              │  │              │           │
│  │  - Cluster   │  │  - MQTT      │  │  - File      │           │
│  │    manager   │  │    client    │  │    upload    │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
├─────────────────────────────────────────────────────────────────┤
│                         Business Layer                           │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              ClusterManager (Network Layer)              │   │
│  │  - SSH connectivity management                           │   │
│  │  - File upload/download operations                       │   │
│  │  - Service status checking                               │   │
│  │  - Cluster lifecycle control (start/stop/reboot)         │   │
│  └─────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────┤
│                         Data Layer                               │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              SettingsManager (Persistence)               │   │
│  │  - JSON configuration management                         │   │
│  │  - Device registry                                       │   │
│  │  - Calibration data                                      │   │
│  │  - Detector profiles                                     │   │
│  └─────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────┤
│                      External Systems                            │
│  ┌──────────────┐      ┌──────────────┐                         │
│  │    SSH       │      │     MQTT     │                         │
│  │   (SSH.NET)  │      │   (MQTTnet)  │                         │
│  └──────────────┘      └──────────────┘                         │
└─────────────────────────────────────────────────────────────────┘
```

### Key Architectural Patterns

1. **Form-based UI with lazy initialization**: Main form acts as orchestrator, creating Viewer and Uploader forms on-demand
2. **Background workers for async operations**: SSH connections, MQTT subscriptions, and file transfers run on background threads
3. **Data transfer objects (DTOs)**: SettingsManager provides strongly-typed data structures for cluster configuration
4. **Event-driven updates**: MQTT messages trigger UI updates via thread-safe invocation patterns

## Major Components

### Main Form (`Main.cs`)

The entry point form that manages the application lifecycle and coordinates between other forms.

**Responsibilities:**
- Initialize logging sinks (RichTextBox and file-based)
- Provide buttons to launch Uploader and Viewer forms
- Clean up references when child forms close

**Key Methods:**
```csharp
Uploader_Button_Click()  // Creates/brings forward Uploader form
Viewer_Button_Click()    // Creates/brings forward Viewer form
AddLogSource()           // Registers logging sources with color coding
```

### ClusterManager (`Network/ClusterManager.cs`)

Central component for all SSH-based operations on the camera cluster.

**Core Functionality:**
- Establishes SSH tunnel to hub device
- Creates SSH tunnels to node devices through hub
- Manages file upload/download (source files, binaries, models)
- Monitors service status (hub.service, device.service)
- Handles cluster lifecycle (start, stop, reboot, shutdown)

**Key Methods:**
```csharp
ClusterManager(Device hub, List<Device> nodes)  // Constructor initializes SSH
CheckSystem()                                   // Returns ClusterStatus
UploadFiles()                                   // Source + model files
DownloadFiles()                                 // Logs, reconstructions
DistributeRuntimeFiles()                        // Push binaries/settings to nodes
startMain() / stopMain()                        // Service daemon control
RebootCluster() / ShutdownCluster()             // Power management
```

### SettingsManager (`SettingsManager.cs`)

Singleton class providing global application state and cluster configuration.

**Data Structures:**
- `AppSettings`: Remote/local paths, MQTT topics, viewer settings
- `Device`: Name, role (Hub/Node), credentials, cluster ID
- `ClusterProfile`: Detector profiles, extrinsics, calibration settings
- `Intrinsics` / `Extrinsics`: Camera calibration data with comparison operators
- `SourceFile` / `RuntimeFile` / `ModelFile`: File tracking metadata

**Key Methods:**
```csharp
LoadSettings()  // JSON deserialization with flexible DateTime conversion
SaveSettings()  // JSON serialization
GetClusterProfile() / GetDeviceByName() / GetIntrinsicsForNode()
MergeNewCalibrationData()  // Incremental calibration data integration
```

### Viewer (`Viewer.cs`)

Real-time visualization component subscribing to cluster telemetry via MQTT.

**Features:**
- MQTT subscription to hub telemetry topic
- Live and saved frame playback with configurable buffer limits
- Multi-viewer display (Image1, Image2) with overlay support
- Orthographic views (XY, YZ, XZ planes) with auto-fitting grid
- Perspective view with camera frustums and 3D points
- Detection overlays (YoloPose, YoloObject, FaceRec, ChArUco, Chessboard)
- TreeView data inspector for detailed frame analysis

**Key Methods:**
```csharp
HandleIncomingFrame(string json)  // MQTT message processing
UpdateViewers(RigFrame frame)     // Sync all viewers to new frame
ShowPoseDetections()              // Render YoloPose with skeleton
ShowCharucoDetection()            // Render ChArUco corners and IDs
DisplayRigFrameData(TreeView, frame)  // Populate data inspector
```

**RigFrame Data Model:**
```csharp
public class RigFrame {
    int commandID;
    DateTime Timestamp;
    List<CameraFrame> camFrames;      // Per-camera detections
    List<YoloPoseReconstruction> poseRecs;
    List<YoloObjectReconstruction> objectRecs;
    List<FaceReconstruction> faceRecs;
    ChArUcoReconstruction? charucoRec;
    ChessboardReconstruction? chessboardRec;
}
```

### ImageControls (`ImageControls.cs`)

Custom WinForms controls for displaying images with overlay primitives.

**Components:**
- `ImageControls`: Base control with pan/zoom, layer system
- `OrthographicViewer`: 2D plane viewer with auto-fitting grid
- `PerspectiveViewer`: 3D camera frustum visualization
- `PrimitiveOverlayLayer`: Layered overlay system (points, lines, polygons, text)

**Overlay Primitives:**
```csharp
PointOverlay      // Colored circles with optional text labels
LineOverlay       // Lines between points
PolygonOverlay    // Polygons with fill and outline
TextOverlay       // Backgrounded text labels
```

## Data Flow

### Image Flow (Embedded → Desktop)

```
┌─────────────────────────────────────────────────────────────┐
│                    Embedded Firmware                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                   │
│  │   Node1  │  │   Node2   │  │   Hub    │                   │
│  │Camera    │  │Camera     │  │ Capture  │                   │
│  │Capture   │  │Capture    │  │ Service  │                   │
│  └────┬─────┘  └────┬──────┘  └────┬─────┘                   │
│      │              │              │                           │
│      └──────────────┼──────────────┘                           │
│                     │                                          │
│            ┌────────▼────────┐                                 │
│            │   MQTT Broker   │                                 │
│            │ /hubTelemetry   │                                 │
│            └────────┬────────┘                                 │
│                     │                                          │
└─────────────────────┼──────────────────────────────────────────┘
                      │
                      ▼
          ┌─────────────────────┐
          │    Dplus Desktop    │
          │   MQTT Worker       │
          │  (Viewer.cs)        │
          └──────────┬──────────┘
                     │
          ┌──────────▼──────────┐
          │ HandleIncomingFrame │
          │ Deserialize RigFrame│
          │ UpdateViewers       │
          └──────────┬──────────┘
                     │
          ┌──────────▼──────────┐
          │  ImageControls      │
          │  Overlay Primitives │
          │  3D Viewers         │
          └─────────────────────┘
```

**Sequence:**
1. Embedded firmware captures images (via camera or detection pipeline)
2. Frame metadata serialized to JSON
3. MQTT publish to hub telemetry topic
4. Desktop MQTT worker subscribes, receives message
5. `HandleIncomingFrame` deserializes to `RigFrame` DTO
6. Frame added to live buffer (configurable limit)
7. `UpdateViewers` called, rendering overlays on ImageControls
8. Orthographic/Perspective viewers update with 3D reconstructions

### Command Flow (Desktop → Embedded)

```
┌─────────────────────────────────────────────────────────────┐
│                    Dplus Desktop                             │
│  ┌──────────────┐  ┌──────────────┐                         │
│  │   Cluster    │  │ MQTT Worker  │                         │
│  │  Manager     │  │              │                         │
│  └──────┬───────┘  └──────┬───────┘                         │
│        │                  │                                  │
│        ▼                  ▼                                  │
│  ┌──────────────────────────────────────┐                   │
│  │         SSH.NET Connections           │                   │
│  │   Hub: SSH to hub device              │                   │
│  │   Nodes: SSH tunnel via hub           │                   │
│  └─────────────────┬─────────────────────┘                   │
│                    │                                          │
│        ┌───────────▼───────────┐                             │
│        │ ExecuteHubCommand     │                             │
│        │ ExecuteNodeCommand    │                             │
│        │ PCtoHubAsync          │                             │
│        │ PCtoNodeAsync         │                             │
│        └───────────┬───────────┘                             │
│                    │                                          │
│        ┌───────────▼───────────┐                             │
│        │   Hub Device          │                             │
│        │   SSH Gateway         │                             │
│        └───────────┬───────────┘                             │
│                    │                                          │
│        ┌───────────▼───────────┐                             │
│        │    Node Devices       │                             │
│        │  (via SSH tunnel)     │                             │
│        └───────────────────────┘                             │
└─────────────────────────────────────────────────────────────┘
```

**Key Operations:**
- **Commands**: SSH.NET executes shell commands (`systemctl`, `make`, `shutdown`)
- **File Transfer**: Custom `ClusterFileIOCommand` with progress callbacks
- **SSH Tunneling**: Hub acts as gateway for node access

## Execution Flow

### Startup Sequence

```
┌─────────────────────────────────────────────────────────────────┐
│                        Program.cs                                 │
│                                                                  │
│  [1] ApplicationConfiguration.Initialize()                       │
│      └── Set High DPI, default font                              │
│                                                                  │
│  [2] Settings.LoadSettings()                                     │
│      └── Read managerSettings.json                               │
│          └── Deserialize AppSettings                             │
│              └── FlexibleDateTimeConverter for timestamps        │
│          └── isLoaded = true/false                               │
│                                                                  │
│  [3] if (!isLoaded) { MessageBox; return }                       │
│                                                                  │
│  [4] Logger.Instance.Initialize("CamManager")                    │
│      └── Setup logging infrastructure                            │
│                                                                  │
│  [5] Application.Run(new Main())                                 │
│      └── Show Main form                                          │
└─────────────────────────────────────────────────────────────────┘
```

**Main Form Initialization:**
1. `ComponentInitialize()` loads designer-generated controls
2. Create RichTextBoxSink → add to logger
3. Create TextFileSink (async file logging) → add to logger
4. Log heading "CamManager Initialized"

### Main Loop

The application is event-driven with no explicit main loop beyond the WinForms message pump:
- Forms handle their own timer events, button clicks, and background tasks
- MQTT worker runs on separate thread for non-blocking telemetry
- SSH operations use async/await to avoid UI blocking

### Shutdown Sequence

1. User closes Main form (Application.Exit invoked)
2. WinForms disposes child forms automatically
3. MQTT worker task completes, disconnects
4. SSH connections close
5. Logger finalizes file sink

## Extension Points

### Custom Display Layers

The `ILayer` interface enables adding custom overlays:

```csharp
public interface ILayer {
    bool Visible { get; set; }
    void Render(Graphics g, Matrix imageTransform);
}

// Usage in ImageControls.Layers (List<ILayer>)
```

**Implementation Steps:**
1. Create class implementing `ILayer`
2. Add to viewer via `AddLayer()`
3. Implement `Render` using provided `Graphics` context
4. Apply image transform for proper coordinate mapping

### Plugin Architecture

Currently no formal plugin system exists, but the architecture supports extension through:
- Adding new forms (follow Main pattern)
- Extending SettingsManager with new DTOs
- Registering new MQTT topics in AppSettings
- Creating new overlay types in ImageControls

**Future Considerations:**
- Dependency injection for service registration
- Event aggregation for decoupled component communication
- Plugin manifest format for discoverable extensions

### Custom SSH Operations

Extend `ClusterManager` with new methods following existing patterns:
```csharp
// Example: custom command execution
public async Task<CommandResult> ExecuteCustomAsync(string command) {
    return await _hubCom.ExecuteHubCommandAsync(command);
}
```

### MQTT Message Handlers

Viewer's MQTT handler pattern can be reused:
```csharp
var worker = new MqttWorker(hubIP, port, topic);
worker.OnMessage += async e => {
    var data = ProcessMessage(e.ApplicationMessage.PayloadSegment);
    await HandleDataAsync(data);
};
await worker.StartAsync();
```

### File I/O Operations

Use `jCommunicator.Communicator` methods:
- `_hubCom.PCtoHubAsync()` for upload
- `_hubCom.ExecuteHubCommandAsync()` for remote execution
- Custom command batching via `List<ClusterFileIOCommand>`

## Security Considerations

- SSH credentials stored in plaintext in settings (consider encryption)
- MQTT topics hardcoded (support for topic wildcards?)
- No certificate-based authentication currently
- File transfer lacks integrity verification

## Performance Notes

- Live frame buffer defaults to 1 frame (configurable via `LiveFrameBufferLimit`)
- Orthographic viewer auto-fitting with adaptive grid spacing
- Perspective viewer uses hardware-accelerated 3D rendering
- Double-buffered controls prevent flicker during updates
