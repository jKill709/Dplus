# Device

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Represents a Dplus cluster device - either a Hub or Node. Contains connection information (IP address, credentials), role identification, and cluster membership. Used throughout the application for device management and settings operations.

## Constructors

### `Device()`

Default constructor initializes all properties to empty/default values.

```csharp
public Device()
{
    Name = string.Empty;
    Role = string.Empty;
    isActive = false;
    ClusterID = string.Empty;
    IPAddress = string.Empty;
    APAddress = string.Empty;
    Username = string.Empty;
    Password = string.Empty;
    CameraIDnumber = 0;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Unique device identifier (e.g., "Hub1", "Node2") |
| `Role` | `string` | Device role: "Hub" or "Node" |
| `isActive` | `bool` | Whether device is currently active in cluster |
| `ClusterID` | `string` | Cluster identifier for multi-cluster setups |
| `IPAddress` | `string` | SSH connection IP address |
| `APAddress` | `string` | Local network AP address (for ping) |
| `Username` | `string` | SSH username |
| `Password` | `string` | SSH password |
| `CameraIDnumber` | `int` | Camera ID for intrinsics lookup (0 if none) |

## Usage Example

```csharp
// Create a hub device
Device hub = new Device
{
    Name = "Hub1",
    Role = "Hub",
    isActive = true,
    ClusterID = "CLUSTER001",
    IPAddress = "192.168.1.10",
    APAddress = "192.168.1.10",
    Username = "admin",
    Password = "password123"
};

// Create a node device
Device node = new Device
{
    Name = "Node2",
    Role = "Node",
    isActive = true,
    ClusterID = "CLUSTER001",
    IPAddress = "192.168.1.20",
    APAddress = "192.168.1.20",
    Username = "admin",
    Password = "password123",
    CameraIDnumber = 42
};

// Use with SettingsManager
Device? found = Settings.All.GetDeviceByName("Hub1");
if (found != null)
{
    Console.WriteLine($"Connected to {found.Name} at {found.IPAddress}");
}
```

## Related Types

- `AppSettings` - Parent settings container
- `Intrinsics`, `Extrinsics` - Calibration data associated with devices
- `ClusterProfile` - Configuration applied across multiple devices
