# Dplus Documentation Index

This directory contains comprehensive documentation for the Dplus project.

## Root Level Documentation

- [README.md](../README.md) - Project overview and quick start guide
- [ARCHITECTURE.md](../ARCHITECTURE.md) - System architecture, data flow, and MQTT messaging
- [Desktop/README.md](../Desktop/README.md) - Desktop application documentation
- [Embedded/README.md](../Embedded/README.md) - Embedded firmware documentation

## Desktop Application Documentation

### Forms

- [Main.md](../Desktop/doc/cS/Main.md) - Main entry point form
- [Viewer.md](../Desktop/doc/cS/Viewer.md) - Image display with overlays
- [Uploader.md](../Desktop/doc/cS/Uploader.md) - Image upload functionality

### Network Components

- [ClusterManager.md](../Desktop/doc/cS/Network/ClusterManager.md) - SSH-based node communication
- [ClusterStatus.md](../Desktop/doc/cS/Network/ClusterStatus.md) - MQTT-based status updates

### Settings Manager

All configuration types are documented in the SettingsManager directory. See [Desktop/README.md](../Desktop/README.md) for an overview.

### WinForms Controls

- [DeviceStatusStrip.md](../Desktop/doc/cS/WinForms/Controls/ClusterStatusDisplay/DeviceStatusStrip.md)
- [IClusterStatusDisplay.md](../Desktop/doc/cS/WinForms/Controls/ClusterStatusDisplay/IClusterStatusDisplay.md)

### Image Controls

- [ImageControls.md](../Desktop/doc/Dplus_Desktop/ImageControls.md) - Comprehensive documentation of all image display controls

## Embedded Firmware Documentation

- [hub.cpp.md](../Embedded/hub.cpp.md) - Hub device implementation
- [node.cpp.md](../Embedded/node.cpp.md) - Node device implementation
- [camera.cpp.md](../Embedded/camera.cpp.md) - Camera capture functionality
- [detectors.cpp.md](../Embedded/detectors.cpp.md) - YOLO detection logic

### Per-File Documentation

Detailed documentation for each source file is available in the `doc/` subdirectory.

## API Reference

This documentation includes complete API references for all public types in the Desktop application, including:

- All forms and their members
- Network components (ClusterManager, ClusterStatus)
- Settings types (Device, Intrinsics, YoloPoseDetectorProfile, etc.)
- Image controls (OrthographicViewer, PerspectiveViewer, BoundingBox, etc.)
- WinForms controls (DeviceStatusStrip, IClusterStatusDisplay)

Each type document includes:
- Purpose and namespace
- Constructors
- Properties
- Methods with full signatures and descriptions
- Events
- Usage examples
- Related types

## How to Use This Documentation

### For Users

Start with the [README.md](../README.md) at the root level for an overview. Then explore:
- Desktop application features in [Desktop/README.md](../Desktop/README.md)
- Embedded firmware in [Embedded/README.md](../Embedded/README.md)

### For Developers

Refer to the architecture documentation first:
- [ARCHITECTURE.md](../ARCHITECTURE.md) for system design and data flow

Then consult the API reference in the `doc/` directory for detailed type information.

### For Contributors

The embedded firmware documentation provides implementation details, while the Desktop API docs show the public interface.

## Documentation Quality

This documentation was generated from the source code using automated tools, with additional context added to ensure clarity and completeness. All public members are documented with examples where applicable.
