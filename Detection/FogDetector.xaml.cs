// Kinect4FOG research implementation and modifications
// Copyright (c) Amin Amini.
// SPDX-License-Identifier: Apache-2.0
// Author: Amin Amini
// Associated publication: https://doi.org/10.1080/17483107.2018.1467975
//
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Media3D;
using Microsoft.Kinect;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Windows.Controls;
using Parkinson.Detection.Detectors;
using System.IO;
namespace Parkinson.Detection
{
    /// <summary>
    /// Interaction logic for DataWindow.xaml
    /// </summary>
    public partial class FogDetector : Window
    {
        double _leftProjectionAngleDegrees = 0;
        double _rightProjectionAngleDegrees = 0;

        private readonly SerialController _serialController;
//For head height calculator
        double distanceHeadCurrent = 0;
        double distanceHeadCurrentAbs = 0;
        //For real-time head height plot
        public Joint headJoint;
        private float _oldHeadHeight = 0;
        private Label[] _labelHeadHeight;
        private int _labelSecondCounter = 0;
        private int _labelXAxisIncrement = 0;
        private bool _labelsInitialised = false;

        private bool fallAlertWindowShown = false;

        //For receiving UDP commands from client
        private Thread _udpReceiveThread;
        private const int UdpCommandPort = 8089;
        private UdpClient _udpListener = new UdpClient(UdpCommandPort);
        private IPEndPoint _udpIP = new IPEndPoint(IPAddress.Any, UdpCommandPort);
        private byte[] _udpReceiveBytes;



        // Research data is written to a per-user Documents folder.
        private readonly string _recordingDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Kinect4FOGData");
        private string _recordingFilePath;
        private int _textRowNumber;
        private bool _recordTextData;
        private StreamWriter _recordingWriter;
//FOG Determination
        public Joint rightFootJoint;
        public Joint leftFootJoint;

        private int _leftFootstepcountUp = 0;
        private int _rightFootstepcountUp = 0;
        private int _leftFootstepcountTotal = 0;
        private int _rightFootstepcountTotal = 0;
            // Knee-angle detection is used instead of the abandoned foot-to-floor distance experiment.

Vector3D hipLeftVector = new Vector3D(hipLeftJoint.Position.X, hipLeftJoint.Position.Y, hipLeftJoint.Position.Z);
            Vector3D kneeLeftVector = new Vector3D(kneeLeftJoint.Position.X, kneeLeftJoint.Position.Y, kneeLeftJoint.Position.Z);
            Vector3D ankleLeftVector = new Vector3D(ankleLeftJoint.Position.X, ankleLeftJoint.Position.Y, ankleLeftJoint.Position.Z);
            _angleKneeLeft = CalculateAngleDegrees(kneeLeftVector - hipLeftVector, kneeLeftVector - ankleLeftVector);


            Vector3D hipRightVector = new Vector3D(hipRightJoint.Position.X, hipRightJoint.Position.Y, hipRightJoint.Position.Z);
            Vector3D kneeRightVector = new Vector3D(kneeRightJoint.Position.X, kneeRightJoint.Position.Y, kneeRightJoint.Position.Z);
            Vector3D ankleRightVector = new Vector3D(ankleRightJoint.Position.X, ankleRightJoint.Position.Y, ankleRightJoint.Position.Z);
            _angleKneeRight = CalculateAngleDegrees(kneeRightVector - hipRightVector, kneeRightVector - ankleRightVector);
//Determine the number of left foot steps in total and given time
            if ((_angleKneeLeft <= KneeAngleThresholdDegrees))
            {
                _leftStrideStopwatch.Start();

            }
              else{

                    _leftStrideStopwatch.Stop();


                    _leftFootStrideDuration = _leftStrideStopwatch.Elapsed.Milliseconds;
                if ((_leftFootStrideDuration > MinimumStrideDurationMilliseconds))
                {
                    _leftFootstepcountTotal++;
                    _leftFootstepcountUp++;

                }
               
                _leftStrideStopwatch.Reset();
                    
                }


            //Determine the number of left foot steps in total and given time
            if ((_angleKneeRight <= KneeAngleThresholdDegrees))
            {
                _rightStrideStopwatch.Start();

            }
            else {

                _rightStrideStopwatch.Stop();


                _rightFootStrideDuration = _rightStrideStopwatch.Elapsed.Milliseconds;

                if ((_rightFootStrideDuration > MinimumStrideDurationMilliseconds))
                {
                    _rightFootstepcountTotal++;
                    _rightFootstepcountUp++;

                }

                _rightStrideStopwatch.Reset();

            }
        }
        /// <summary>
        /// Checking the number of time within 2 secs the left foot has been touched down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _fogCheckTimer_Tick(System.Object sender, EventArgs e)
        {
            if ((_leftFootstepcountUp <= 2)&& (_rightFootstepcountUp <= 2))
            {
                _leftFootstepcountUp = 0;
                _rightFootstepcountUp = 0;
            }

            else
            {
                fogOccurred = true;
                _lableFOG.Visibility = System.Windows.Visibility.Visible;
                _leftFootstepcountUp = 0;
                _rightFootstepcountUp = 0;
                _fogResetTimer.Start();
            }
        }




