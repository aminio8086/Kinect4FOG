# Kinect4FOG components

This folder contains companion software that was historically maintained in separate repositories.

## ArduinoController

Firmware for the Arduino-based two-axis laser visual-cue controller.

See [ArduinoController/README.md](ArduinoController/README.md).

## WindowsClient

WPF remote-monitoring client for receiving Kinect4FOG JPEG broadcasts over UDP and sending commands back to the main system.

See [WindowsClient/README.md](WindowsClient/README.md).

## Runtime relationship

```text
Microsoft Kinect v2
       |
       v
Main Kinect4FOG application
       |
       +---- Serial 9600 baud ----> ArduinoController ----> vertical/horizontal servos
       |
       +---- UDP JPEG :8082 ------> WindowsClient
       ^
       |
       +---- UDP commands :8089 --- WindowsClient
```

The main application remains at the repository root so the historical Visual Studio project can be preserved without unnecessarily restructuring its build paths.
