# IClusterStatusDisplay

**Namespace:** `Dplus_Desktop.cS.WinForms.Controls.ClusterStatusDisplay`

## Purpose

Interface for controls that can display cluster status information. Enables polymorphic handling of status displays throughout the application.

## Methods

### `UpdateStatus(ClusterStatus status)`

Updates the control's displayed status with new cluster information.

```csharp
void UpdateStatus(ClusterStatus status)
```

**Parameters:**
- `status`: New ClusterStatus to display

## Usage Example

```csharp
// Use polymorphically
List<IClusterStatusDisplay> displays = new List<IClusterStatusDisplay>();
displays.Add(_uploader.CurrentCluster_StatusStrip);
displays.Add(_viewer.ClusterStatusDisplay);

await clusterManager.CheckSystem();
foreach (var display in displays)
{
    display.UpdateStatus(status);
}
```

## Related Types

- `DeviceStatusStrip` - Implementation of this interface
- `ClusterStatus` - Status data displayed by this interface
