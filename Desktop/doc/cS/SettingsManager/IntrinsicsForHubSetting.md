# IntrinsicsForHubSetting

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Represents camera intrinsics calibration data as applied to a hub. Contains the profile name, enabled flags, and associated intrinsics parameters.

## Constructors

### `IntrinsicsDetectorProfileForHubSetting()`

Default constructor initializes with empty name, disabled detection, and default intrinsics.

```csharp
public IntrinsicsDetectorProfileForHubSetting()
{
    name = string.Empty;
    useIntrinsicsDetection = false;
    saveIntrinsicsDetections = false;
    intrinsics = new Intrinsics();
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string` | Profile name (matches a profile in IntrinsicsDetProfiles) |
| `useIntrinsicsDetection` | `bool` | Enable intrinsics detection |
| `saveIntrinsicsDetections` | `bool` | Save images with detected intrinsics |
| `intrinsics` | `Intrinsics` | Camera calibration parameters |

## Usage Example

```csharp
// Create intrinsics detection settings
IntrinsicsDetectorProfileForHubSetting settings = new IntrinsicsDetectorProfileForHubSetting
{
    name = "Standard",
    useIntrinsicsDetection = true,
    saveIntrinsicsDetections = true
};

// Access from node settings
var nodeSettings = Settings.All.Nodes.FirstOrDefault(n => n.name == "Node1");
if (nodeSettings != null)
{
    var intrinsicsSettings = nodeSettings.intrinsicsDetSettings;
    Console.WriteLine($"Intrinsics detection: {(intrinsicsSettings.useIntrinsicsDetection ? "Enabled" : "Disabled")}");
    Console.WriteLine($"Using intrinsics: {intrinsicsSettings.intrinsics.CameraIDnumber}");
}

// Modify settings
intrinsicsSettings.useIntrinsicsDetection = false;
```

## Related Types

- `AppSettings` - IntrinsicsDetProfiles collection and accessor methods
- `NodeSettings` - intrinsicsDetSettings property
- `IntrinsicsDetectorProfileForHubSetting` - Same structure for hub
- `ChessboardDetectorProfileForHubSetting` - Similar structure for chessboards
