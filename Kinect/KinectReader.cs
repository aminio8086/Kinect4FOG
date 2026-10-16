// Kinect4FOG research implementation and modifications
// Copyright (c) Amin Amini. All rights reserved.
// Author: Amin Amini
// Associated publication: https://doi.org/10.1080/17483107.2018.1467975
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System.Windows.Controls;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;

namespace Parkinson.Detection
{

    public class KinectReader
    {
        private readonly MainWindow _mainWindow;
        public KinectReader(MainWindow inWindow)
        {
            _mainWindow = inWindow;

        }
        /// <summary>
        /// Reader for color frames
        /// </summary>
        public ColorFrameReader colorFrameReader = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap _colorBitmap = null;
       
        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 1;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing _bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        public KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper _coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        public BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the _bodies
        /// </summary>
        private Body[] _bodies = null;

        /// <summary>
        /// definition of _bones
        /// </summary>
        private List<Tuple<JointType, JointType>> _bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int _displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int _displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        /// <summary>
        /// Current status text to display
        /// </summary>
        private string _statusText = null;
        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

       

        //Image to be sent to a UDP server
        private byte[] _bitmapData;
//Downsizing RGB feed for UDP
        private TransformedBitmap _broadcastBitmap;
private void SendImage()
        {

                if (_mainWindow.broadcaster == true)
            {

                if (_mainWindow.udpBroadcast.IsConfigured==true)
                {

                        if (_bitmapData != null)
                        {
                            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                            IPAddress broadcast = _mainWindow.udpBroadcast.broadcast;
                            IPEndPoint ep = _mainWindow.udpBroadcast.ep;
                            s.SendTo(_bitmapData, ep);
                        } 
                    }
                    
                
                Thread.Sleep(1);
            } 
        }
      


        private void SendMonitoringInfo()
        {

            if (_mainWindow.fogDetectorBool == true)
            {

              
                byte[] buffer = Encoding.ASCII.GetBytes(((Math.Abs(_mainWindow.fogDetector.headJoint.Position.Y))*100).ToString());

                Socket s2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                IPAddress broadcast3 = IPAddress.Parse("127.0.0.1");
                IPEndPoint ep3 = new IPEndPoint(broadcast3, 8082);
             
               
                s2.SendTo(buffer, ep3);
            }

            Thread.Sleep(1);
           
        }


        public void Start()
        {
// one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // open the reader for the color frames
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

            // wire handler for frame arrival
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // create the bitmap to display
            this._colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);


            // get the coordinate mapper
            this._coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this._displayWidth = frameDescription.Width;
            this._displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this._bones = new List<Tuple<JointType, JointType>>();


            // set IsAvailableChanged event notifier

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            _mainWindow.Image.Source = imageSource;

            if (bodyFrameReader != null)
            {
                bodyFrameReader.FrameArrived += Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return _statusText;
            }

            set
            {
                if (_statusText != value)
                {
                    _statusText = value;

                    // notify any bound elements that the text has changed
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }
private void CloseSensorResources(object sender, RoutedEventArgs e)
        {


            if (colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                colorFrameReader.Dispose();
                colorFrameReader = null;
            }

            if (bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                bodyFrameReader.Dispose();
                bodyFrameReader = null;
            }

            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        }


        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        _colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this._colorBitmap.PixelWidth) && (colorFrameDescription.Height == this._colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                _colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            _colorBitmap.AddDirtyRect(new Int32Rect(0, 0, _colorBitmap.PixelWidth, _colorBitmap.PixelHeight));
                        }

                        _colorBitmap.Unlock();



                        // Transforming bitmap to lower resolution.
                        ScaleTransform scale = new ScaleTransform((160.0 / _colorBitmap.PixelWidth), (90.0 / _colorBitmap.PixelHeight));
                        this._broadcastBitmap = new TransformedBitmap(_colorBitmap, scale);


                        var width = _broadcastBitmap.PixelWidth;
                        var height = _broadcastBitmap.PixelHeight;
                        var stride = width * ((_broadcastBitmap.Format.BitsPerPixel + 7) / 8);

                        _bitmapData = new byte[height * stride];

