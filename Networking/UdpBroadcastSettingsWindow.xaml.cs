// Kinect4FOG research implementation and modifications
// Copyright (c) Amin Amini.
// SPDX-License-Identifier: Apache-2.0
// Author: Amin Amini
// Associated publication: https://doi.org/10.1080/17483107.2018.1467975
//
using System.Net;
using System.Windows;

namespace Parkinson.Detection
{
    /// <summary>
    /// Configures the UDP endpoint used for remote monitoring broadcasts.
    /// </summary>
    public partial class UdpBroadcastSettingsWindow : Window
    {
        public bool IsConfigured { get; private set; }

        public IPEndPoint EndPoint { get; private set; }

        public UdpBroadcastSettingsWindow()
        {
            InitializeComponent();
        }

        private void UdpBroadcastSettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void B_save_Click(object sender, RoutedEventArgs e)
        {
            IPAddress address;
            int port;

            if (!IPAddress.TryParse(text_IP.Text, out address))
            {
                MessageBox.Show(
                    "Enter a valid IP address.",
                    "Invalid address",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(text_Port.Text, out port) ||
                port < IPEndPoint.MinPort ||
                port > IPEndPoint.MaxPort)
            {
                MessageBox.Show(
                    "Enter a valid UDP port number (0-65535).",
                    "Invalid port",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            EndPoint = new IPEndPoint(address, port);
            IsConfigured = true;
        }
    }
}
