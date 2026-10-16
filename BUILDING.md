# Building Kinect4FOG

Kinect4FOG is historical Windows research software built around Microsoft Kinect v2 and WPF.

## Requirements

- Windows with Microsoft Kinect v2 support.
- Visual Studio capable of building the .NET Framework target used by the project.
- Microsoft Kinect for Windows SDK v2, including the Microsoft.Kinect assemblies.
- NuGet package restore for dependencies listed in `packages.config`.
- Arduino/servo hardware is required only for physical laser-cue control.

## Build procedure

1. Install the Kinect v2 SDK and Visual Studio prerequisites.
2. Open `Kinect4FOG.sln`.
3. Restore NuGet packages.
4. Build the solution.

The clean-up preserves the original research behaviour. Hardware-dependent runtime paths should be validated with the Kinect v2 sensor and associated Arduino controller before a formal release is tagged.
