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
        private const int c_SampleRate = 48000;
        private WaveIn m_WaveIn;
        private Recorder m_Recorder = new Recorder(c_SampleRate);
        private WaveOut m_WaveOut;
        private readonly BandPass2 _bandPassProvider;

        public MainWindow(string[] args)
        {
            InitializeComponent();

            IWaveProvider waveIn;

            polygonSpectrumControl1.Recorder = m_Recorder;

            m_Recorder.NewPrimaryFrequencyEvent += m_Recorder_NewPrimaryFrequencyEvent;

            if (args.Length == 1) /* TODO: button for this */
            {
                WaveStream readerStream = new WaveFileReader(args[0]);
                if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                {
                    readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                    readerStream = new BlockAlignReductionStream(readerStream);
                }

                if (readerStream.WaveFormat.BitsPerSample != 16)
                {
                    var format = new WaveFormat(readerStream.WaveFormat.SampleRate, 16, readerStream.WaveFormat.Channels);
                    readerStream = new WaveFormatConversionStream(format, readerStream);
                }
                waveIn = new LoopingStream(new WaveChannel32(readerStream));
            }
            else
            {
                m_WaveIn = new WaveIn
                {
                    DeviceNumber = 2,
                    WaveFormat = new WaveFormat(m_Recorder.SampleRate, 16, 1)
                };
                waveIn = new WaveInProvider(m_WaveIn);
                m_WaveIn.StartRecording();
            }
            
            ISampleProvider sampleChannel = new SampleChannel(waveIn); //TODO: when we're not using recorder any more, this stay as a wave provider, I think
            _bandPassProvider = new BandPass2(sampleChannel);
            NotifyingSampleProvider sampleStream = new NotifyingSampleProvider(_bandPassProvider);
            sampleStream.Sample += sampleStream_Sample; //TODO: want to get away from using the Recorder, really

            m_WaveOut = new WaveOut
            {
                Volume = 1
            };
            m_WaveOut.Init(new SampleToWaveProvider(sampleStream));
            m_WaveOut.Play();
            // TODO: dispose of these, close, etc

            sliderMinFreq.ValueChanged += SliderMinFreqValueChanged;
            sliderMaxFreq.ValueChanged += SliderMaxFreqValueChanged;
            textBoxPistonDiameter.TextChanged += textBoxPistonDiameter_TextChanged;
            textBoxEpsilon.TextChanged += textBoxEpsilon_TextChanged;

            UpdateShit();
        }

        private void sampleStream_Sample(object sender, SampleEventArgs e)
        {
            m_Recorder.Update(e.Left);
        }

        void m_Recorder_NewPrimaryFrequencyEvent(object sender, Recorder.NewPrimaryFrequencyEventArgs e)
        {
            label1.Content = e.PrimaryFrequency;
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
            //if (IsInitialized)
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
            (new WindowSettings(m_WaveIn, m_WaveOut)).ShowDialog();
        }

        private void buttonQuit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }
    }
}