                        var stream = new MemoryStream();
                        var encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(_broadcastBitmap));
                        encoder.Save(stream);
                        _bitmapData = stream.GetBuffer();

                    }
                }
            }
        }



        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {

           
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {

            


                    if (bodyFrame != null)
                    {
                        if (this._bodies == null)
                        {
                            this._bodies = new Body[bodyFrame.BodyCount];
                        }


                        // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                        // As long as those body objects are not disposed and not set to null in the array,
                        // those body objects will be re-used.
                        bodyFrame.GetAndRefreshBodyData(_bodies);
                        dataReceived = true;

                        if (_mainWindow.fogDetector != null && _mainWindow.fogDetector.Visibility == Visibility.Visible)
                        {
                            _mainWindow.fogDetector.A_floor = bodyFrame.FloorClipPlane.X;
                            _mainWindow.fogDetector.B_floor = bodyFrame.FloorClipPlane.Y;
                            _mainWindow.fogDetector.C_floor = bodyFrame.FloorClipPlane.Z;
                            _mainWindow.fogDetector.D_floor = bodyFrame.FloorClipPlane.W;
                        }

                    }

                
            }
            if (dataReceived)
            {


                {
                    using (DrawingContext dc = this.drawingGroup.Open())
                    {


                        BitmapEncoder encoder = new PngBitmapEncoder();
                        // create frame from the writable bitmap and add to encoder
                        encoder.Frames.Add(BitmapFrame.Create(this._colorBitmap));

                        ImageBrush ib = new ImageBrush();



                        ib.ImageSource = this._colorBitmap;
                        // Draw a transparent background to set the render size
                        dc.DrawRectangle(ib, null, new Rect(0.0, 0.0, this._displayWidth, this._displayHeight));
                    }
                    _mainWindow.canvas.Children.Clear();


                    foreach (Body body in this._bodies)
                    {

                        if (body.IsTracked)
                        {

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();


                            foreach (JointType jointType in joints.Keys)
                            {
                                if (_mainWindow.fogDetector != null && _mainWindow.fogDetector.Visibility == Visibility.Visible)
                                {
                                    _mainWindow.fogDetector.leftFootJoint = (joints[JointType.AnkleLeft]);
                                    _mainWindow.fogDetector.rightFootJoint = (joints[JointType.AnkleRight]);

                                    //For real-time head height plot
                                    _mainWindow.fogDetector.headJoint = (joints[JointType.Head]);

                                    // Joints used for knee-angle detection
                                    _mainWindow.fogDetector.spineMidJoint = (joints[JointType.SpineMid]);
                                    _mainWindow.fogDetector.spineBaseJoint = (joints[JointType.SpineBase]);
                                    _mainWindow.fogDetector.kneeLeftJoint = (joints[JointType.KneeLeft]);
                                    _mainWindow.fogDetector.hipLeftJoint = (joints[JointType.ShoulderLeft]);
                                    _mainWindow.fogDetector.ankleLeftJoint = (joints[JointType.AnkleLeft]);
                                    _mainWindow.fogDetector.kneeRightJoint = (joints[JointType.KneeRight]);
                                    _mainWindow.fogDetector.hipRightJoint = (joints[JointType.ShoulderRight]);
                                    _mainWindow.fogDetector.ankleRightJoint = (joints[JointType.AnkleRight]);

                                    // Joints used for body-orientation detection
                                    _mainWindow.fogDetector.shoulderRightJoint = (joints[JointType.ShoulderRight]);
                                    _mainWindow.fogDetector.shoulderLeftJoint = (joints[JointType.ShoulderLeft]);
                                }

                                //3D space point
                                CameraSpacePoint jointPosition = joints[jointType].Position;

                                // 2D space point
                                Point point = new Point();

                                ColorSpacePoint colorPoint = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(jointPosition);

                                point.X = float.IsInfinity(colorPoint.X) ? 0 : colorPoint.X;
                                point.Y = float.IsInfinity(colorPoint.Y) ? 0 : colorPoint.Y;
                                point.X = point.X / 3;
                                point.Y = point.Y / 3;

                                // Draw
                                System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse
                                {
                                    Fill = Brushes.Red,
                                    Width = 5,
                                    Height = 5
                                };

                                Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                                Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                                _mainWindow.canvas.Children.Add(ellipse);


                            }
                            if (_mainWindow.fogDetector != null && _mainWindow.fogDetector.Visibility == Visibility.Visible)
                            {
                                _mainWindow.fogDetector.UpdateHeadHeightAndFallState();
                                _mainWindow.fogDetector.UpdateVerticalLaserPosition();
                                _mainWindow.fogDetector.DetectFreezingOfGait();
                                _mainWindow.fogDetector.UpdateBodyOrientation();
                                _mainWindow.fogDetector.WriteDataSample();
                                _mainWindow.fogDetector.UpdateGaitInformation();



                            }



                                SendImage();
                                 SendMonitoringInfo();




                        }

                    }


                }
            }



          



        }
}
}
