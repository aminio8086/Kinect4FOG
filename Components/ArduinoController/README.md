# Kinect4FOG Arduino Controller

This component controls the two servo motors used by the Kinect4FOG laser visual-cue system.

Copyright (c) Amin Amini. Licensed under Apache-2.0.

Associated publication: https://doi.org/10.1080/17483107.2018.1467975

## Hardware

The historical implementation uses:

- an Arduino-compatible microcontroller;
- vertical servo on digital pin 11;
- horizontal servo on digital pin 12;
- 16×2 I²C LCD at address `0x27`;
- 9600-baud serial communication.

## Serial protocol

The Windows application sends newline-terminated decimal commands.

- `1<angle>\n` controls the vertical servo.
- `2<angle>\n` controls the horizontal servo.

For example, `145\n` requests a vertical angle of 45 degrees and `290\n` requests a horizontal angle of 90 degrees.

The protocol is retained from the original research implementation so this controller remains compatible with the main Kinect4FOG application.

## Arduino libraries

The sketch uses:

- `Wire`
- `LCD`
- `LiquidCrystal_I2C`
- `Servo`

The LCD constructor is retained from the original implementation, so use a compatible `LiquidCrystal_I2C`/LCD library implementation.

## Release clean-up

The release version:

- preserves the original serial protocol and pin assignments;
- corrects the historical `Horizental` spelling;
- replaces opaque global variables with named constants and functions;
- removes commented-out prototype code;
- documents the command protocol and research provenance.
