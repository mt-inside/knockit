using System;
using System.Windows;
using System.Windows.Controls;
using NAudio.Wave;

namespace knockit
{
    /// <summary>
    /// Interaction logic for WindowSettings.xaml
    /// </summary>
    public partial class WindowSettings : Window
    {
        private readonly WaveIn _waveIn;
        private readonly WaveOut _waveOut;

        public WindowSettings(WaveIn waveIn, WaveOut waveOut)
        {
            _waveIn = waveIn;
            _waveOut = waveOut;

            InitializeComponent();


            for (int i = 0; i < WaveIn.DeviceCount; ++i)
            {
                var caps = WaveIn.GetCapabilities(i);
                listBoxIn.Items.Add(String.Format("{0} ({1})", caps.ProductName, caps.Channels));
            }

            for (int i = 0; i < WaveOut.DeviceCount; ++i)
            {
                var caps = WaveOut.GetCapabilities(i);
                listBoxOut.Items.Add(String.Format("{0} ({1})", caps.ProductName, caps.Channels));
            }
        }


        private void listBoxIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var me = sender as ListBox;
            if (me != null && me.SelectedIndex >= 0)
            {
                /* TODO: don't think this is working */
                _waveIn.StopRecording();
                _waveIn.DeviceNumber = me.SelectedIndex;
                _waveIn.StartRecording();
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void listBoxOut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var me = sender as ListBox;
            if (me != null && me.SelectedIndex >= 0)
            {
                /* TODO: don't think this is working */
                _waveOut.Stop();
                _waveOut.DeviceNumber = me.SelectedIndex;
                _waveOut.Play();
            }
        }
    }
}
