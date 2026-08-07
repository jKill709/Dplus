# Dplus

A Single Board Computer (SBC) based, distributed computer vision system for creating 3D datasets from arbitrary 2D detection models.

## Overview 

This project is composed to two 'sides':

- A data generation side (C++ on SBC)
    - Operates on SBC (raspberryPi 3B+ were used)
    - Multiple 'node' devices, each with one web cam, capture images of a common detection space, and run one or more traditional 2D computer vision detection algorithms on the image
    - Detections from each node are transmitted via JSON over MQTT through dedicated AP to a central 'Hub' pi which creates 3D reconstructions of each detected point.
    - The hub sends reconstructed frames to Client Viewer for live viewing via JSON over MQTT through building Wi-Fi, separated from dedicated AP
    - Programs (node and hub) run as service daemons
    - The hub commands and synchronizes activities of its' nodes
- A Maintenance and Viewing Side (C# Client)
    - 'Main' Form
        - Live data logging
        - Access to other forms
    - 'Uploader'
        - Can start/stop services
        - Push/Compile/Distribute new Code to Nodes and Hubs
        - Downloads device logs, images, and detection artifacts
    - 'Viewer'
        - Can view live reconstruction output and logged data
        - Shows source images (after download only), 2D detection data and 3D reconstructions

## Project File Architecture

This project has three distinct system architectures.  The 

### Client side

For the Client side of the system, install this repo, and its dependencies as shown, or simply relink projects in VS after downloading each repo to you preferred  location.

```text

X:\YourDir
├───apps
│ └─── Dplus            //<--This Repo's Root 'https://github.com/jkill709/Dplus'
│   ├───.git 
│   ├───Desktop
│   │ ├─── managerSettings.example.json
│   │ ├───.... 
│   │ ├───src 
│   │ │ ├───cS
│   │ │ │  └───CS source files 
│   │ │ └───images 
│   │ ├───... 
│   │ └───Dplus_Desktop.csproj 
│   ├───Embedded 
│   │ └───... 
│   │ ├───src 
│   │ │ └───c++ source files 
│   │ ├───... 
│   │ └───Dplus_Embedded.csproj 
│   ├───Logs 
│   │ ├───CamManager 
│   │ ├───Hub1 
│   │ │ └───... 
│   │ ├───Node1 
│   │ │ └───... 
│   │ ├───Node2 
│   │ │ └───... 
│   │ ├───Node3 
│   │ │ └───... 
│   │ └───Node4 
│   │ └───... 
│   └───.gitignore
└─── libs
  ├───jCommunicator     //<-- from 'https://github.com/jkill709/jCommunicator'
  │ ├─── jCommunicator.slnx
  │ │
  │ ├───.git 
  │ ├───jCommunicator
  │ │ └───... 
  │ ├───jCommunicator_UnitTests
  │ │ └───... 
  └─── mLogger          //<-- from 'https://github.com/jKill709/mLogger'
    ├─── mLogger.slnx
    │
    ├───.git 
    ├─── mLogger
    │ └───mLogger.csproj
    └─── mLogger_Winforms
      └───mLogger_Winforms.csproj
```

### Cluster Side (Hub)


```text

~
├───build
│   └───...
├───hub
├───logs
│   ├───...
│   └───HubX_YYYY-MM-DD.log
├───node
├───previous_hub
├───previous_node
├───reconstructions
│   ├───...
│   └───1779XXXXXXXXX(X).json
└───src
    ├───BoardGen
    │   ├───BoardGen
    │   ├───charuco_board_BigMedium.png
    │   ├───charuco_board_BigSmall.png
    │   ├───charuco_board_Large.png
    │   ├───charuco_board_Medium.png
    │   └───charuco_board_Small.png
    ├───hub
    │   ├───calibration.h
    │   ├───CMakeLists.txt
    │   ├───detections.h
    │   ├───detectors.cpp
    │   ├───detectors.h
    │   ├───hub.cpp
    │   ├───hubSettings.json
    │   ├───indicatorBank.cpp
    │   ├───indicatorBank.h
    │   ├───messages.hpp
    │   ├───nodeLink.cpp
    │   ├───nodeLink.h
    │   ├───nodeLogger.cpp
    │   ├───nodeLogger.h
    │   ├───roomViewer.cpp
    │   ├───roomViewer.h
    │   ├───settings.cpp
    │   ├───settings.h
    │   └───stopwatch.h
    ├───hubSettings.json
    ├───node
    │   ├───calibration.h
    │   ├───camera.cpp
    │   ├───camera.h
    │   ├───CMakeLists.txt
    │   ├───detections.h
    │   ├───detectors.cpp
    │   ├───detectors.h
    │   ├───indicatorBank.cpp
    │   ├───indicatorBank.h
    │   ├───messages.hpp
    │   ├───node.cpp
    │   ├───nodeLink.cpp
    │   ├───nodeLink.h
    │   ├───nodeLogger.cpp
    │   ├───nodeLogger.h
    │   ├───nodeSettings.json
    │   ├───settings.cpp
    │   ├───settings.h
    │   └───stopwatch.h
    └───nodeSettings.json

```

### Cluster Side (Node)

```text

~
├───captures
│   ├───Captures
│   │   ├─── Frame_mmmDDYY_HHmmss.png
│   │   ├─── ...
│   │   └─── Frame_mmmDDYY_HHmmss.png
│   ├───Charuco
│   │   ├─── Frame_mmmDDYY_HHmmss.png
│   │   ├─── ...
│   │   └─── Frame_mmmDDYY_HHmmss.png
│   ├───Chessboard
│   │   ├─── Frame_mmmDDYY_HHmmss.png
│   │   ├─── ...
│   │   └─── Frame_mmmDDYY_HHmmss.png
│   ├───Face
│   │   ├─── Frame_mmmDDYY_HHmmss.png
│   │   ├─── ...
│   │   └─── Frame_mmmDDYY_HHmmss.png
│   ├───Motion
│   │   ├─── Frame_mmmDDYY_HHmmss.png
│   │   ├─── ...
│   │   └─── Frame_mmmDDYY_HHmmss.png
│   ├───Preprocessed
│   ├───Startup
│   │   ├─── Frame_mmmDDYY_HHmmss.png
│   │   ├─── ...
│   │   └─── Frame_mmmDDYY_HHmmss.png
│   ├───YoloObject
│   │   ├─── Frame_mmmDDYY_HHmmss.png
│   │   ├─── ...
│   │   └─── Frame_mmmDDYY_HHmmss.png
│   └───YoloPose
│   │   ├─── Frame_mmmDDYY_HHmmss.png
│   │   ├─── ...
│   │   └─── Frame_mmmDDYY_HHmmss.png
├───logs
│   └───NodeX_YYYY-MM-DD.log
├───models
│   ├───Haar-Face
│   │   ├───face_trained.yml
│   │   └───haar_face.xml
│   ├───YOLO11-Objects
│   │   ├───base_optmized.onnx
│   │   ├───best_int8.onnx
│   │   ├───best_int8.ort
│   │   ├───best.onnx
│   │   ├───best_optmized.onnx
│   │   ├───best.ort
│   │   ├───best_quant.onnx
│   │   └───classes.txt
│   └───YOLO11-Pose
│       ├───yolo11l-pose.onnx
│       ├───yolo11l-pose.pt
│       ├───yolo11m-pose.onnx
│       ├───yolo11m-pose.pt
│       ├───yolo11n-pose.onnx
│       ├───yolo11n-pose.pt
│       ├───yolo11s-pose.onnx
│       ├───yolo11s-pose.pt
│       ├───yolo11x-pose.onnx
│       └───yolo11x-pose.pt
├───node
└───src
    └───nodeSettings.json

```

## Dependencies

 - C# side
   - mLogger: 'https://github.com/jKill709/mLogger'
   - MQTTnet (4.3.6.1152)
   - openCvSharp4 (4.11.0.20250507)
   - openCvSharp4.Extensions (4.11.0.20250507)
   - openCvSharp4.runtime.win (4.11.0.20250507)
   - ssh.net (2025.0.0)

 - c++ side
   - Many.  Still documenting existing system state and setup path

## Dev Notes

This codebase and its documentation are works in progress.  Please check back for regular updates, especially for documentation if you're trying to recreate this project yourself.
