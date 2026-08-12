# ChArUcoBoardDetectorProfileForHubSetting

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Represents ChArUco board detection settings as applied to a node. Contains the profile name, enabled flags, associated board parameters, and reproduction error threshold for reconstruction.

## Constructors

### `ChArUcoBoardDetectorProfileForHubSetting()`

Default constructor initializes with detection disabled, empty board, and default threshold.

```csharp
public ChArUcoBoardDetectorProfileForHubSetting()
{
    name = string.Empty;
    useChArUcoBoardDetection = false;
    saveChArUcoBoardDetections = false;
    chArUcoBoard = new ChArUcoBoardParameters();
    RepErrThreshAtReconstruction = 10;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string` | Profile name (matches a profile in ChArUcoBoardDetProfiles) |
| `useChArUcoBoardDetection` | `bool` | Enable ChArUco board detection |
| `saveChArUcoBoardDetections` | `bool` | Save images with detected ChArUco markers |
| `chArUcoBoard` | `ChArUcoBoardParameters` | Board geometry parameters |
| `RepErrThreshAtReconstruction` | `int` | Reproduction error threshold in pixels (default: 10) |

## Usage Example

```csharp
// Create ChArUco board detection settings
ChArUcoBoardDetectorProfileForHubSetting settings = new ChArUcoBoardDetectorProfileForHubSetting
{
    name = "Standard5x6",
    useChArUcoBoardDetection = true,
    saveChArUcoBoardDetections = true,
    RepErrThreshAtReconstruction = 5
};

// Access from node settings
var nodeSettings = Settings.All.Nodes.FirstOrDefault(n => n.name == "Node1");
if (nodeSettings != null)
{
    var chArUcoSettings = nodeSettings.chArUcoBoardDetSettings;
    Console.WriteLine($"ChArUco detection: {(chArUcoSettings.useChArUcoBoardDetection ? "Enabled" : "Disabled")}");
    Console.WriteLine($"Using board: {chArUcoSettings.chArUcoBoard.name}");
    Console.WriteLine($"Reproduction threshold: {chArUcoSettings.RepErrThreshAtReconstruction}px");
}

// Modify settings
chArUcoSettings.useChArUcoBoardDetection = false;
```

## Related Types

- `AppSettings` - ChArUcoBoardDetProfiles collection and accessor methods
- `NodeSettings` - chArUcoBoardDetSettings property
- `ChArUcoBoardDetectorProfileForHubSetting` - Same structure for hub
- `ChessboardDetectorProfileForHubSetting` - Similar structure for chessboards
