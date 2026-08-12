# ClusterProfile

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Defines a complete configuration set for a Dplus cluster, including all detector settings, calibration capture requirements, and operational parameters. Each profile can be selected for a specific cluster via AppSettings.ClusterProfileToUse.

## Constructors

### `ClusterProfile()`

Default constructor initializes with empty values and default capture settings.

```csharp
public ClusterProfile()
{
    profileName = string.Empty;
    extrinsics = new List<Extrinsics>();
    intrinsicsCaptureCount = 0;
    extrinsicsCaptureCount = 0;
    YoloPoseProfileToUse = string.Empty;
    YoloObjectProfileToUse = string.Empty;
    FaceIDProfileToUse = string.Empty;
    ChArUcoDetProfileToUse = string.Empty;
    ChessboardDetProfileToUse = string.Empty;
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

### Profile Identification

| Property | Type | Description |
|----------|------|-------------|
| `profileName` | `string` | Unique profile name (used as identifier) |

### Calibration Capture Requirements

| Property | Type | Description |
|----------|------|-------------|
| `extrinsicsCaptureCount` | `int` | Number of capture sequences needed for extrinsics calibration |
| `intrinsicsCaptureCount` | `int` | Number of capture sequences needed for intrinsics calibration |

### Detector Profile References

Each property references a detector profile by name defined in AppSettings collections:

| Property | Type | Description |
|----------|------|-------------|
| `YoloPoseProfileToUse` | `string` | Name of YOLO pose detection profile to use |
| `YoloObjectProfileToUse` | `string` | Name of YOLO object detection profile to use |
| `FaceIDProfileToUse` | `string` | Name of face recognition profile to use |
| `ChArUcoDetProfileToUse` | `string` | Name of ChArUco board detection profile to use |
| `ChessboardDetProfileToUse` | `string` | Name of chessboard detection profile to use |

### Operational Parameters

| Property | Type | Description |
|----------|------|-------------|
| `captureOnStartup` | `bool` | Capture calibration images on cluster startup |
| `captureEachFrame` | `bool` | Capture every incoming frame |
| `captureEachDetection` | `bool` | Capture when new detection appears |
| `captureNewDetection` | `bool` | Capture each unique detection |
| `targetFrameRate` | `double` | Target frames per second for capture |
| `indicatorType` | `string` | Type of visual indicator to display |
| `introSequenceIterations` | `int` | Number of iterations in intro sequence |
| `introSequenceDelay` | `int` | Delay between iterations (ms) |

## Usage Example

```csharp
// Create a cluster profile
ClusterProfile profile = new ClusterProfile
{
    profileName = "Standard",
    extrinsicsCaptureCount = 3,
    intrinsicsCaptureCount = 1,
    YoloPoseProfileToUse = "Standard",
    YoloObjectProfileToUse = "Standard",
    FaceIDProfileToUse = "Standard",
    ChArUcoDetProfileToUse = "Standard5x6",
    ChessboardDetProfileToUse = "Standard3x3",
    captureOnStartup = true,
    captureEachFrame = false,
    targetFrameRate = 30.0
};

// Access from settings by cluster ID
ClusterProfile activeProfile = Settings.All.GetClusterProfileByName("CLUSTER001");

// Get detector profile references
YoloPoseDetectorProfile poseProfile = Settings.All.getYoloPoseDetectorProfileByClusterID("CLUSTER001");
ChessboardParameters board = Settings.All.GetChessboardParametersByName(activeProfile.ChessboardDetProfileToUse);

// Check capture requirements
Console.WriteLine($"Need {activeProfile.extrinsicsCaptureCount} extrinsics captures");
Console.WriteLine($"Need {activeProfile.intrinsicsCaptureCount} intrinsics captures");

// Modify operational parameters
activeProfile.captureOnStartup = false;
```

## Related Types

- `AppSettings` - Contains ClusterProfiles list and profile accessor
- `YoloPoseDetectorProfile`, `YoloObjectDetectorProfile`, `FaceIDDetectorProfile`
- `ChArUcoBoardDetectorProfile`, `ChessboardDetectorProfile`
- `Extrinsics` - Calibration data referenced by capture count
- `ClusterProfileForHubSetting`, `NodeSettings` - Profile data in device settings
