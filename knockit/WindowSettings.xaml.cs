using System;
using System.Windows;
using NAudio.Wave;

namespace knockit
{
    /// <summary>
    /// Interaction logic for WindowSettings.xaml
    /// </summary>
    public partial class WindowSettings : Window
    {
        public WindowSettings()
        {
            InitializeComponent();


            for (int i = 0; i < WaveIn.DeviceCount; ++i)
            {
                var caps = WaveIn.GetCapabilities(i);
                listBox1.Items.Add(String.Format("name {0} channels {1}", caps.ProductName, caps.Channels));
            }
        }
    }
}
