# Kinect4FOG

Kinect4FOG is research software for detecting freezing of gait (FOG) and falls in people with Parkinson's disease using Microsoft Kinect v2, with automated laser-based visual cueing.

This repository is a cleaned, documented archival release of the original Kinect4FOG research implementation. The clean-up is intended to improve readability, maintainability, attribution and reproducibility while preserving the behaviour and methodological thresholds of the published research.

## Associated publication

Amini, A., Banitsas, K. and Young, W. R. (2019). **Kinect4FOG: monitoring and improving mobility in people with Parkinson's using a novel system incorporating the Microsoft Kinect v2.** *Disability and Rehabilitation: Assistive Technology*, 14(6), 566–573.

**Published paper:** https://doi.org/10.1080/17483107.2018.1467975

## Research software

The system combines:

- Microsoft Kinect v2 RGB-D and body-joint tracking.
- Knee-angle and gait-cycle analysis for freezing-of-gait detection.
- Fall monitoring.
- Automated laser visual cue positioning.
- Serial communication with the associated Arduino/servo controller.
- UDP-based remote monitoring support.

The research-critical thresholds and calculations from the original implementation are preserved unless a separately documented correction is required.

## Author and copyright

Research implementation and modifications: **Amin Amini**.

Copyright (c) Amin Amini. All rights reserved.

Some source files are derived from or incorporate Microsoft Kinect SDK sample code. Original third-party notices are retained where applicable; the authorship statement above applies to the Kinect4FOG research implementation and modifications.

## Release status

This repository is being prepared as the maintained release version of the historical Kinect4FOG research software. See the associated publication above for the scientific methodology.
