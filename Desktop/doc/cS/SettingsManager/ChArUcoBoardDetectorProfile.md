# ChArUcoBoardDetectorProfile

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Defines ChArUco board detection configuration. Controls whether to use ChArUco markers for reconstruction, saving behavior, and the specific board parameters to apply.

## Constructors

### `ChArUcoBoardDetectorProfile()`

Default constructor initializes with ChArUco detection disabled.

```csharp
public ChArUcoBoardDetectorProfile()
{
    name = string.Empty;
    useChArUcoBoardDetection = false;
    saveChArUcoBoardDetections = false;
    chArUcoBoardToUse = string.Empty;
    RepErrThreshAtReconstruction = 0;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string` | Profile name (used to select profile by cluster) |
| `useChArUcoBoardDetection` | `bool` | Enable ChArUco board detection |
| `saveChArUcoBoardDetections` | `bool` | Save images with detected ChArUco markers |
| `chArUcoBoardToUse` | `string` | Name of ChArUco board parameters to apply |
| `RepErrThreshAtReconstruction` | `int` | Reproduction error threshold in pixels (0 = disabled) |

## Usage Example

```csharp
// Create a ChArUco detection profile
ChArUcoBoardDetectorProfile profile = new ChArUcoBoardDetectorProfile
{
    name = "Standard",
    useChArUcoBoardDetection = true,
    saveChArUcoBoardDetections = true,
    chArUcoBoardToUse = "Chessboard3x3",
    RepErrThreshAtReconstruction = 10  // Only reconstruct if error < 10px
};

// Access from settings by cluster ID
ChArUcoBoardDetectorProfile activeProfile = Settings.All.getChArUcoBoardDetectorProfileByClusterID("CLUSTER001");

// Check detection settings
if (activeProfile.useChArUcoBoardDetection)
{
    Console.WriteLine($"Using board: {activeProfile.chArUcoBoardToUse}");
    Console.WriteLine($"Reproduction threshold: {activeProfile.RepErrThreshAtReconstruction}px");
}

// Modify settings
activeProfile.RepErrThreshAtReconstruction = 5;
```

## Related Types

- `AppSettings` - Contains ChArUcoBoardDetProfiles list and profile accessor
- `ChArUcoBoardParameters` - Board geometry defined by name in chArUcoBoardToUse
- `ChessboardDetectorProfile` - Alternative detection method
- `ClusterProfile` - References active ChArUco profile name
