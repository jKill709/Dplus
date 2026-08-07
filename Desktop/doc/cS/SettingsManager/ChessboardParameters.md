# ChessboardParameters

**Namespace:** `Dplus_Desktop.SettingsManager`

## Purpose

Represents chessboard parameters as applied to a hub. Contains the profile name, enabled flags, and associated chessboard geometry.

## Constructors

### `ChessboardDetectorProfileForHubSetting()`

Default constructor initializes with detection disabled, empty board, and default flags.

```csharp
public ChessboardDetectorProfileForHubSetting()
{
    name = string.Empty;
    useChessboardDetection = false;
    saveChessboardDetections = false;
    chessboard = new ChessboardParameters();
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string` | Profile name (matches a profile in ChessboardDetProfiles) |
| `useChessboardDetection` | `bool` | Enable chessboard detection |
| `saveChessboardDetections` | `bool` | Save images with detected chessboards |
| `chessboard` | `ChessboardParameters` | Board geometry parameters |

## Usage Example

```csharp
// Create chessboard detection settings
ChessboardDetectorProfileForHubSetting settings = new ChessboardDetectorProfileForHubSetting
{
    name = "Standard3x3",
    useChessboardDetection = true,
    saveChessboardDetections = true
};

// Access from node settings
var nodeSettings = Settings.All.Nodes.FirstOrDefault(n => n.name == "Node1");
if (nodeSettings != null)
{
    var chessboardSettings = nodeSettings.chessboardDetSettings;
    Console.WriteLine($"Chessboard detection: {(chessboardSettings.useChessboardDetection ? "Enabled" : "Disabled")}");
    Console.WriteLine($"Using board: {chessboardSettings.chessboard.name}");
}

// Modify settings
chessboardSettings.useChessboardDetection = false;
```

## Related Types

- `AppSettings` - ChessboardDetProfiles collection and accessor methods
- `NodeSettings` - chessboardDetSettings property
- `ChessboardDetectorProfileForHubSetting` - Same structure for hub
- `ChArUcoBoardDetectorProfileForHubSetting` - Similar structure for ChArUco boards