        private void _fogResetTimer_Tick(System.Object sender, EventArgs e)
        {
            if (_lableFOG.Visibility == System.Windows.Visibility.Visible)
            {
                _lableFOG.Visibility = System.Windows.Visibility.Hidden;
                _fogResetTimer.Stop();
               
            }
            
        }

        /// <summary>
        /// Checking the number of time within 2 secs the left foot has been lifted up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        ///
/// <summary>
        /// Determines knees angle for step calculation
        /// </summary>
        /// <param name="vectorA"></param>
        /// <param name="vectorB"></param>
        /// <returns></returns>
        public double CalculateAngleDegrees(Vector3D vectorA, Vector3D vectorB)
        {
            double dotProduct = 0.0;
            vectorA.Normalize();
            vectorB.Normalize();

            dotProduct = Vector3D.DotProduct(vectorA, vectorB);

            return (double)Math.Acos(dotProduct) / Math.PI * 180;
        }
        /// <summary>
        /// Send signal to the micro-controller for the vertical laser movement
        /// </summary>
        /// <summary>
        /// Calculates the vertical laser projection angle from the tracked ankle depth
        /// and sends the corresponding command to the Arduino controller.
        /// </summary>
        public void UpdateVerticalLaserPosition()
        {
            const double laserHeightCentimetres = 136.0;
            const double minimumServoAngleDegrees = 24.0;
            const double maximumServoAngleDegrees = 80.0;
            const double calibrationOffsetDegrees = 3.0;

            double leftFootDepthCentimetres = leftFootJoint.Position.Z * 100.0;
            double rightFootDepthCentimetres = rightFootJoint.Position.Z * 100.0;

            double leftAngleRadians = Math.Acos(laserHeightCentimetres / leftFootDepthCentimetres);
            double rightAngleRadians = Math.Acos(laserHeightCentimetres / rightFootDepthCentimetres);

            _leftProjectionAngleDegrees = Math.Ceiling(leftAngleRadians * (180.0 / Math.PI));
            _rightProjectionAngleDegrees = Math.Ceiling(rightAngleRadians * (180.0 / Math.PI));

            _leftProjectionAngleDegrees = LimitServoAngle(
                _leftProjectionAngleDegrees,
                minimumServoAngleDegrees,
                maximumServoAngleDegrees);

            _rightProjectionAngleDegrees = LimitServoAngle(
                _rightProjectionAngleDegrees,
                minimumServoAngleDegrees,
                maximumServoAngleDegrees);

            if (SerialCheckbox.IsChecked == true)
            {
                double selectedAngleDegrees = Math.Min(
                    _leftProjectionAngleDegrees,
                    _rightProjectionAngleDegrees);

                double commandAngleDegrees = selectedAngleDegrees - calibrationOffsetDegrees;
                _serialController.SendServoCommand(
                    "1" + commandAngleDegrees.ToString(System.Globalization.CultureInfo.InvariantCulture));

                TB_laser.Text = selectedAngleDegrees.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private static double LimitServoAngle(double angleDegrees, double minimumDegrees, double maximumDegrees)
        {
            if (angleDegrees < minimumDegrees)
            {
                return minimumDegrees;
            }

            if (angleDegrees > maximumDegrees)
            {
                return maximumDegrees;
            }

            return angleDegrees;
        }

        /// <summary>
        /// Updates the body-orientation diagnostics used by the historical horizontal
        /// laser-control experiment. Horizontal servo write statements were disabled
        /// in the recovered source and are intentionally not reactivated here.
        /// </summary>
        private void _horizontalMotorTimer_Tick(System.Object sender, EventArgs e)
        {
            if (Math.Abs(_resultShoulderTurnLeft - _resultShoulderTurnRight) <= 20)
            {
                return;
            }

            TB_temp1.Text = Math.Abs(90 + _resultShoulderTurnRight).ToString();
            TB_temp2.Text = Math.Abs(90 - _resultShoulderTurnLeft).ToString();
        }

        private void ReceiveUdpCommands()
        {
            while (true)
            {
                try
                {
                    IPEndPoint remoteEndpoint = _udpIP;
                    _udpReceiveBytes = _udpListener.Receive(ref remoteEndpoint);
                    string command = System.Text.Encoding.ASCII.GetString(_udpReceiveBytes);

                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        TB_udp.Text = command;
                    }));
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    return;
                }
            }
        }
    }
}
