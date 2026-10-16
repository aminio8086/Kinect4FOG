// Kinect4FOG research implementation and modifications
// Copyright (c) Amin Amini. All rights reserved.
// Author: Amin Amini
// Associated publication: https://doi.org/10.1080/17483107.2018.1467975
//
//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Parkinson.Detection
{

    #region Namespace
    using System.ComponentModel;
    using System.Windows;






    #endregion

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window
    {





        public bool fogDetectorBool = false;
        public bool broadcaster = false;

       private KinectReader _kinectReader;

        public FogDetector fogDetector;
        public UdpBroadcastSettingsWindow udpBroadcast;

        private readonly SerialController _serialController;
        private readonly SerialPortSettingsWindow _serialPortSettingsWindow;


        #region KinectStuff


        #endregion


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
       {


           // initialize the components (controls) of the window
           this.InitializeComponent();

       }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {


            _kinectReader = new KinectReader(this);
            _kinectReader.Start();
       
        }
        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {

            if (_kinectReader.colorFrameReader != null)
            {
                // ColorFrameReader is IDisposable
                _kinectReader.colorFrameReader.Dispose();
                _kinectReader.colorFrameReader = null;
            }

            if (_kinectReader.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                _kinectReader.bodyFrameReader.Dispose();
                _kinectReader.bodyFrameReader = null;
            }

            if (_kinectReader.kinectSensor != null)
            {
                _kinectReader.kinectSensor.Close();
                _kinectReader.kinectSensor = null;
            }

            Application.Current.Shutdown();
        
        }


        private void Menu_COMSetting_Click(object sender, RoutedEventArgs e)
        {
           
            _serialPortSettingsWindow.Show();
        }

        private void Menu_DataWindow_Click(object sender, RoutedEventArgs e)
        {
            fogDetector = new FogDetector(_serialController);
            fogDetector.Show();
            fogDetectorBool = true;
    }

        private void Menu_Broadcast_Click(object sender, RoutedEventArgs e)
        {
            udpBroadcast = new UdpBroadcastSettingsWindow();
            udpBroadcast.Show();
            broadcaster = true;

        }

    }

}
