# Kinect4FOG

Kinect4FOG is research software for detecting freezing of gait (FOG) and falls in people with Parkinson's disease using Microsoft Kinect v2, with automated laser-based visual cueing.

This repository contains a cleaned and documented archival release of the original research implementation. The refactoring improves source organisation, naming, documentation and repository hygiene while preserving the methodology and research-critical values used by the original system.

## Associated publication

**Kinect4FOG: monitoring and improving mobility in people with Parkinson's using a novel system incorporating the Microsoft Kinect v2**

Published paper: https://doi.org/10.1080/17483107.2018.1467975

If you use this software in academic work, please cite the associated publication. GitHub citation metadata is provided in `CITATION.cff`.

## What the system does

Kinect4FOG combines:

- Microsoft Kinect v2 RGB-D and body-joint tracking.
- Knee-angle and gait-cycle analysis for freezing-of-gait detection.
- Fall monitoring.
- Automated laser visual cue positioning.
- Serial communication with an Arduino/servo laser controller.
- UDP-based remote monitoring support.

The original research values, including the 170-degree knee-angle threshold and 200 ms minimum stride timing used by the FOG implementation, are retained.

## Repository structure

- `Detection/` — gait/FOG and fall-related detection UI and logic.
- `Kinect/` — Kinect v2 acquisition and body-joint processing.
- `Hardware/` — serial communication and serial-port configuration.
- `Networking/` — UDP broadcast configuration.
- `UI/` — supporting WPF windows.
- `Images/` — runtime resources used by the WPF application.
- `BUILDING.md` — build prerequisites and instructions.
- `CHANGELOG.md` — release clean-up history.

## Release clean-up

Compared with the historical development repository, this release:

- excludes generated `bin/`, `obj/`, package binaries and Visual Studio metadata;
- corrects filenames, class names, method names and spelling;
- replaces selected opaque identifiers with descriptive names;
- removes obsolete commented-out executable code;
- retains Microsoft or other third-party notices where applicable;
- adds consistent research attribution and the publication DOI;
- preserves the original research calculations unless a correction is explicitly documented.

## Building

See [BUILDING.md](BUILDING.md).

This is Windows research software requiring Microsoft Kinect v2 support and the Kinect for Windows SDK v2. Hardware-dependent features require the associated sensor and Arduino/servo setup for full runtime validation.

## Copyright and provenance

Research implementation and modifications:

**Copyright (c) Amin Amini.**

The Kinect4FOG research implementation in this repository is licensed under the Apache License 2.0. See [LICENSE](LICENSE) and [NOTICE](NOTICE).

Associated publication:

https://doi.org/10.1080/17483107.2018.1467975

Some source files originate from or incorporate Microsoft Kinect SDK sample code. Original third-party copyright notices are retained where applicable. See [COPYRIGHT.md](COPYRIGHT.md) for details.
