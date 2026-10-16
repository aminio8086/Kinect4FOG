// Kinect4FOG research implementation and modifications
// Copyright (c) Amin Amini. All rights reserved.
// Author: Amin Amini
// Associated publication: https://doi.org/10.1080/17483107.2018.1467975
//

using System.Windows;


namespace Parkinson.Detection
{
    /// <summary>
    /// Interaction logic for SerialPortSettingsWindow.xaml
    /// </summary>
    public partial class SerialPortSettingsWindow : Window
    {

        private SerialController st=new SerialController();

      

        public SerialPortSettingsWindow()
        {
            InitializeComponent();
        }

        private void B_scan_Click(object sender, RoutedEventArgs e)
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();



            // Display each port name to the console. 
            foreach (string port in ports)
            {
                List_com.Items.Clear();
                List_com.Items.Add(port);
            }


        }

        private void B_save_Click(object sender, RoutedEventArgs e)
        {

            st.savePort(List_com.SelectedItem.ToString());
            

        }
    }
}
    

