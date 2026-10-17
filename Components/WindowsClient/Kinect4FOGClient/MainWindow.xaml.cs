// Kinect4FOG Windows monitoring client
// Copyright (c) Amin Amini.
// SPDX-License-Identifier: Apache-2.0
// Author: Amin Amini
// Associated publication: https://doi.org/10.1080/17483107.2018.1467975
//
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Kinect4FOG.Client
{
    /// <summary>
    /// Receives Kinect4FOG monitoring datagrams and sends commands to the main application.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MonitoringPort = 8082;
        private const int CommandPort = 8089;

        private UdpClient _listener;
        private Thread _receiveThread;
        private volatile bool _isClosing;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartReceiver();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;

            if (_listener != null)
            {
                _listener.Close();
                _listener = null;
            }
        }

        private void StartReceiver()
        {
            if (_listener != null)
            {
                return;
            }

            try
            {
                _listener = new UdpClient(MonitoringPort);
            }
            catch (SocketException ex)
            {
                MessageBox.Show(
                    "Unable to listen on UDP port " + MonitoringPort + ".\n\n" + ex.Message,
                    "Kinect4FOG Client",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Name = "Kinect4FOG UDP Receiver";
            _receiveThread.Start();

            StatusText.Text = "Listening on UDP port " + MonitoringPort + ".";
        }

        private void ReceiveLoop()
        {
            IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);

            while (!_isClosing)
            {
                byte[] payload;

                try
                {
                    payload = _listener.Receive(ref remoteEndpoint);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    if (_isClosing)
                    {
                        return;
                    }

                    continue;
                }

                if (payload == null || payload.Length == 0)
                {
                    continue;
                }

                if (IsJpeg(payload))
                {
                    BitmapImage frame = DecodeJpeg(payload);
                    if (frame != null)
                    {
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            MonitoringImage.Source = frame;
                            StatusText.Text = "Image received from " + remoteEndpoint.Address + ".";
                        }));
                    }
                }
                else
                {
                    string message = Encoding.ASCII.GetString(payload).Trim();
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        StatusText.Text = string.IsNullOrWhiteSpace(message)
                            ? "Monitoring packet received."
                            : "Monitoring data: " + message;
                    }));
                }
            }
        }

        private static bool IsJpeg(byte[] payload)
        {
            return payload.Length >= 2 &&
                   payload[0] == 0xFF &&
                   payload[1] == 0xD8;
        }

        private static BitmapImage DecodeJpeg(byte[] payload)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(payload, false))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch
            {
                return null;
            }
        }

        private void SendCommandButton_Click(object sender, RoutedEventArgs e)
        {
            IPAddress targetAddress;

            if (!IPAddress.TryParse(TargetAddressTextBox.Text, out targetAddress))
            {
                MessageBox.Show(
                    "Enter a valid target IP address.",
                    "Invalid address",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string command = CommandTextBox.Text.Trim();
            if (command.Length == 0)
            {
                MessageBox.Show(
                    "Enter a command to send.",
                    "Command required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            byte[] commandBytes = Encoding.ASCII.GetBytes(command);

            try
            {
                using (UdpClient senderClient = new UdpClient())
                {
                    senderClient.Send(
                        commandBytes,
                        commandBytes.Length,
                        new IPEndPoint(targetAddress, CommandPort));
                }

                StatusText.Text = "Command sent to " + targetAddress + ":" + CommandPort + ".";
            }
            catch (SocketException ex)
            {
                MessageBox.Show(
                    "Unable to send the UDP command.\n\n" + ex.Message,
                    "Kinect4FOG Client",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
