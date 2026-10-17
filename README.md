# Kinect4FOG

Kinect4FOG is research software for detecting freezing of gait (FOG) and falls in people with Parkinson's disease using Microsoft Kinect v2, with automated laser-based visual cueing and remote monitoring support.

This repository contains a cleaned, documented and consolidated archival release of the original Kinect4FOG research software package. The original historical repositories remain untouched.

## Associated publication

**Kinect4FOG: monitoring and improving mobility in people with Parkinson's using a novel system incorporating the Microsoft Kinect v2**

Published paper: https://doi.org/10.1080/17483107.2018.1467975

If you use this software in academic work, please cite the associated publication. GitHub citation metadata is provided in `CITATION.cff`.

## Package contents

The release contains the three deployable parts of the Kinect4FOG system:

1. **Main Kinect4FOG application** — Kinect v2 sensing, gait/FOG analysis, fall monitoring, laser-position calculations, research-data recording and network broadcasting.
2. **Arduino laser controller** — receives serial commands from the main application and drives the vertical/horizontal servo motors.
3. **Windows monitoring client** — receives the remote monitoring stream and can send commands back to the main application.

### Repository structure

```text
Kinect4FOG-Release/
├── Detection/                  # FOG/fall analysis and research UI
├── Hardware/                   # Main-app serial controller/configuration
├── Images/                     # Main-app runtime resources
├── Kinect/                     # Kinect v2 acquisition and joint processing
├── Networking/                 # Main-app UDP configuration
├── Properties/                 # Main-app .NET project properties
├── UI/                         # Supporting main-app WPF windows
├── Components/
│   ├── ArduinoController/      # Arduino firmware for the two-axis laser system
│   └── WindowsClient/          # Remote WPF monitoring client
├── Kinect4FOG.sln             # Main Kinect4FOG application
├── Kinect4FOG.csproj
├── BUILDING.md
├── CHANGELOG.md
├── CITATION.cff
├── COPYRIGHT.md
├── LICENSE
└── NOTICE
```

## System communication

### Main application → Arduino

Serial communication uses **9600 baud**.

- `1<angle>\n` — vertical servo.
- `2<angle>\n` — horizontal servo.

The release preserves the serial command format used by the original research implementation.

### Main application → Windows client

The main application broadcasts reduced camera frames as **JPEG data over UDP**. Configure **Tools → Broadcast** with the Windows client's IP address and UDP port **8082**.

### Windows client → main application

The Windows client sends commands to UDP port **8089** on the machine running the main Kinect4FOG application.

## Research behaviour

The release retains the research-critical values and calculations from the published implementation, including:

- **170°** knee-angle threshold used by the FOG logic;
- **200 ms** minimum stride duration;
- Kinect v2 joint-based gait analysis;
- vertical laser geometry and the original serial command protocol;
- the fall-monitoring thresholds retained from the historical application unless a correction is explicitly documented.

## Release clean-up

Compared with the historical development repositories, this release:

- excludes generated `bin/`, `obj/`, package binaries and Visual Studio metadata;
- corrects filenames, class names, method names, variable names and spelling;
- replaces opaque prototype identifiers with descriptive names;
- removes obsolete commented-out executable code;
- removes broken prototype-only functionality such as the unused Excel export path;
- improves network/socket and serial resource handling;
- uses a per-user data directory rather than a hard-coded `D:\` location;
- consolidates the Arduino and Windows client into the release package;
- corrects the Windows client to decode the JPEG frames actually emitted by the main application;
- retains Microsoft and other third-party notices where applicable;
- adds consistent authorship, licence, citation and publication metadata.

Documented correctness fixes are listed in `CHANGELOG.md`.

## Historical related repositories

Two other historical repositories were inspected during consolidation:

- **Kinect4FOGWebClient** contains only a solution stub and README in the available GitHub repository, with no usable web-client source to package.
- **FallDetectHeuristic** contains related Kinect v2 fall-detection research code from a separate experimental project. It is relevant to the research history but is not required to build or operate the Kinect4FOG package, so it is intentionally kept separate rather than represented as a runtime component of this release.

## Building and setup

See [BUILDING.md](BUILDING.md) for the main application, Arduino controller and Windows monitoring client.

Full hardware validation requires Microsoft Kinect v2 and, for visual cueing, the associated Arduino/servo hardware.

## Copyright and licence

Research implementation and modifications:

**Copyright (c) Amin Amini.**

The Kinect4FOG research implementation in this repository is licensed under the Apache License 2.0. See [LICENSE](LICENSE) and [NOTICE](NOTICE).

Associated publication:

https://doi.org/10.1080/17483107.2018.1467975

Some source files originate from or incorporate Microsoft Kinect SDK sample code. Original third-party copyright notices are retained where applicable. See [COPYRIGHT.md](COPYRIGHT.md) for details.
