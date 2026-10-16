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

        //DISABLEED FOR LENSE DISTORTION ISSUE
        private int _leftFootStrideDuration = 0;
        private int _rightFootStrideDuration = 0;

        private const double KneeAngleThresholdDegrees = 170.0;
        private const int MaximumStrideDurationMilliseconds = 900;
        private const int MinimumStrideDurationMilliseconds = 200;

        //Spine angle detection
        public Joint spineMidJoint;
        public Joint kneeLeftJoint;
        public Joint kneeRightJoint;
        public Joint spineBaseJoint;


        // Body orientation detection
        public Joint shoulderRightJoint;
        public Joint shoulderLeftJoint;

        private double _resultShoulderTurnRight = 0;
        private double _resultShoulderTurnLeft = 0;


        //Knee angle detection
        public Joint hipLeftJoint;
        public Joint hipRightJoint;
        public Joint ankleLeftJoint;
        public Joint ankleRightJoint;

        private double _angleKneeLeft = 0;
        private double _angleKneeRight = 0;


        //FloorPlane Detection
        public float A_floor, B_floor, C_floor, D_floor = 0;


        //To apply delay for the horizontal motor
        private dynamic HorizontalMotorRotation = new System.Windows.Threading.DispatcherTimer();

        private dynamic FOGChecker = new System.Windows.Threading.DispatcherTimer();
        private dynamic FOGHappened = new System.Windows.Threading.DispatcherTimer();
        private dynamic resetFOGIndicator = new System.Windows.Threading.DispatcherTimer();
        private dynamic SystemTimer = new System.Windows.Threading.DispatcherTimer();
        private dynamic LeftStrideTimer = new System.Windows.Threading.DispatcherTimer();
        private dynamic RightStrideTimer = new System.Windows.Threading.DispatcherTimer();

        private dynamic grapherTimer = new System.Windows.Threading.DispatcherTimer();

        private Stopwatch stopwatchLeft = new Stopwatch();
        private Stopwatch stopwatchRight = new Stopwatch();

        private Stopwatch fallover = new Stopwatch();


        private bool fogOccurred = false;
        private void FogDetector_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
        public FogDetector()
            : this(new SerialController())
        {
        }

        public FogDetector(SerialController serialController)
        {
            if (serialController == null)
            {
                throw new ArgumentNullException("serialController");
            }

            _serialController = serialController;
            InitializeComponent();
            Directory.CreateDirectory(_recordingDirectory);
            _recordingFilePath = Path.Combine(
                _recordingDirectory,
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt");
            _labelHeadHeight = new Label[12];

            _udpReceiveThread = new Thread(ReceiveUdpCommands) { IsBackground = true };
            _udpReceiveThread.Name = "Kinect4FOG UDP Command Receiver";
            _udpReceiveThread.Start();

            HorizontalMotorRotation.Interval = TimeSpan.FromMilliseconds(150);
            HorizontalMotorRotation.Tick += new EventHandler(HorizontalMotorRotation_Tick);
            HorizontalMotorRotation.Start();

            FOGChecker.Interval = TimeSpan.FromMilliseconds(2000);
            FOGChecker.Tick += new EventHandler(FOGChecker_Tick);
            FOGChecker.Start();

            FOGHappened.Interval = TimeSpan.FromMilliseconds(1800);
            FOGHappened.Tick += new EventHandler(FOGHappened_Tick);

            resetFOGIndicator.Interval = TimeSpan.FromSeconds(2);
            resetFOGIndicator.Tick += new EventHandler(resetFOGIndicator_Tick);

            grapherTimer.Interval = TimeSpan.FromSeconds(1);
            grapherTimer.Tick += new EventHandler(grapherTimer_Tick);
            grapherTimer.Start();
        }


        public void UpdateHeadHeightAndFallState()
        {

            distanceHeadCurrent = (((headJoint.Position.X * A_floor) + (headJoint.Position.Y * B_floor) + (headJoint.Position.Z * C_floor) + D_floor) / Math.Sqrt((A_floor * A_floor) + (B_floor * B_floor) + (C_floor * C_floor)));

            distanceHeadCurrentAbs = Math.Abs(distanceHeadCurrent);


            decimal HeaddecimalValue = Math.Round((decimal)distanceHeadCurrent, 3);

            distanceHeadCurrent = (float)HeaddecimalValue;

            if (FallCheckbox.IsChecked == true)
            {
                if (distanceHeadCurrent <= 0.3)
                {
                    fallover.Start();
                    _imageStanding.Visibility = Visibility.Hidden;
                    _imageFalling.Visibility = Visibility.Visible;

                    if (fallAlertWindowShown == false)
                    {
                        FallAlertWindow fallAlertWindow = new FallAlertWindow();
                        fallAlertWindow.Show();
                        fallAlertWindowShown = true;
                    }

                }
                if (fallover.IsRunning && fallover.ElapsedMilliseconds >= 1000 && distanceHeadCurrent >= 0.5)
                {
                    _imageStanding.Visibility = Visibility.Visible;
                    _imageFalling.Visibility = Visibility.Hidden;
                    fallover.Stop();
                    fallover.Reset();
                    fallAlertWindowShown = false;
                    FallCheckbox.IsChecked = false;
                }



            }

        }
        private void grapherTimer_Tick(System.Object sender, EventArgs e)
        {
            _oldHeadHeight = headJoint.Position.Y * 100;
            _labelSecondCounter++;

        }

        public void UpdateHeadHeightPlot()
        {

            int labelLocation = 675;


            if (_labelsInitialised == false)
            {
                for (int i = 0; i <= 11; i++)
                {

                    _labelHeadHeight[i] = new Label();
                    _labelHeadHeight[i].Name = "_labelHeight" + i.ToString();
                    _labelHeadHeight[i].Margin = new Thickness(labelLocation, 262, 0, 0);

                    _labelHeadHeight[i].Content = i.ToString();
                    grid.Children.Add(_labelHeadHeight[i]);

                    labelLocation = labelLocation + 30;
                }
                _labelsInitialised = true;
            }
            if (_labelSecondCounter >= 12)
            {

                for (int i = 0; i <= 11; i++)
                {

                    int labelValue = Convert.ToInt32(_labelHeadHeight[i].Content);
                    labelValue += 12;
                    _labelHeadHeight[i].Content = labelValue.ToString();

                }


                _labelSecondCounter = 0;
                _labelXAxisIncrement = 0;

                grapher.Children.Clear();
            }

            System.Windows.Shapes.Line _headHeightGraphLine = new System.Windows.Shapes.Line();
            _headHeightGraphLine.Stroke = System.Windows.Media.Brushes.Red;
            _headHeightGraphLine.X1 = _labelXAxisIncrement - 1;
            _headHeightGraphLine.X2 = _labelXAxisIncrement;
            _headHeightGraphLine.Y1 = Math.Abs(headJoint.Position.Y * 100);
            _headHeightGraphLine.Y2 = Math.Abs(headJoint.Position.Y * 100);
            _headHeightGraphLine.HorizontalAlignment = HorizontalAlignment.Left;
            _headHeightGraphLine.VerticalAlignment = VerticalAlignment.Center;
            _headHeightGraphLine.StrokeThickness = 3;

            grapher.Children.Add(_headHeightGraphLine);

            _labelXAxisIncrement++;


        }
        //Create Excel sheet with the given information
        private void RecordDataButton_Click(object sender, RoutedEventArgs e)
        {

            switch (_recordTextData)
            {
                case true:
                    _recordTextData = false;
                    RecordDataButton.Content = "Rec";
                    break;
                case false:
                    _recordTextData = true;
                    RecordDataButton.Content = "Pause";
                    break;

            }
}
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


            UpdateHeadHeightPlot();


            TB_udp.Text = distanceHeadCurrentAbs.ToString();
}





        public void WriteDataSample()
        {


            if (_recordTextData == true)
            {

                if (!File.Exists(_recordingFilePath))
                {
                    // Create a file to write to.
                    _recordingWriter = new StreamWriter(_recordingFilePath);

                }
                if (_textRowNumber == 0)
                {
                    _recordingWriter.WriteLine(_textRowNumber + " "
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
                    _textRowNumber++;
                }

                else
                {
                    _recordingWriter.WriteLine(_textRowNumber + " "

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
                    _textRowNumber++;
                }

            }



        }





        public void UpdateBodyOrientation()
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
        /// <summary>
        /// Detect the Freezing of Gait based on the number of steps
        /// Features stride duration
        /// </summary>
        public void DetectFreezingOfGait()
        {

            // Knee-angle detection is used; the abandoned foot-to-floor experiment has been removed.





            Vector3D hipLeftVector = new Vector3D(hipLeftJoint.Position.X, hipLeftJoint.Position.Y, hipLeftJoint.Position.Z);
            Vector3D kneeLeftVector = new Vector3D(kneeLeftJoint.Position.X, kneeLeftJoint.Position.Y, kneeLeftJoint.Position.Z);
            Vector3D ankleLeftVector = new Vector3D(ankleLeftJoint.Position.X, ankleLeftJoint.Position.Y, ankleLeftJoint.Position.Z);
            _angleKneeLeft = CalculateAngleDegrees(kneeLeftVector - hipLeftVector, kneeLeftVector - ankleLeftVector);
            Vector3D hipRightVector = new Vector3D(hipRightJoint.Position.X, hipRightJoint.Position.Y, hipRightJoint.Position.Z);
            Vector3D kneeRightVector = new Vector3D(kneeRightJoint.Position.X, kneeRightJoint.Position.Y, kneeRightJoint.Position.Z);
            Vector3D ankleRightVector = new Vector3D(ankleRightJoint.Position.X, ankleRightJoint.Position.Y, ankleRightJoint.Position.Z);
            _angleKneeRight = CalculateAngleDegrees(kneeRightVector - hipRightVector, kneeRightVector - ankleRightVector);
            //Check if FOG has happened
            if (_angleKneeLeft >= KneeAngleThresholdDegrees || _angleKneeRight >= KneeAngleThresholdDegrees)
            {
            }
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
        }
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
                _lableFOG.Visibility = System.Windows.Visibility.Hidden;
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
        /// <summary>
        /// Receive signal from the client app for commands
        /// </summary>
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
