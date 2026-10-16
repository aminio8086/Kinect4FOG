// Kinect4FOG research implementation and modifications
// Copyright (c) Amin Amini.
// SPDX-License-Identifier: Apache-2.0
// Author: Amin Amini
// Associated publication: https://doi.org/10.1080/17483107.2018.1467975
//
using System;
using System.IO.Ports;
namespace Parkinson.Detection
{
    public class SerialController
    {
       
        private readonly SerialPort _serialPort = new SerialPort("COM4", 9600);
public void ConfigurePort(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentException("A serial port name is required.", nameof(portName));
            }

            if (_serialPort.IsOpen)
            {
                throw new InvalidOperationException("The serial port cannot be changed while it is open.");
            }

            _serialPort.PortName = portName;
        }

        public void SendServoCommand(string command)
        {

            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();

            }

            if (_serialPort.IsOpen)
            {
_serialPort.WriteLine(command);

            }
}
    }
}
