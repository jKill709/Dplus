# ChArUcoBoardParameters

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Represents ChArUco board parameters as applied to a hub. Contains the profile name, enabled flags, and associated ChArUco board geometry.

## Constructors

### `ChArUcoBoardDetectorProfileForHubSetting()`

Default constructor initializes with detection disabled, empty board, and default flags.

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

## Usage Example

```csharp
// Create ChArUco board detection settings
ChArUcoBoardDetectorProfileForHubSetting settings = new ChArUcoBoardDetectorProfileForHubSetting
{
    name = "Standard5x6",
    useChArUcoBoardDetection = true,
    saveChArUcoBoardDetections = true
};

// Access from node settings
var nodeSettings = Settings.All.Nodes.FirstOrDefault(n => n.name == "Node1");
if (nodeSettings != null)
{
    var chArUcoSettings = nodeSettings.chArUcoBoardDetSettings;
    Console.WriteLine($"ChArUco detection: {(chArUcoSettings.useChArUcoBoardDetection ? "Enabled" : "Disabled")}");
    Console.WriteLine($"Using board: {chArUcoSettings.chArUcoBoard.name}");
}

// Modify settings
chArUcoSettings.useChArUcoBoardDetection = false;
```

## Related Types

- `AppSettings` - ChArUcoBoardDetProfiles collection and accessor methods
- `NodeSettings` - chArUcoBoardDetSettings property
- `ChArUcoBoardDetectorProfileForHubSetting` - Same structure for hub
- `ChessboardDetectorProfileForHubSetting` - Similar structure for chessboards
