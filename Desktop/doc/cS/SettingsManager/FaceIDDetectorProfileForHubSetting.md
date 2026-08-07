# FaceIDDetectorProfileForHubSetting

**Namespace:** `Dplus_Desktop.SettingsManager`

## Purpose

Represents face detection and recognition settings as applied to a hub. Contains the profile name, Haar detection flags, LBPH recognition flags, model paths, and face class names.

## Constructors

### `FaceIDDetectorProfileForHubSetting()`

Default constructor initializes with Haar detection disabled and LBPH enabled.

```csharp
public FaceIDDetectorProfileForHubSetting()
{
    name = string.Empty;
    useHaarFaceDetection = false;
    saveHaarDetections = false;
    haarFaceModel = string.Empty;
    useLBPHFaceRecognition = true;
    saveLBPHRecognitions = true;
    lbphFaceRecognizeModel = string.Empty;
    FaceClassNames = string.Empty;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string` | Profile name (matches a profile in FaceIDDetProfiles) |
| `useHaarFaceDetection` | `bool` | Enable Haar cascade face detection |
| `saveHaarDetections` | `bool` | Save images with Haar-detected faces |
| `haarFaceModel` | `string` | Path to Haar cascade model (e.g., haarcascade_frontalface_default.xml) |
| `useLBPHFaceRecognition` | `bool` | Enable LBPH face recognition |
| `saveLBPHRecognitions` | `bool` | Save images with recognized faces |
| `lbphFaceRecognizeModel` | `string` | Path to LBPH model (e.g., lbph_frontalface_recognition_model.dat) |
| `FaceClassNames` | `string` | Comma-separated face class names |

## Usage Example

```csharp
// Create face detection settings
FaceIDDetectorProfileForHubSetting settings = new FaceIDDetectorProfileForHubSetting
{
    name = "Standard",
    useHaarFaceDetection = true,
    haarFaceModel = "/models/haarcascade_frontalface_default.xml",
    saveHaarDetections = true,
    useLBPHFaceRecognition = true,
    lbphFaceRecognizeModel = "/models/lbph_frontalface_recognition_model.dat",
    FaceClassNames = "Person1,Person2,Unknown"
};

// Access from hub settings
var hubSettings = Settings.All.Hubs.FirstOrDefault(h => h.name == "Hub1");
if (hubSettings != null)
{
    var faceSettings = hubSettings.faceIDDetSettings;
    Console.WriteLine($"Haar detection: {(faceSettings.useHaarFaceDetection ? "Enabled" : "Disabled")}");
    Console.WriteLine($"LBPH recognition: {(faceSettings.useLBPHFaceRecognition ? "Enabled" : "Disabled")}");
    
    // Parse face class names
    string[] faceClasses = faceSettings.FaceClassNames.Split(',');
    foreach (var cls in faceClasses)
    {
        Console.WriteLine($"Face class: {cls.Trim()}");
    }
}

// Modify settings
faceSettings.useLBPHFaceRecognition = false;
```

## Related Types

- `AppSettings` - FaceIDDetProfiles collection and accessor methods
- `HubSettings` - faceIDDetSettings property
- `FaceIDDetectorProfile` - Source profile definition
