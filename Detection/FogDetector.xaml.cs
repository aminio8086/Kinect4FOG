// Kinect4FOG research implementation and modifications
// Copyright (c) Amin Amini. All rights reserved.
// Author: Amin Amini
// Associated publication: https://doi.org/10.1080/17483107.2018.1467975
//
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Media3D;
using Excel = Microsoft.Office.Interop.Excel;
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

        private readonly SerialController _serialController = new SerialController();

        private Thread _threadExcel;
        private ThreadStart _startThreadExcel;

        //For head height calculator
        double distanceHeadCurrent = 0;
        double distanceHeadCurrentAbs = 0;
        //For real-time head height plot
        public Joint headJoint;
        private float _oldHeadHeight = 0;
        private Label[] _labelHeadHeight;
        private int _lableSecondCounter = 0;
        private int _lableXaxisIncrement = 0;
        private bool _lableOneTimeGenerator = false;

        private bool fallAlertWindowShown = false;

        //For recieving UDP commands from client
        private Thread _udpReceiveThread;
        private ThreadStart _udpReceiveThreadStart;
        private const int _udpListenPort = 8089;
        private UdpClient _udpListener = new UdpClient(_udpListenPort);
        private IPEndPoint _udpIP = new IPEndPoint(IPAddress.Any, _udpListenPort);
        private byte[] _udpReceiveBytes;



        //To create a text file for data collection
        string folderName = @"d:\kinect_data_recording";
        private int _txtRowNum = 0;
        private bool _rectxtdata = false;

        string path = @"d:\kinect_data_recording\" + DateTime.Now.ToString("(yyyy") + DateTime.Now.ToString("-MM")
            + DateTime.Now.ToString("-dd)") + DateTime.Now.ToString("_HH")
            + DateTime.Now.ToString("_mm")
            + DateTime.Now.ToString("_ss") + ".txt";
        StreamWriter sw;


        //For excel data recording
        private Excel.Application _excelInstance = new Excel.Application();
        private Excel.Workbook _excelworkbook;
        private Excel.Worksheet _excelWorksheet;
        private int _excelCol = 1;
        private int _excelRowNum = 1;

        //FOG Determination
        public Joint rightFootJoint;
        public Joint leftFootJoint;

        private int _leftFootstepcountUp = 0;
        private int _rightFootstepcountUp = 0;
        private int _leftFootstepcountTotal = 0;
        private int _rightFootstepcountTotal = 0;

            // Legacy foot-to-floor distance approach removed; knee-angle detection is used instead.


        }

        //#############################################################################################################################//

        /// <summary>
        /// Show gait information on the GUI
        /// </summary>
        public void UpdateGaitInformation()
        {


            TB_sensorHeight.Text = D_floor.ToString();                    //Show the Kinect height to the ground

            TB_leftAngle.Text = _angleKneeLeft.ToString();                //Show the left foot distance to the ground
            TB_leftStepinTime.Text = _leftFootstepcountUp.ToString();     //Show the left foot steps before reset
            TB_leftNumStep.Text = _leftFootstepcountTotal.ToString();     //Show the  left foot steps in total

            TB_rightAngle.Text = _angleKneeRight.ToString();              //Show the left foot distance to the ground
            TB_rightStepinTime.Text = _rightFootstepcountUp.ToString();   //Show the left foot steps before reset
            TB_rightNumStep.Text = _rightFootstepcountTotal.ToString();   //Show the  left foot steps in total

            //Show stride duration for both feet
            TB_leftStride.Text = _leftFootStrideDuration.ToString();
            TB_rightStride.Text = _rightFootStrideDuration.ToString();


            plotter();


            TB_udp.Text = distanceHeadCurrentAbs.ToString();
            //#############################################################################################################################//  

            //Write data to an Excel sheet
            if (_checkBoxExcel.IsChecked == true)
            {

                _startThreadExcel = new ThreadStart(excelwriter);
                _threadExcel = new Thread(_startThreadExcel) { IsBackground = true };
                _threadExcel.Start();
                _checkBoxExcel.IsChecked = false;

            }



        }





        public void CSVwriter()
        {


            if (_rectxtdata == true)
            {

                if (!File.Exists(path))
                {
                    // Create a file to write to.
                    sw = new StreamWriter(path);

                }

                if (_txtRowNum == 0)
                {
                    sw.WriteLine(_txtRowNum + " "
                    + "Head" + " "
                    + "Neck" + " "
                    + "Spine_Shoulder" + " "
                    + "R_Shoulder" + " "
                    + "L_Shoulder" + " "
                    + "Spine_Mid" + " "
                    + "Spine_Base" + " "
                    + "R_Hip" + " "
                    + "L_Hip" + " "
                    + "R_Knee" + " "
                    + "L_Knee" + " "
                    + "R_Ankle" + " "
                    + "L_Ankle" + " "
                    + "R_Foot" + " "
                    + "L_Foot" + " "

                   // + originalJointDistance[0].ToString() + " "

                   + "Distance_to_Cam");
                    _txtRowNum++;
                }

                else
                {
                    sw.WriteLine(_txtRowNum + " "

                    + _angleKneeRight.ToString() + " "
                    + _angleKneeLeft.ToString()  + " "
                    + "*" + " "
                    + rightFootJoint.Position.X.ToString() + " "
                    + rightFootJoint.Position.Y.ToString() + " "
                    + rightFootJoint.Position.Z.ToString() + " "
                    + "*" + " "
                    + leftFootJoint.Position.X.ToString() + " "
                    + leftFootJoint.Position.Y.ToString() + " "
                    + leftFootJoint.Position.Z.ToString() + " "
                    + "*" + " "
                    + _rightProjectionAngleDegrees.ToString() + " "
                    + _leftProjectionAngleDegrees.ToString() + " "
                    + "*" + " "
                    + (Math.Abs(90 + _resultShoulderTurnRight)).ToString() + " "
                    + (Math.Abs(90 - _resultShoulderTurnLeft)).ToString() + " "




           );
                    _txtRowNum++;
                }

            }



        }





        private void excelwriter()
        {
            

         
            while (true)
            {
                _excelWorksheet.Cells[_excelCol, 1] = _excelRowNum;
                //_excelWorksheet.Cells[_excelCol, 2] = _leftFootStrideDuration.ToString();
                //_excelWorksheet.Cells[_excelCol, 3] = _rightFootStrideDuration.ToString();
                
                _excelWorksheet.Cells[_excelCol, 2] = "*";
                _excelWorksheet.Cells[_excelCol, 3] = _angleKneeRight.ToString();
                _excelWorksheet.Cells[_excelCol, 4] = _angleKneeLeft.ToString();
                _excelWorksheet.Cells[_excelCol, 5] = "*";
                _excelWorksheet.Cells[_excelCol, 6] = rightFootJoint.Position.X.ToString();
                _excelWorksheet.Cells[_excelCol, 7] = rightFootJoint.Position.Y.ToString();
                _excelWorksheet.Cells[_excelCol, 8] = rightFootJoint.Position.Z.ToString();
                _excelWorksheet.Cells[_excelCol, 9] = "*";
                _excelWorksheet.Cells[_excelCol, 10] = leftFootJoint.Position.X.ToString();
                _excelWorksheet.Cells[_excelCol, 11] = leftFootJoint.Position.Y.ToString();
                _excelWorksheet.Cells[_excelCol, 12] = leftFootJoint.Position.Z.ToString();
                _excelWorksheet.Cells[_excelCol, 13] = "*";
                _excelWorksheet.Cells[_excelCol, 14] = _rightProjectionAngleDegrees.ToString();
                _excelWorksheet.Cells[_excelCol, 15] = _leftProjectionAngleDegrees.ToString();
                _excelWorksheet.Cells[_excelCol, 16] = "*";
                _excelWorksheet.Cells[_excelCol, 17] = (Math.Abs(90 + _resultShoulderTurnRight)).ToString();
                _excelWorksheet.Cells[_excelCol, 18] = (Math.Abs(90 - _resultShoulderTurnLeft)).ToString();


                _excelCol++;
                _excelRowNum++;
                Thread.Sleep(1);
                        
            }
   

        }
        //#############################################################################################################################//
        /// <summary>
        /// Determines the upper body orientation in degrees (facing camera = 0)
        /// for the use of the horizontal motor
        /// </summary>
        public void BodyRotate()
        {
            float leftShoulder = shoulderLeftJoint.Position.Z;
            float rightShoulder = shoulderRightJoint.Position.Z;

            //Turning right
            if (leftShoulder < rightShoulder)
            {
                _resultShoulderTurnRight = Math.Abs(rightShoulder - leftShoulder);
                _resultShoulderTurnRight = Math.Asin(_resultShoulderTurnRight);
                _resultShoulderTurnRight *= 1000;

                _resultShoulderTurnRight = Math.Ceiling(_resultShoulderTurnRight);

                }

            //Turning left
            else
            {
                _resultShoulderTurnLeft = Math.Abs(leftShoulder - rightShoulder);
                _resultShoulderTurnLeft = Math.Asin(_resultShoulderTurnLeft);
                _resultShoulderTurnLeft *= 1000;

                _resultShoulderTurnLeft = Math.Ceiling(_resultShoulderTurnLeft);
                
                }
        }

        //#############################################################################################################################//       
        /// <summary>
        /// Detect the Freezing of Gait based on the number of steps
        /// Features stride duration
        /// </summary>
        public void DetectFreezingOfGait()
        {

            // Legacy foot-to-floor distance approach removed; knee-angle detection is used instead.



            Vector3D hipLeftVect = new Vector3D(hipLeftJoint.Position.X, hipLeftJoint.Position.Y, hipLeftJoint.Position.Z);
            Vector3D kneeLeftVect = new Vector3D(kneeLeftJoint.Position.X, kneeLeftJoint.Position.Y, kneeLeftJoint.Position.Z);
            Vector3D AnkleLeftVect = new Vector3D(ankleLeftJoint.Position.X, ankleLeftJoint.Position.Y, ankleLeftJoint.Position.Z);
            _angleKneeLeft = AngleBetweenTwoVectors(kneeLeftVect - hipLeftVect, kneeLeftVect - AnkleLeftVect);

           // decimal AngleKneeLeftRound = Math.Round((decimal)_angleKneeLeft, 3);


            Vector3D hipRightVect = new Vector3D(hipRightJoint.Position.X, hipRightJoint.Position.Y, hipRightJoint.Position.Z);
            Vector3D kneeRightVect = new Vector3D(kneeRightJoint.Position.X, kneeRightJoint.Position.Y, kneeRightJoint.Position.Z);
            Vector3D AnkleRightVect = new Vector3D(ankleRightJoint.Position.X, ankleRightJoint.Position.Y, ankleRightJoint.Position.Z);
            _angleKneeRight = AngleBetweenTwoVectors(kneeRightVect - hipRightVect, kneeRightVect - AnkleRightVect);

         //   decimal AngleKneeRightRound = Math.Round((decimal)_angleKneeRight, 3);

            //#############################################################################################################################//

            //Check if FOG has happened
            if (_angleKneeLeft >= KneeAngleThresholdDegrees || _angleKneeRight >= KneeAngleThresholdDegrees)
            {
            }

            //#############################################################################################################################//

   


            //Determine the number of left foot steps in total and given time
            if ((_angleKneeLeft <= KneeAngleThresholdDegrees))
            {
                stopwatchLeft.Start();

            }
              else{

                    stopwatchLeft.Stop();


                    _leftFootStrideDuration = stopwatchLeft.Elapsed.Milliseconds;
                if ((_leftFootStrideDuration > MinimumStrideDurationMilliseconds))
                {
                    _leftFootstepcountTotal++;
                    _leftFootstepcountUp++;

                }
               
                stopwatchLeft.Reset();
                    
                }


            //Determine the number of left foot steps in total and given time
            if ((_angleKneeRight <= KneeAngleThresholdDegrees))
            {
                stopwatchRight.Start();

            }
            else {

                stopwatchRight.Stop();


                _rightFootStrideDuration = stopwatchRight.Elapsed.Milliseconds;

                if ((_rightFootStrideDuration > MinimumStrideDurationMilliseconds))
                {
                    _rightFootstepcountTotal++;
                    _rightFootstepcountUp++;

                }

                stopwatchRight.Reset();

            }




            //#############################################################################################################################//



            //#############################################################################################################################//            
           
        }
        //#############################################################################################################################//

        /// <summary>
        /// Checking the number of time within 2 secs the left foot has been touched down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FOGChecker_Tick(System.Object sender, EventArgs e)
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
                resetFOGIndicator.Start();
            }
        }




        private void resetFOGIndicator_Tick(System.Object sender, EventArgs e)
        {
            if (_lableFOG.Visibility == System.Windows.Visibility.Visible)
            {
                _lableFOG.Visibility = System.Windows.Visibility.Visible;
                resetFOGIndicator.Stop();
               
            }
            
        }

        /// <summary>
        /// Checking the number of time within 2 secs the left foot has been lifted up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 


        private void FOGHappened_Tick(System.Object sender, EventArgs e)
        {

            if ((_leftFootstepcountUp > 2)&& (_rightFootstepcountUp > 2))
            {

                _lableFOG.Visibility = System.Windows.Visibility.Visible;
                _leftFootstepcountUp = 0;
                _rightFootstepcountUp = 0;
                resetFOGIndicator.Start();
                FOGHappened.Stop();

            }

            else
            {
                FOGHappened.Stop();
                _leftFootstepcountUp = 0;
                _rightFootstepcountUp = 0;

            }
        }
        //#############################################################################################################################//
        /// <summary>
        /// Determines knees angle for step calculation
        /// </summary>
        /// <param name="vectorA"></param>
        /// <param name="vectorB"></param>
        /// <returns></returns>
        public double AngleBetweenTwoVectors(Vector3D vectorA, Vector3D vectorB)
        {
            double dotProduct = 0.0;
            vectorA.Normalize();
            vectorB.Normalize();

            dotProduct = Vector3D.DotProduct(vectorA, vectorB);

            return (double)Math.Acos(dotProduct) / Math.PI * 180;
        }


        //#############################################################################################################################//
        /// <summary>
        /// Send signal to the micro-controller for the vertical laser movement
        /// </summary>
        public void UpdateVerticalLaserPosition()
        {

                double a = 136d;

                double c = leftFootJoint.Position.Z * 100;           //the distance from a body joint to the pointer
                double c2 = rightFootJoint.Position.Z * 100;

                double Beta = (a / c);    //hypotenuse
                double Beta2 = (a / c2);    //hypotenuse

                Beta = Math.Acos(Beta);
                Beta *= (180f / Math.PI);

                Beta2 = Math.Acos(Beta2);
                Beta2 *= (180f / Math.PI);

                _leftProjectionAngleDegrees = Beta;
                _leftProjectionAngleDegrees = Math.Ceiling(_leftProjectionAngleDegrees);
                _rightProjectionAngleDegrees = Beta2;
                _rightProjectionAngleDegrees = Math.Ceiling(_rightProjectionAngleDegrees);

                if (SerialCheckbox.IsChecked == true)
                {
                    if (_leftProjectionAngleDegrees <= 24)
                    {

                        _leftProjectionAngleDegrees = 24;
                    }

                    if (_leftProjectionAngleDegrees >= 80)
                    {

                        _leftProjectionAngleDegrees = 80;
                    }

                    if (_rightProjectionAngleDegrees <= 24)
                    {

                        _rightProjectionAngleDegrees = 24;
                    }

                    if (_rightProjectionAngleDegrees >= 80)
                    {

                        _rightProjectionAngleDegrees = 80;
                    }

                    if (_leftProjectionAngleDegrees < _rightProjectionAngleDegrees)
                    {

                        _serialController.SendServoCommand(("1" + (_leftProjectionAngleDegrees-3)).ToString());
                        TB_laser.Text = _leftProjectionAngleDegrees.ToString();
                    }
                    else
                    {
                        _serialController.SendServoCommand(("1" + (_rightProjectionAngleDegrees-3)).ToString());
                        TB_laser.Text = _rightProjectionAngleDegrees.ToString();
                    }



                }



            

        


        }

        //#############################################################################################################################//
        /// <summary>
        /// Send signal in intervals to the micro-controller for the horizontal laser movement
        /// </summary>
        private void HorizontalMotorRotation_Tick(System.Object sender, EventArgs e)
        {

            if ((Math.Abs(_resultShoulderTurnLeft - _resultShoulderTurnRight) > 20) || (Math.Abs(_resultShoulderTurnRight - _resultShoulderTurnLeft) > 20))
            {

                if (_resultShoulderTurnLeft > _resultShoulderTurnRight)
                {
                    if (Math.Abs(90 - _resultShoulderTurnLeft) >= 40 && Math.Abs(90 - _resultShoulderTurnLeft) <= 70)
                    {
                    }
                }
                if (_resultShoulderTurnRight > _resultShoulderTurnLeft)
                {
                    if (Math.Abs(90 + _resultShoulderTurnRight) >= 120 && Math.Abs(90 + _resultShoulderTurnRight) <= 140)
                    {
                    }
                }
                else
                {

                }
                TB_temp1.Text = (Math.Abs(90 + _resultShoulderTurnRight)).ToString();
                TB_temp2.Text = (Math.Abs(90 - _resultShoulderTurnLeft)).ToString();
            }
        }

        //#############################################################################################################################//
        /// <summary>
        /// Recieve signal from the client app for commands
        /// </summary>
        private void ReceiveUdpCommands()
        {
            while (_udpListener.Receive(ref _udpIP) != null)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    _udpReceiveBytes = _udpListener.Receive(ref _udpIP);

                    char[] chars = new char[_udpReceiveBytes.Length / sizeof(char)];
                    System.Buffer.BlockCopy(_udpReceiveBytes, 0, chars, 0, _udpReceiveBytes.Length);
                    string str = new string(chars);

                    TB_udp.Text = str;

                }));
                Thread.Sleep(1);
                if (_udpListener.Receive(ref _udpIP) == null)
                {
                    _udpReceiveThread.Abort();
                }
            }
        }
    }
}