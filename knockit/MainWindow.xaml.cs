using System;
using System.Windows;
using System.Windows.Controls;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace knockit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _inputDeviceNo;
        private int _outputDeviceNo;
        private WaveIn _waveInDevice;
        private WaveStream _waveInFileStream;
        private WaveOut _waveOutDevice;
        private const int c_SampleRate = 48000;
        private readonly BandPass2 _bandPassProvider;

        public static int SampleRate
        {
            get { return c_SampleRate; }
        }

        public MainWindow(string[] args)
        {
            InitializeComponent();

            sliderMinFreq.Maximum = sliderMaxFreq.Value = sliderMaxFreq.Maximum = SampleRate;


            if (args.Length >= 1)
            {
                int.TryParse(args[0], out _inputDeviceNo);
            }
            if (args.Length >= 2)
            {
                int.TryParse(args[1], out _outputDeviceNo);
            }

            /* Input waveform */
            IWaveProvider inputWaveform;

            if (args.Length >= 3) /* TODO: button for this */
            {
                _waveInFileStream = new WaveFileReader(args[2]);
                if (_waveInFileStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                {
                    _waveInFileStream = WaveFormatConversionStream.CreatePcmStream(_waveInFileStream);
                    _waveInFileStream = new BlockAlignReductionStream(_waveInFileStream);
                }

                if (_waveInFileStream.WaveFormat.BitsPerSample != 16)
                {
                    var format = new WaveFormat(_waveInFileStream.WaveFormat.SampleRate, 16, _waveInFileStream.WaveFormat.Channels);
                    _waveInFileStream = new WaveFormatConversionStream(format, _waveInFileStream);
                }
                inputWaveform = new LoopingStream(new WaveChannel32(_waveInFileStream));
            }
            else
            {
                _waveInDevice = new WaveIn
                {
                    DeviceNumber = _inputDeviceNo,
                    WaveFormat = new WaveFormat(SampleRate, 16, 1)
                };
                _waveInDevice.StartRecording();

                inputWaveform = new WaveInProvider(_waveInDevice);
            }
            
            /* Input processing pipeline */
            ISampleProvider sampleChannel = new SampleChannel(inputWaveform); //TODO: when we're not using recorder any more, this stay as a wave provider, I think
            _bandPassProvider = new BandPass2(sampleChannel);
            NotifyingSampleProvider sampleStream = new NotifyingSampleProvider(_bandPassProvider);
            _polygonSpectrumControl.bp2 = _bandPassProvider;

            
            /* Output */
            _waveOutDevice = new WaveOut
            {
                DeviceNumber = _outputDeviceNo,
                Volume = 1
            };
            _waveOutDevice.Init(new SampleToWaveProvider(sampleStream));
            _waveOutDevice.Play();

            
            /* UI Events */
            sliderMinFreq.ValueChanged += SliderMinFreqValueChanged;
            sliderMaxFreq.ValueChanged += SliderMaxFreqValueChanged;
            textBoxPistonDiameter.TextChanged += textBoxPistonDiameter_TextChanged;
            textBoxEpsilon.TextChanged += textBoxEpsilon_TextChanged;

            UpdateShit();
        }

        private void SliderMinFreqValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int freq = (int) e.NewValue;

            if (sliderMaxFreq.Value < e.NewValue) sliderMaxFreq.Value = freq;
            _bandPassProvider.MinFreq = freq;
            labelMinFreq.Content = freq + "Hz";
        }

        private void SliderMaxFreqValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int freq = (int) e.NewValue;

            if (sliderMinFreq.Value > e.NewValue) sliderMinFreq.Value = freq;
            _bandPassProvider.MaxFreq = freq;
            labelMaxFreq.Content = freq + "Hz";
        }

        private void textBoxPistonDiameter_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateShit();
        }

        private void textBoxEpsilon_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateShit();
        }

        private void UpdateShit()
        {
            if (IsInitialized)
            {
                const float c_KnockFreq1mm = 572200; /* Frequency of knock in a 1mm piston. http://www.phormula.co.uk/KnockCalculator.aspx */
                double diameter, knockFreq, eFreq;
    
                if (!Double.TryParse(textBoxEpsilon.Text, out eFreq) || eFreq == 0) eFreq = 500; //TODO to const and use for init
    
                if (Double.TryParse(textBoxPistonDiameter.Text, out diameter) && diameter != 0)
                {
                    knockFreq = c_KnockFreq1mm/diameter; //TODO raise event
                    labelKnockFreq.Content = Math.Round(knockFreq/1000,3) + "kHz";
                    sliderMinFreq.Value = knockFreq - eFreq;
                    sliderMaxFreq.Value = knockFreq + eFreq;
                }
                else
                {
                    labelKnockFreq.Content = "--kHz";
                }
            }
        }

        private void buttonSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_waveInDevice != null && _waveOutDevice != null)
            {
                (new WindowSettings(_waveInDevice, _waveOutDevice)).ShowDialog();
            }
        }

        private void buttonQuit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_waveInDevice != null)
            {
                _waveInDevice.StopRecording();
                _waveInDevice.Dispose();
            }
            else if (_waveInFileStream != null)
            {
                _waveInFileStream.Close();
            }

            _waveOutDevice.Stop();
            _waveOutDevice.Dispose();
        }
    }
}
