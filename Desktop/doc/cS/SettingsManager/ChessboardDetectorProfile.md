# ChessboardDetectorProfile

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Defines chessboard detection configuration. Controls whether to use chessboard markers for reconstruction, saving behavior, and the specific board parameters to apply.

## Constructors

### `ChessboardDetectorProfile()`

Default constructor initializes with chessboard detection disabled.

```csharp
public ChessboardDetectorProfile()
{
    name = string.Empty;
    useChessboardDetection = false;
    saveChessboardDetections = false;
    chessboardToUse = string.Empty;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string` | Profile name (used to select profile by cluster) |
| `useChessboardDetection` | `bool` | Enable chessboard detection |
| `saveChessboardDetections` | `bool` | Save images with detected chessboards |
| `chessboardToUse` | `string` | Name of chessboard parameters to apply |

## Usage Example

```csharp
// Create a chessboard detection profile
ChessboardDetectorProfile profile = new ChessboardDetectorProfile
{
    name = "Standard",
    useChessboardDetection = true,
    saveChessboardDetections = true,
    chessboardToUse = "Standard3x3"
};

// Access from settings by cluster ID
ChessboardDetectorProfile activeProfile = Settings.All.getChessboardDetectorProfileByClusterID("CLUSTER001");

// Check detection settings
if (activeProfile.useChessboardDetection)
{
    Console.WriteLine($"Using board: {activeProfile.chessboardToUse}");
}

// Modify settings
activeProfile.useChessboardDetection = false;
```

## Related Types

- `AppSettings` - Contains ChessboardDetProfiles list and profile accessor
- `ChessboardParameters` - Board geometry defined by name in chessboardToUse
- `ChArUcoBoardDetectorProfile` - Alternative detection method
- `ClusterProfile` - References active chessboard profile name
