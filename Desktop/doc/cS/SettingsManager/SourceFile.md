# SourceFile

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Tracks a source file to be uploaded to Dplus devices. Records local and remote timestamps to determine upload status. Supports hub-only, node-only, or both deployment targets.

## Constructors

### `SourceFile()`

Default constructor initializes with empty values and default flags (not for hub, not for node).

```csharp
public SourceFile()
{
    FileName = string.Empty;
    LastUploadTime = null;
    LastModifiedTime = null;
    IsForHub = false;
    IsForNode = false;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `FileName` | `string` | Name of the source file (relative to SourceFilesDirectory) |
| `LastUploadTime` | `DateTime?` | Time when last uploaded to remote device(s) |
| `LastModifiedTime` | `DateTime?` | Local file's last modification time |
| `IsForHub` | `bool` | Whether this file should be uploaded to the hub |
| `IsForNode` | `bool` | Whether this file should be uploaded to nodes |

## Usage Example

```csharp
// Create a source file entry
SourceFile sourceFile = new SourceFile
{
    FileName = "hubSettings.json",
    IsForHub = true,
    IsForNode = false
};

// Access from settings
var sourceFiles = Settings.All.SourceFiles;
foreach (SourceFile sf in sourceFiles)
{
    Console.WriteLine($"File: {sf.FileName}");
    Console.WriteLine($"  Local last modified: {sf.LastModifiedTime?.ToString("yyyy-MM-dd HH:mm:ss")}");
    Console.WriteLine($"  Last uploaded: {sf.LastUploadTime?.ToString("yyyy-MM-dd HH:mm:ss")}");
    Console.WriteLine($"  Deploy to: {(sf.IsForHub ? "Hub" : "") + (sf.IsForNode ? " Nodes" : "")}");
}

// Check upload status
if (sourceFile.LastModifiedTime > sourceFile.LastUploadTime)
{
    Console.WriteLine("File needs to be uploaded");
}
```

## Related Types

- `AppSettings` - Contains SourceFiles collection
- `RuntimeFile`, `ModelFile` - Similar file tracking for runtime and model files
- `ClusterManager` - Uploads source files to devices
