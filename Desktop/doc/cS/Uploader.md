# Uploader

**Namespace:** `Dplus_Desktop`

## Purpose

Cluster management form that allows uploading files, recompiling source code, distributing runtime files, and controlling cluster services (start/stop/reboot/shutdown).

## Constructors

### `Uploader()`

Constructor that initializes the form, loads clusters from settings, and sets up the UI.

```csharp
public Uploader()
{
    InitializeComponent();

    AddLogSource("Uploader");
    AddLogSource("ClusterManager");

    logger.LogHeading(LogLevel.INFO, "Uploader", "Uploader Initialing");

    clusters = new Dictionary<string, ClusterManager>();

    foreach (Device hub in Settings.All.Hubs)
    {
        clusters.Add(hub.ClusterID, new ClusterManager(hub, Settings.All.GetNodesByClusterID(hub.ClusterID)));
        Clusters_Box.Items.Add(hub.ClusterID);
    }

    if (Clusters_Box.Items.Count == 0)
    {
        MessageBox.Show("No hubs configured. Please configure settings first.");
        throw new Exception("No hubs configured. Please configure settings first.");
    }
    else
    {
        Clusters_Box.SelectedIndex = 0;

        Task.Run(async () => checkServiceStatus(clusters[Clusters_Box.Text]));

        UpdateManagedFiles_Boxes();
    }
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `clusters` | `Dictionary<string, ClusterManager>` | Dictionary mapping cluster IDs to ClusterManager instances |
| `LastUploadTime` | `DateTime` | Timestamp of last upload operation |

## Methods

### `LoadNodes()`

Loads the list of nodes for the currently selected cluster.

```csharp
private async Task LoadNodes()
{
    logger.Log(LogLevel.INFO, "Uploader", "Loading nodes for '" + Clusters_Box.SelectedItem?.ToString() + "'\n");
    Device hub = Settings.All.Hubs[Clusters_Box.SelectedIndex];

    Nodes_Box.Items.Clear();
    foreach (Device node in Settings.All.GetNodesByClusterID(hub.ClusterID))
    {
        ListViewItem item = new ListViewItem(node.Name);
        item.SubItems.Add(node.APAddress);
        Nodes_Box.Items.Add(item);
    }

    await HighlightNodes();
}
```

**Returns:** `Task` - Completion when nodes are loaded and highlighted

### `UpdateManagedFiles_Boxes()`

Updates all managed files display boxes (source, runtime, models).

```csharp
private void UpdateManagedFiles_Boxes()
{
    if (Settings.isLoaded == false)
    {
        Upload_Button.Enabled = false;
        Reboot_Button.Enabled = false;
        Shutdown_Button.Enabled = false;

        MessageBox.Show("Settings not loaded. Please configure settings first.");
        return;
    }
    else
    {
        Upload_Button.Enabled = true;
        Reboot_Button.Enabled = true;
        Shutdown_Button.Enabled = true;

        UpdateSourceFiles_Box();
        UpdateRuntimeFiles_Box();
        UpdateModels_Box();
    }
}
```

### `UpdateSourceFiles_Box()`

Updates the source files display box with upload status.

```csharp
private void UpdateSourceFiles_Box()
{
    SourceFiles_Box.Items.Clear();

    foreach (SourceFile file in Settings.All.SourceFiles)
    {
        string filePath = Path.Combine(Settings.All.SourceFilesDirectory, file.FileName);
        string lastUploadedTime = file.LastUploadTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
        string lastModifiedTime = file.LastModifiedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";

        Color color = Color.Black;

        try
        {
            if (file.LastModifiedTime.HasValue)
            {
                if (file.LastModifiedTime == DateTime.MinValue)
                    color = Color.Red;          // Local copy missing
                if (file.LastUploadTime == DateTime.MinValue)
                    color = Color.Orange;       // Hub copy missing
                else if (file.LastModifiedTime > file.LastUploadTime)
                    color = Color.Yellow;       // Needs upload to update
                else
                    color = Color.Green;        // Good to go
            }
            else
            {
                color = Color.DarkRed;          // No last modified (file likely missing)
            }
        }
        catch
        {
            color = Color.DarkRed;              // Error
        }

        var item = new ListViewItem(file.FileName);
        item.UseItemStyleForSubItems = true;
        item.BackColor = color;
        item.SubItems.Add(lastUploadedTime);
        item.SubItems.Add(lastModifiedTime);
        item.SubItems.Add(file.IsForHub ? "Yes" : "No");
        item.SubItems.Add(file.IsForNode ? "Yes" : "No");
        item.UseItemStyleForSubItems = true;
        foreach (ListViewItem.ListViewSubItem sub in item.SubItems)
            sub.BackColor = color;

        SourceFiles_Box.Items.Add(item);
    }
}
```

### `UpdateRuntimeFiles_Box()`

Updates the runtime files display box with compilation/push status.

```csharp
private void UpdateRuntimeFiles_Box()
{
    RuntimeFiles_Box.Items.Clear();

    foreach (RuntimeFile file in Settings.All.RuntimeFiles)
    {
        string lastSourceChange = file.LastSourceChangeTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
        string lastCompliedTimeString = file.LastCompliedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
        string pushedTimeString = file.LastPushedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";

        Color color = Color.Black;

        if ((file.LastSourceChangeTime.HasValue && file.LastSourceChangeTime > DateTime.MinValue) && 
            (file.LastCompliedTime.HasValue && file.LastCompliedTime > DateTime.MinValue) && 
            (file.LastPushedTime.HasValue && file.LastPushedTime > DateTime.MinValue))
        {
            if (file.LastSourceChangeTime > file.LastCompliedTime)
                color = Color.Yellow;       // Needs to be compiled
            else if (file.LastCompliedTime > file.LastPushedTime)
                color = Color.Orange;       // Need to be distributed
            else
                color = Color.Green;        // Good to go
        }
        else
        {
            color = Color.Red;              // No last modified (cluster likely not available)
        }

        var item = new ListViewItem(file.FileName);
        item.SubItems.Add(lastSourceChange);
        item.SubItems.Add(lastCompliedTimeString);
        item.SubItems.Add(pushedTimeString);
        item.BackColor = color;
        item.UseItemStyleForSubItems = true;
        RuntimeFiles_Box.Items.Add(item);
    }
}
```

### `UpdateModels_Box()`

Updates the models display box with push status.

```csharp
private void UpdateModels_Box()
{
    ModelFiles_Box.Items.Clear();

    foreach (ModelFile file in Settings.All.Models)
    {
        string localPath = Path.Combine(Settings.All.LocalModelsPath, file.ModelType, file.ModelName);
        string lastPushTime = file.LastPushTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
        string lastModifiedTime = file.LastModifiedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";

        Color color = Color.Black;

        if (file.LastModifiedTime.HasValue && file.LastPushTime.HasValue)
        {
            if (file.LastModifiedTime == DateTime.MinValue)
                color = Color.DarkRed;
            else if (file.LastPushTime == DateTime.MinValue)
                color = Color.Red;          // No last modified and/or pushed time (file likely missing)
            else if (file.LastModifiedTime > file.LastPushTime)
                color = Color.Yellow;       // Needs to be compiled
            else
                color = Color.Green;        // Good to go
        }
        else
        {
            color = Color.DarkRed;              // No last modified and/or pushed time (file likely missing)
        }

        var item = new ListViewItem(file.ModelName);
        item.SubItems.Add(file.ModelType);
        item.SubItems.Add(lastPushTime);
        item.SubItems.Add(lastModifiedTime);
        item.BackColor = color;
        item.UseItemStyleForSubItems = true;

        ModelFiles_Box.Items.Add(item);
    }
}
```

### `HighlightNodes()`

Highlights nodes based on their service status.

```csharp
private async Task HighlightNodes()
{
    foreach (ListViewItem nodeItem in Nodes_Box.Items)
    {
        Device? nodeDevice = Settings.All.Nodes.Find(d => d.Name == nodeItem.SubItems[0].Text);
        Color color = Color.Black;

        if (nodeDevice != null)
        {
            setControlablilty(false);
            ServiceStatus status = await clusters[Clusters_Box.Text].CheckDeviceServiceStatus(nodeDevice);
            setControlablilty(true);

            switch (status)
            {
                case ServiceStatus.Active:
                    color = Color.Green;
                    break;
                case ServiceStatus.Inactive:
                    color = Color.Yellow;
                    break;
                case ServiceStatus.Failed:
                    color = Color.Red;
                    break;
                case ServiceStatus.Activating:
                    color = Color.Green;
                    break;
                case ServiceStatus.Deactivating:
                    color = Color.Yellow;
                    break;
                case ServiceStatus.Error:
                    color = Color.DarkRed;
                    break;
                default:
                    color = Color.Gray;
                    break;
            }

            nodeItem.BackColor = color;
        }
        else
        {
            color = Color.DarkRed;
            logger.Log(LogLevel.ERROR, "Uploader", $"Device not found in Settings.All.Nodes for name '{nodeItem.SubItems[0].Text}'.\n");
        }

        nodeItem.BackColor = color;
    }
}
```

**Returns:** `Task` - Completion when nodes are highlighted

### `checkServiceStatus(ClusterManager com)`

Checks service status for the current cluster.

```csharp
private async Task checkServiceStatus(ClusterManager com)
{
    setControlablilty(false);
    CurrentCluster_StatusStrip.UpdateStatus(await com.CheckSystem());
    setControlablilty(true);
}
```

### `setControlablilty(bool enable)`

Enables or disables all cluster control buttons.

```csharp
private void setControlablilty(bool enable)
{
    Reboot_Button.Enabled = enable;
    Shutdown_Button.Enabled = enable;
    Upload_Button.Enabled = enable;
    CreateJSONfiles_Button.Enabled = enable;
    ManualRecompile_Button.Enabled = enable;
    DistributeRuntimeFiles_Button.Enabled = enable;
    AutoRecompile_Button.Enabled = enable;
    BackupFirst_Box.Enabled = enable;
    StartService_Button.Enabled = enable;
    Download_Button.Enabled = enable;
    StopService_Button.Enabled = enable;
    CheckServiceStatus_Button.Enabled = enable;
}
```

## Usage Example

```csharp
// Create uploader form
using (Uploader uploader = new Uploader())
{
    uploader.ShowDialog();
}

// Access cluster manager
var clusterManager = uploader.clusters[uploader.Clusters_Box.Text];

// Check system status
ClusterStatus status = await clusterManager.CheckSystem();
Console.WriteLine($"SSH: {status.SSHConnected}, Nodes: {status.NodeCount}");

// Upload files and models
await clusterManager.UploadFiles();

// Download logs and reconstructions
await clusterManager.DownloadFiles();

// Manual recompile with backup
await clusterManager.ManualRecompile(true);

// Distribute runtime files
await clusterManager.DistributeRuntimeFiles();

// Start services
await clusterManager.startMain();

// Stop services
await clusterManager.stopMain();

// Reboot cluster
await clusterManager.RebootCluster();

// Shutdown cluster
await clusterManager.ShutdownCluster();

// Create new settings files
clusterManager.CreateSettingsFiles();
```

## Related Types

- `Device` - Hub and node device configurations
- `ClusterManager` - Cluster management logic
- `SourceFile`, `RuntimeFile`, `ModelFile` - File tracking structures
- `ServiceStatus` - Service state enum
- `MqttWorker` - MQTT communication used internally
