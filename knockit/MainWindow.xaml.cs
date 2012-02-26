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
        private const int c_MaxFrequency = c_SampleRate/2;
        private WaveIn m_WaveIn;
        private Recorder m_Recorder = new Recorder(c_SampleRate);
        private WaveOut m_WaveOut;
        private readonly BandPass2 _bandPassProvider;

        public MainWindow()
        {
            InitializeComponent();

            polygonSpectrumControl1.Recorder = m_Recorder;

            m_Recorder.NewPrimaryFrequencyEvent += m_Recorder_NewPrimaryFrequencyEvent;

            m_WaveIn = new WaveIn
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(m_Recorder.SampleRate, 16, 1)
            };
            
            IWaveProvider waveInWaveProvider = new WaveInProvider(m_WaveIn);
            ISampleProvider sampleChannel = new SampleChannel(waveInWaveProvider);
            _bandPassProvider = new BandPass2(sampleChannel);
            NotifyingSampleProvider sampleStream = new NotifyingSampleProvider(_bandPassProvider);

            sampleStream.Sample +=sampleStream_Sample;
            m_WaveIn.StartRecording();

            m_WaveOut = new WaveOut
                            {
                                Volume = 0
                            };
            m_WaveOut.Init(new SampleToWaveProvider(sampleStream));
            m_WaveOut.Play();
            // TODO: dispose of these, close, etc

            sliderMinFreq.ValueChanged += SliderMinFreqValueChanged;
            sliderMaxFreq.ValueChanged += SliderMaxFreqValueChanged;
            SliderMinFreqValueChanged(sliderMinFreq, new RoutedPropertyChangedEventArgs<double>(sliderMinFreq.Minimum,sliderMinFreq.Minimum));
            SliderMaxFreqValueChanged(sliderMaxFreq, new RoutedPropertyChangedEventArgs<double>(sliderMaxFreq.Maximum,sliderMaxFreq.Maximum));

            textBoxPistonDiameter.TextChanged += textBoxPistonDiameter_TextChanged;
            textBoxEpsilon.TextChanged += textBoxPistonDiameter_TextChanged;
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
            const float c_KnockFreq1mm = 572200; /* Frequency of knock in a 1mm piston. http://www.phormula.co.uk/KnockCalculator.aspx */
            double diameter, knockFreq, eFreq;

            if (!Double.TryParse(textBoxEpsilon.Text, out eFreq)) eFreq = 100; //TODO to const and use for init

            if (Double.TryParse(textBoxPistonDiameter.Text, out diameter))
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
}