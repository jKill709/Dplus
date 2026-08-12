# RuntimeFile

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Tracks a runtime file (compiled binary or settings JSON) for distribution to devices. Records timestamps for source changes, compilation, and pushing to determine deployment status.

## Constructors

### `RuntimeFile()`

Default constructor initializes with empty values and default flag (not for node).

```csharp
public RuntimeFile()
{
    FileName = string.Empty;
    IsForNode = false;
    Path = string.Empty;
    LastSourceChangeTime = null;
    LastCompliedTime = null;
    LastPushedTime = null;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `FileName` | `string` | Name of the runtime file (e.g., "hubSettings.json", "node") |
| `IsForNode` | `bool` | Whether this file should be pushed to nodes (vs. hub only) |
| `Path` | `string` | Relative path within hub/node directories |
| `LastSourceChangeTime` | `DateTime?` | When source was last modified |
| `LastCompliedTime` | `DateTime?` | When file was last compiled |
| `LastPushedTime` | `DateTime?` | When file was last pushed to device(s) |

## Usage Example

```csharp
// Create a runtime file entry
RuntimeFile runtimeFile = new RuntimeFile
{
    FileName = "hubSettings.json",
    IsForNode = false,  // Hub only
    Path = "/home/camcpp/src/"
};

// Access from settings
var runtimeFiles = Settings.All.RuntimeFiles;
foreach (RuntimeFile rf in runtimeFiles)
{
    Console.WriteLine($"File: {rf.FileName}");
    Console.WriteLine($"  Deploy to: {(rf.IsForNode ? "Nodes" : "Hub")}");
    Console.WriteLine($"  Source changed: {rf.LastSourceChangeTime?.ToString("yyyy-MM-dd HH:mm:ss")}");
    Console.WriteLine($"  Compiled: {rf.LastCompliedTime?.ToString("yyyy-MM-dd HH:mm:ss")}");
    Console.WriteLine($"  Pushed: {rf.LastPushedTime?.ToString("yyyy-MM-dd HH:mm:ss")}");
}

// Check deployment status
if (runtimeFile.LastSourceChangeTime > runtimeFile.LastCompliedTime)
{
    Console.WriteLine("Needs compilation");
}
else if (runtimeFile.LastCompliedTime > runtimeFile.LastPushedTime)
{
    Console.WriteLine("Needs distribution");
}
```

## Related Types

- `AppSettings` - Contains RuntimeFiles collection
- `SourceFile`, `ModelFile` - Similar file tracking structures
- `ClusterManager` - Distributes runtime files to devices
