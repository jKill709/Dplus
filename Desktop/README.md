# Dplus Desktop Application

C# WinForms application for managing camera clusters. This is the desktop management console that communicates with embedded hub and node devices via SSH, as well as with other cluster members via MQTT.

## Building

```bash
dotnet build
```

The application requires .NET 8.0 runtime. Build artifacts are in `bin/` directories.

## Running

```bash
dotnet run
```

## Features

- Cluster-wide device management via SSH to hub and nodes
- Real-time status monitoring via MQTT
- Image upload and viewing with OpenCV
- YOLO-based object detection (Pose and Object)
- Charuco board calibration
- Configurable detector profiles
- Multi-node cluster support

## Documentation

For detailed API documentation, see the `doc/` directory:

- [Main form](./doc/cS/Main.md) - Application entry point and main window
- [Viewer form](./doc/cS/Viewer.md) - Image display with overlays
- [Uploader form](./doc/cS/Uploader.md) - Image upload functionality
- [ClusterManager](./doc/cS/Network/ClusterManager.md) - Node communication via SSH
- [ClusterStatus](./doc/cS/Network/ClusterStatus.md) - MQTT-based status updates
- [SettingsManager](./doc/cS/SettingsManager/) - All configuration types
- [ImageControls](./doc/Dplus_Desktop/ImageControls.md) - Image display controls

For architecture overview, see [ARCHITECTURE.md](./doc/ARCHITECTURE.md).

## Configuration

The desktop application reads its configuration from `managerSettings.json`. See the [SettingsManager documentation](./doc/cS/SettingsManager/) for details on all configuration types.

An example configuration is provided in `managerSettings.example.json`.

## MQTT Topics

The desktop application subscribes to cluster MQTT topics and publishes commands. See [ARCHITECTURE.md](./doc/ARCHITECTURE.md#mqtt-messaging) for topic details.
