# Dplus

An SBC-based, distributed computer vision system for creating 3D datasets from arbitrary 2D detection models.

## Overview 

This project is composed to two 'sides':

- A data generation side (C++ on SBC)
 - Operates on SBC (raspberryPi 3B+ were used)
 - Multiple 'node' devices, each with one web cam, capture images of a common detection space, and run one or more traditional 2D computer vision detection algorithm on the image
 - Detections from each node are transmitted via JSON over MQTT through dedicated AP to a central 'Hub' pi which creates 3D reconstructions of each detected point.
 - The hub sends reconstructed frames to Desktop Viewer for live viewing via JSON over MQTT through building Wi-Fi, separated from dedicated AP
 - Programs (node and hub) runs as a service daemon
 - The hub commands and synchronizes activities of its' nodes
- A Maintenance and Viewing Side (C# on Desktop)
 - 'Main' Form
  - Live data logging
  - Access to other forms
 - 'Uploader'
  - Can start/stop services
  - Push/Compile/Distribute new Code to Nodes and Hubs
  - Downloads device logs, images, and detection artifacts
 - 'Viewer'
  - Can view live reconstruction output and logged data

## Project File Architecture
```text

X:\YourDir
├───apps
│ └─── Dplus //<--This Repo's Root
│   ├───.git 
│   ├───Desktop 
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
  └─── mLogger  //<-- from 'https://github.com/jKill709/mLogger'
    ├─── mLogger.slnx
    │
    ├───.git 
    ├─── mLogger
    │ └───mLogger.csproj  // must be present
    ├─── mLogger_Winforms
      └───mLogger_Winforms.csproj
```

If you don't want to recreate the parent directory, or the 'apps' and 'libs' sibling directories, simply add a new reference to your clone of the repo through VS.

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