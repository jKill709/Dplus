# ClusterManager

**Namespace:** `Dplus_Desktop`

## Purpose

Manages one Dplus_Embedded cluster (1 Hub and many Nodes) using a jCommunicator.Communicator. Provides Dplus-specific functionality including file uploads/downloads, recompilation, service control, and settings management.

## Constructors

### `ClusterManager(Device hub, List<Device> nodes)`

Constructor that establishes SSH connection to the hub and creates tunnels to active nodes.

```csharp
public ClusterManager(Device hub, List<Device> nodes)
{
    _hub = hub;
    _hubCom = new Communicator(hub.IPAddress, hub.Username, hub.Password);
    Task.Run(async () => await _hubCom.ConnectAsync());

    _nodes = new List<Device>();
    if ((_hubCom.IsConnected) && (nodes != null) && (nodes.Count > 0))
        foreach (Device node in nodes)
        {
            _nodes.Add(node);
            if (node.isActive)
            {
                Task.Run(async () => await _hubCom.AddNodeTunnelAsync(node.APAddress, node.Username, node.Password, true));
            }
        }

    Task.Run(async () => LoadManagedFiles());
}
```

**Parameters:**
- `hub`: The hub device configuration
- `nodes`: List of node devices (only active ones receive tunnels)

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `_hub` | `Device` | The Hub for this cluster |
| `_nodes` | `List<Device>` | A list of node objects representing each node in the cluster |

## Methods

### `CheckSystem()`

Asynchronously checks the overall system status, including SSH connectivity and service states.

```csharp
public async Task<ClusterStatus> CheckSystem()
```

**Returns:** `Task<ClusterStatus>` - Cluster status information

### `CheckSSH(bool verbose = false)`

Tests SSH connectivity to the hub device.

```csharp
public async Task<bool> CheckSSH(bool verbose = false)
```

**Parameters:**
- `verbose`: Log connection attempts (default: false)

**Returns:** `Task<bool>` - True if connected successfully

### `CheckDeviceServiceStatus(string deviceName)`

Gets service status for a device by name.

```csharp
public async Task<ServiceStatus> CheckDeviceServiceStatus(string deviceName)
```

**Parameters:**
- `deviceName`: Name of the device to check

**Returns:** `Task<ServiceStatus>` - Service status enum value

### `CheckDeviceServiceStatus(Device device)`

Gets service status for a device object.

```csharp
public async Task<ServiceStatus> CheckDeviceServiceStatus(Device device)
```

**Parameters:**
- `device`: Device object to check

**Returns:** `Task<ServiceStatus>` - Service status enum value

### `UploadFiles()`

Uploads source files and model files to the cluster.

```csharp
public async Task UploadFiles()
```

**Returns:** `Task` - Completion when uploads finish

### `DownloadFiles()`

Downloads logs, reconstructions, and calibration data from the cluster.

```csharp
public async Task DownloadFiles()
```

**Returns:** `Task` - Completion when downloads finish

### `ManualRecompile(bool backupFirst)`

Manually recompile source files, optionally backing up previous binaries first.

```csharp
public async Task ManualRecompile(bool backupFirst)
```

**Parameters:**
- `backupFirst`: Whether to backup bin directories before recompilation (default: false)

**Returns:** `Task` - Completion when recompilation finishes

### `AutoRecompile(bool backupFirst)`

Automatically recompile and distribute runtime files.

```csharp
public async Task AutoRecompile(bool backupFirst)
```

**Parameters:**
- `backupFirst`: Whether to backup bin directories before recompilation (default: false)

**Returns:** `Task` - Completion when recompilation and distribution finish

### `DistributeRuntimeFiles()`

Distributes runtime files (binaries and settings) to all nodes.

```csharp
public async Task DistributeRuntimeFiles()
```

**Returns:** `Task` - Completion when distribution finishes

### `startMain()`

Starts the main service daemon on the hub and all nodes.

```csharp
public async Task startMain()
```

**Returns:** `Task` - Completion when services are started

### `stopMain()`

Stops the main service daemon on the hub and all nodes.

```csharp
public async Task stopMain()
```

**Returns:** `Task` - Completion when services are stopped

### `RebootCluster()`

Reboots all nodes then the hub.

```csharp
public async Task RebootCluster()
```

**Returns:** `Task` - Completion when cluster reboots finish

### `ShutdownCluster()`

Shuts down all nodes then the hub.

```csharp
public async Task ShutdownCluster()
```

**Returns:** `Task` - Completion when cluster shutdowns finish

### `CreateSettingsFiles()`

Creates new settings files for the cluster, backing up existing ones if present.

```csharp
public void CreateSettingsFiles()
```

## Usage Example

```csharp
// Create cluster manager from settings
Device hub = Settings.All.GetDeviceByName("Hub1");
List<Device> nodes = Settings.All.GetNodesByClusterID(hub.ClusterID);
var clusterManager = new ClusterManager(hub, nodes);

// Check system status
ClusterStatus status = await clusterManager.CheckSystem();
Console.WriteLine($"SSH: {status.SSHConnected}, Hub: {status.HubServiceStatus}");

// Upload files and models
await clusterManager.UploadFiles();

// Download logs and reconstructions
await clusterManager.DownloadFiles();

// Manual recompile with backup
await clusterManager.ManualRecompile(true);

// Distribute runtime files after recompilation
await clusterManager.DistributeRuntimeFiles();

// Start services
await clusterManager.startMain();

// Stop services
await clusterManager.stopMain();

// Reboot all devices
await clusterManager.RebootCluster();

// Create fresh settings
clusterManager.CreateSettingsFiles();
```

## Related Types

- `Device` - Hub and node device configurations
- `ClusterStatus` - Status returned by CheckSystem()
- `ServiceStatus` - Enum for service states
- `MqttWorker` - MQTT communication used internally
