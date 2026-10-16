// Kinect4FOG research implementation and modifications
// Copyright (c) Amin Amini. All rights reserved.
// Author: Amin Amini
// Associated publication: https://doi.org/10.1080/17483107.2018.1467975
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace Parkinson.Detection
{
    public class SerialController
    {
       
        private readonly SerialPort _serialPort = new SerialPort("COM4", 9600);
      //  public SerialPort port = 
        public void ConfigurePort(string portName)
        {
            
        }

        public void SendServoCommand(string command)
        {

            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();

            }

            if (_serialPort.IsOpen)
            {

           //     _serialPort.Open();
                _serialPort.WriteLine(command);

            }
       //    _serialPort.Close();
        }
    }
}
