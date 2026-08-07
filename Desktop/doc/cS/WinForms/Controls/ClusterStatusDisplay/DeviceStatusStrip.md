# DeviceStatusStrip

**Namespace:** `Dplus_Desktop.cS.WinForms.Controls.ClusterStatusDisplay`

## Purpose

Custom status strip control that displays cluster connection and service status information in a compact, visual format. Used throughout the WinForms UI to show cluster health.

## Constructors

### `DeviceStatusStrip()`

Default constructor initializes with an empty ClusterStatus and default colors.

```csharp
public DeviceStatusStrip()
{
    _status = new ClusterStatus();
    _sshColor = Color.Green;
    _nodeCountColor = Color.Green;
    _hubServiceColor = Color.Green;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `_status` | `ClusterStatus` | Current cluster status (read-only) |
| `_sshColor` | `Color` | Color for SSH connection indicator (default: Green) |
| `_nodeCountColor` | `Color` | Color for node count indicator (default: Green) |
| `_hubServiceColor` | `Color` | Color for hub service indicator (default: Green) |

## Methods

### `UpdateStatus(ClusterStatus status)`

Updates the displayed status with new cluster information.

```csharp
public void UpdateStatus(ClusterStatus status)
{
    _status = status;
    _sshColor = status.SSHConnected ? Color.Green : Color.Red;
    _nodeCountColor = status.NodeCount > 0 ? Color.Green : Color.Gray;
    _hubServiceColor = MapServiceStatus(status.HubServiceStatus);
}
```

**Parameters:**
- `status`: New ClusterStatus to display

### `MapServiceStatus(ServiceStatus status)`

Maps a ServiceStatus enum value to an indicator color.

```csharp
private Color MapServiceStatus(ServiceStatus status)
{
    switch (status)
    {
        case ServiceStatus.Active:
            return Color.Green;
        case ServiceStatus.Inactive:
            return Color.Yellow;
        case ServiceStatus.Failed:
        case ServiceStatus.Error:
            return Color.Red;
        case ServiceStatus.Activating:
            return Color.LightGreen;
        case ServiceStatus.Deactivating:
            return Color.LightYellow;
        default:
            return Color.Gray;
    }
}
```

**Returns:** `Color` - Indicator color based on service status

## Usage Example

```csharp
// Create status strip
DeviceStatusStrip statusStrip = new DeviceStatusStrip();

// Use in form
this.Controls.Add(statusStrip);
statusStrip.Dock = DockStyle.Bottom;
statusStrip.Height = 40;

// Update with cluster status
await clusterManager.CheckSystem();
ClusterStatus status = await clusterManager.CheckSystem();
statusStrip.UpdateStatus(status);
```

## Related Types

- `ClusterStatus` - Status data displayed by this control
- `ServiceStatus` - Enum values mapped to colors
- `Color` - Indicator color types
