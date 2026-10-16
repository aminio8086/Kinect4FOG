// Kinect4FOG research implementation and modifications
// Copyright (c) Amin Amini.
// SPDX-License-Identifier: Apache-2.0
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
        private WriteableBitmap colorBitmap = null;

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
        /// Pen used for drawing bones that are currently inferred
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
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        public BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;
        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        //Image to be sent to a UDP server
        private byte[] _bitmapData;
//Downsizing RGB feed for UDP
        private TransformedBitmap tbBitmap;
private void SendImage()
        {

                if (_mainWindow.broadcaster == true)
            {

                if (_mainWindow.udpBroadcast.IsConfigured==true)
                {

                        //   byte[] buffer = Encoding.ASCII.GetBytes("hi");
                        //byte[] buffer = (byte[])converter.ConvertTo(imageSource, typeof(byte[]));

                        if (_bitmapData != null)
                        {
                            using (UdpClient client = new UdpClient())
                            {
                                client.Send(_bitmapData, _bitmapData.Length, _mainWindow.udpBroadcast.EndPoint);
                            }
                        }
                    }
}
        }

        private void SendMonitoringInfo()
        {

            if (_mainWindow.fogDetectorBool == true)
            {

                byte[] buffer = Encoding.ASCII.GetBytes(((Math.Abs(_mainWindow.fogDetector.headJoint.Position.Y))*100).ToString());
                using (UdpClient client = new UdpClient())
                {
                    IPEndPoint monitoringEndpoint = new IPEndPoint(IPAddress.Loopback, 8082);
                    client.Send(buffer, buffer.Length, monitoringEndpoint);
                }
            }
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
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // set IsAvailableChanged event notifier
            //   this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

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
                return statusText;
            }

            set
            {
                if (statusText != value)
                {
                    statusText = value;

                    // notify any bound elements that the text has changed
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        private void kinectReader_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void kinectReader_Closing(object sender, RoutedEventArgs e)
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
                        colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));
                        }

                        colorBitmap.Unlock();

                        // Transforming bitmap to lower resolution.
                        ScaleTransform scale = new ScaleTransform((160.0 / colorBitmap.PixelWidth), (90.0 / colorBitmap.PixelHeight));
                        this.tbBitmap = new TransformedBitmap(colorBitmap, scale);

                        var width = tbBitmap.PixelWidth;
                        var height = tbBitmap.PixelHeight;
                        var stride = width * ((tbBitmap.Format.BitsPerPixel + 7) / 8);

                        _bitmapData = new byte[height * stride];
                        var stream = new MemoryStream();
                        var encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(tbBitmap));
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
                    if (bodies == null)
                    {
                        bodies = new Body[bodyFrame.BodyCount];
                    }

                    // Kinect reuses the Body instances in this array after the initial allocation.
                    bodyFrame.GetAndRefreshBodyData(bodies);
                    dataReceived = true;

                    if (_mainWindow.fogDetector != null &&
                        _mainWindow.fogDetector.Visibility == Visibility.Visible)
                    {
                        _mainWindow.fogDetector.A_floor = bodyFrame.FloorClipPlane.X;
                        _mainWindow.fogDetector.B_floor = bodyFrame.FloorClipPlane.Y;
                        _mainWindow.fogDetector.C_floor = bodyFrame.FloorClipPlane.Z;
                        _mainWindow.fogDetector.D_floor = bodyFrame.FloorClipPlane.W;
                    }
                }
            }

            if (!dataReceived)
            {
                return;
            }

            using (DrawingContext drawingContext = drawingGroup.Open())
            {
                ImageBrush imageBrush = new ImageBrush
                {
                    ImageSource = colorBitmap
                };

                drawingContext.DrawRectangle(
                    imageBrush,
                    null,
                    new Rect(0.0, 0.0, displayWidth, displayHeight));
            }

            _mainWindow.canvas.Children.Clear();

            foreach (Body body in bodies)
            {
                if (!body.IsTracked)
                {
                    continue;
                }

                IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                foreach (JointType jointType in joints.Keys)
                {
                    if (_mainWindow.fogDetector != null &&
                        _mainWindow.fogDetector.Visibility == Visibility.Visible)
                    {
                        _mainWindow.fogDetector.leftFootJoint = joints[JointType.AnkleLeft];
                        _mainWindow.fogDetector.rightFootJoint = joints[JointType.AnkleRight];
                        _mainWindow.fogDetector.headJoint = joints[JointType.Head];

                        // Joints used for knee-angle detection.
                        _mainWindow.fogDetector.spineMidJoint = joints[JointType.SpineMid];
                        _mainWindow.fogDetector.spineBaseJoint = joints[JointType.SpineBase];
                        _mainWindow.fogDetector.kneeLeftJoint = joints[JointType.KneeLeft];
                        _mainWindow.fogDetector.hipLeftJoint = joints[JointType.HipLeft];
                        _mainWindow.fogDetector.ankleLeftJoint = joints[JointType.AnkleLeft];
                        _mainWindow.fogDetector.kneeRightJoint = joints[JointType.KneeRight];
                        _mainWindow.fogDetector.hipRightJoint = joints[JointType.HipRight];
                        _mainWindow.fogDetector.ankleRightJoint = joints[JointType.AnkleRight];

                        // Joints used for body-orientation detection.
                        _mainWindow.fogDetector.shoulderRightJoint = joints[JointType.ShoulderRight];
                        _mainWindow.fogDetector.shoulderLeftJoint = joints[JointType.ShoulderLeft];
                    }

                    CameraSpacePoint jointPosition = joints[jointType].Position;
                    ColorSpacePoint colorPoint =
                        kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(jointPosition);

                    Point point = new Point
                    {
                        X = (float.IsInfinity(colorPoint.X) ? 0 : colorPoint.X) / 3,
                        Y = (float.IsInfinity(colorPoint.Y) ? 0 : colorPoint.Y) / 3
                    };

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

                if (_mainWindow.fogDetector != null &&
                    _mainWindow.fogDetector.Visibility == Visibility.Visible)
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
