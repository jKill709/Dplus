# Dplus

A single board computer based, distributed computer vision system for creating 3D datasets from arbitrary 2D detection models.

## Overview

Dplus provides a unified interface for operating camera cluster hardware. It consists of two main components:

- **Desktop Application** (C# WinForms) - The management console that communicates with the cluster via SSH to hub and node devices
- **Embedded Firmware** (C++) - Runs on the actual hardware (hub and node devices), handling camera control, image processing, and MQTT messaging

## Features

- Cluster-wide device management
- Real-time status monitoring via MQTT
- Image upload and viewing
- YOLO-based object detection (Pose and Object)
- Charuco board calibration
- SSH-based communication with embedded devices
- Configurable detector profiles
- Multi-node cluster support

## Requirements

### Desktop Application

- .NET 8.0 runtime
- OpenSSH client (built into most modern OS)
- SSH access to hub and node devices

### Embedded Firmware

- MSVC C++ compiler (Windows) or appropriate toolchain for your platform
- vcpkg package manager (for optional dependencies)

## Project Structure

```
Dplus/
├── README.md                 # This file
├── ARCHITECTURE.md           # Overall architecture documentation
├── Dplus.sln                 # Solution file
├── Embedded/                 # C++ embedded firmware
│   ├── src/                 # Source files
│   │   ├── hub.cpp          # Hub device implementation
│   │   ├── node.cpp         # Node device implementation
│   │   ├── camera.cpp       # Camera control
│   │   ├── detectors.cpp    # YOLO detection logic
│   │   └── settings.cpp     # Settings management
│   ├── doc/                 # Per-file documentation
│   ├── README.md            # Embedded firmware readme
│   └── CamSandbox.vcxproj   # C++ project file
├── Desktop/                 # C# WinForms application
│   ├── Dplus_Desktop.csproj # .NET project
│   ├── managerSettings.json # Configuration
│   ├── src/                 # Source files
│   │   ├── cS/              # Main forms and logic
│   │   ├── Network/         # Cluster management
│   │   └── images/          # UI assets
│   └── doc/                 # API documentation
│       ├── cS/              # Forms and managers
│       ├── Network/         # Cluster components
│       └── SettingsManager/ # Configuration types
└── vcpkg/                   # Optional dependencies
```

## Building

### Desktop Application

```bash
cd Desktop
dotnet build
```

### Embedded Firmware

Open `Embedded/CamSandbox.vcxproj` in Visual Studio and build.

## Documentation

Comprehensive documentation is available:

- **Root level**: [ARCHITECTURE.md](./doc/ARCHITECTURE.md) - Overall system architecture, data flow, MQTT messaging
- **Desktop**: [README.md](./Desktop/README.md) and `doc/` directory - API reference for all public types
- **Embedded**: [README.md](./Embedded/README.md) and `doc/` directory - Per-file documentation

## Configuration

See the respective README files for configuration details:

- Desktop: `Desktop/managerSettings.json`
- Embedded: `Embedded/src/managerSettings.json`

The configuration structure is documented in the API reference under `SettingsManager`.

## MQTT Topics

The embedded devices use MQTT for cluster-wide communication. Topics and message formats are documented in [ARCHITECTURE.md](./doc/ARCHITECTURE.md#mqtt-messaging).

## License

Copyright © 2026 Jeremy Killinger. See LICENSE for details.
