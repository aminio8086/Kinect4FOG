# Kinect4FOG Windows Monitoring Client

This WPF application is the remote-monitoring companion for the Kinect4FOG research system.

Copyright (c) Amin Amini. Licensed under Apache-2.0.

Associated publication: https://doi.org/10.1080/17483107.2018.1467975

## Network protocol

The historical Kinect4FOG system uses:

- UDP **8082** for monitoring data sent to the client.
- UDP **8089** for commands sent back to the main Kinect4FOG application.

The main application encodes reduced camera frames as JPEG before transmitting them. The cleaned client therefore decodes JPEG packets directly. Non-JPEG datagrams received on port 8082 are displayed as text monitoring data instead of being interpreted as image pixels.

## Using the client

1. Build and run `Kinect4FOGClient.sln`.
2. In the main Kinect4FOG application, open **Tools → Broadcast**.
3. Enter the IP address of the machine running this client.
4. Use UDP port **8082**.
5. The client will display incoming JPEG frames.
6. To send a command back to the main application, enter the main system's IP address and a command, then select **Send**.

The historical default command `11` is retained in the UI. The main Kinect4FOG application receives commands on UDP port 8089.

## Requirements

- Windows
- .NET Framework 4.6
- Visual Studio capable of building WPF/.NET Framework 4.6 projects

The monitoring client does **not** require the Kinect SDK because it receives already-encoded network data from the main application.

## Release clean-up

The release version:

- corrects the historical `Recive` spelling and opaque variable names;
- removes unused namespaces, generated settings/resources and commented prototype code;
- disposes network resources correctly;
- stops using `Thread.Abort`;
- decodes the actual JPEG payload produced by the Kinect4FOG main application;
- validates the target IP address;
- retains the original monitoring and command UDP ports;
- adds research attribution, DOI and Apache-2.0 metadata.
