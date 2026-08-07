# NodeInfoForHubSettings

**Namespace:** `Dplus_Desktop.SettingsManager`

## Purpose

Represents summary information for a node device as seen from the hub perspective. Used in HubSettings.nodes list to provide quick node status without full settings.

## Constructors

### `NodeInfoForHubSettings()`

Default constructor initializes with empty values, inactive state, and null intrinsics.

```csharp
public NodeInfoForHubSettings()
{
    name = string.Empty;
    role = string.Empty;
    isActive = false;
    IPAddress = string.Empty;
    intrinsics = new Intrinsics();
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string` | Node device name (e.g., "Node1") |
| `role` | `string` | Always "Node" for this type |
| `isActive` | `bool` | Whether node is currently active |
| `IPAddress` | `string` | IP address for hub-to-node communication |
| `intrinsics` | `Intrinsics?` | Camera intrinsics (empty if none) |

## Usage Example

```csharp
// Create a node info entry
NodeInfoForHubSettings nodeInfo = new NodeInfoForHubSettings
{
    name = "Node1",
    role = "Node",
    isActive = true,
    IPAddress = "192.168.1.20"
};

// Access from hub settings
var hubSettings = Settings.All.Hubs.FirstOrDefault(h => h.name == "Hub1");
if (hubSettings != null)
{
    Console.WriteLine($"Hub1 has {hubSettings.nodes.Count} nodes:");
    foreach (var node in hubSettings.nodes)
    {
        Console.WriteLine($"  - {node.name}: {(node.isActive ? "Active" : "Inactive")} at {node.IPAddress}");
    }
}
```

## Related Types

- `AppSettings` - Hubs collection containing nodes list
- `HubSettings` - Parent hub settings with nodes property
- `Intrinsics` - Camera calibration data
