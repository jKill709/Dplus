# ClusterStatus

**Namespace:** `Dplus_Desktop`

## Purpose

Implements a data transfer object (DTO) representing the status of a Dplus cluster connection and services. Returned by `ClusterManager.CheckSystem()` to provide a snapshot of cluster health without holding any managed resources.

## Constructors

### `ClusterStatus(bool SSHConnected, int NodeCount, ServiceStatus HubServiceStatus, Dictionary<string, ServiceStatus> NodeServiceStatuses)`

Primary constructor with all status information.

```csharp
public ClusterStatus(bool SSHConnected, int NodeCount, ServiceStatus HubServiceStatus, Dictionary<string, ServiceStatus> NodeServiceStatuses)
{
    this.SSHConnected = SSHConnected;
    this.NodeCount = NodeCount;
    this.HubServiceStatus = HubServiceStatus;
    this.NodeServiceStatuses = NodeServiceStatuses;
}
```

**Parameters:**
- `SSHConnected`: Whether SSH connection to hub is established
- `NodeCount`: Number of active nodes in cluster
- `HubServiceStatus`: Service status for the hub daemon
- `NodeServiceStatuses`: Dictionary mapping node names to their service statuses

### `ClusterStatus()`

Default constructor for creating an empty/unknown status object.

```csharp
public ClusterStatus()
{
    this.SSHConnected = false;
    this.NodeCount = 0;
    this.HubServiceStatus = ServiceStatus.Error;
    this.NodeServiceStatuses = new Dictionary<string, ServiceStatus>();
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `SSHConnected` | `bool` | Whether SSH connection to hub is established |
| `NodeCount` | `int` | Number of active nodes in cluster |
| `HubServiceStatus` | `ServiceStatus` | Service status for the hub daemon |
| `NodeServiceStatuses` | `Dictionary<string, ServiceStatus>` | Dictionary mapping node names to their service statuses |

## Usage Example

```csharp
// ClusterManager returns this type
ClusterStatus status = await clusterManager.CheckSystem();

Console.WriteLine($"SSH Connected: {status.SSHConnected}");
Console.WriteLine($"Nodes: {status.NodeCount}");
Console.WriteLine($"Hub Service: {status.HubServiceStatus}");

foreach (var node in status.NodeServiceStatuses)
{
    Console.WriteLine($"  {node.Key}: {node.Value}");
}

// Use in UI
if (!status.SSHConnected || status.HubServiceStatus != ServiceStatus.Active)
{
    CurrentCluster_StatusStrip.UpdateStatus(status);
}
```

## Related Types

- `ClusterManager` - Returns ClusterStatus from CheckSystem()
- `Device` - Node names in NodeServiceStatuses map to Device.Name
- `ServiceStatus` - Enum for hub and node service states
