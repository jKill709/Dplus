# ModelFile

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Tracks a model file (typically ONNX or PT format) for distribution to devices. Records local modification time and push time to determine when models need to be uploaded.

## Constructors

### `ModelFile()`

Default constructor initializes with empty values, null push time, and default type/name.

```csharp
public ModelFile()
{
    ModelName = string.Empty;
    ModelType = string.Empty;
    LastModifiedTime = null;
    LastPushTime = null;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `ModelName` | `string` | Name of the model file (e.g., "yolov8x-pose.onnx") |
| `ModelType` | `string` | Model type/variant (e.g., "Pose", "Object") |
| `LastModifiedTime` | `DateTime?` | Local file's last modification time |
| `LastPushTime` | `DateTime?` | Time when last pushed to devices |

## Usage Example

```csharp
// Create a model file entry
ModelFile modelFile = new ModelFile
{
    ModelName = "yolov8x-pose.onnx",
    ModelType = "Pose"
};

// Access from settings
var models = Settings.All.Models;
foreach (ModelFile mf in models)
{
    Console.WriteLine($"Model: {mf.ModelName}");
    Console.WriteLine($"  Type: {mf.ModelType}");
    Console.WriteLine($"  Modified: {mf.LastModifiedTime?.ToString("yyyy-MM-dd HH:mm:ss")}");
    Console.WriteLine($"  Pushed: {mf.LastPushTime?.ToString("yyyy-MM-dd HH:mm:ss")}");
}

// Check push status
if (modelFile.LastModifiedTime > modelFile.LastPushTime)
{
    Console.WriteLine("Model needs to be pushed");
}
```

## Related Types

- `AppSettings` - Contains Models collection
- `SourceFile`, `RuntimeFile` - Similar file tracking structures
- `ClusterManager` - Pushes models to devices
