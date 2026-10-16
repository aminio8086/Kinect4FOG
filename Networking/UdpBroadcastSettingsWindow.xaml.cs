// Kinect4FOG research implementation and modifications
// Copyright (c) Amin Amini. All rights reserved.
// Author: Amin Amini
// Associated publication: https://doi.org/10.1080/17483107.2018.1467975
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Parkinson.Detection
{
    /// <summary>
    /// Interaction logic for UdpBroadcastSettingsWindow.xaml
    /// </summary>
    public partial class UdpBroadcastSettingsWindow : Window
    {
        public bool IsConfigured = false;
       public IPAddress broadcast;
         public    IPEndPoint ep;
        public UdpBroadcastSettingsWindow()
        {
            InitializeComponent();
        }

        private void B_save_Click(object sender, RoutedEventArgs e)
        {
            int anInteger;
            anInteger = Convert.ToInt32(text_Port.Text);
            anInteger = int.Parse(text_Port.Text);

             broadcast = IPAddress.Parse(text_IP.Text);

            ep = new IPEndPoint(broadcast, anInteger);
            IsConfigured = true;



        }
    }
}
