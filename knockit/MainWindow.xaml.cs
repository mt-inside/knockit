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
        private WaveIn m_WaveIn;
        private Recorder m_Recorder = new Recorder(48000);
        private WaveOut m_WaveOut;

        public MainWindow()
        {
            InitializeComponent();

            polygonWaveFormControl1.Recorder = m_Recorder; // pass this to the ctor
            polygonSpectrumControl1.Recorder = m_Recorder;

            for (int i = 0; i < WaveIn.DeviceCount; ++i)
            {
                var caps = WaveIn.GetCapabilities(i);
                listBox1.Items.Add(String.Format("name {0} channels {1}", caps.ProductName, caps.Channels));
            }

            m_Recorder.NewVolumeEvent += m_Recorder_NewVolumeEvent;
            m_Recorder.NewPrimaryFrequencyEvent += m_Recorder_NewPrimaryFrequencyEvent;

            m_WaveIn = new WaveIn
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(m_Recorder.SampleRate, 16, 1)
            };
            
            IWaveProvider foo = new WaveInProvider(m_WaveIn);
            ISampleProvider sampleChannel = new SampleChannel(foo);
            ISampleProvider bandpass = new BandPassProvider(sampleChannel);
            NotifyingSampleProvider sampleStream = new NotifyingSampleProvider(bandpass);

            sampleStream.Sample +=sampleStream_Sample;
            m_WaveIn.StartRecording();

            m_WaveOut = new WaveOut
                            {
                                Volume = 1
                            };
            m_WaveOut.Init(new SampleToWaveProvider(sampleStream));
            m_WaveOut.Play();
            // TODO: dispose of these, close, etc
        }

        private void sampleStream_Sample(object sender, SampleEventArgs e)
        {
            m_Recorder.Update(e.Left);
        }

        // implement an IWaveProvider that takes wave in and band-passes it.
        //   IWaveBuffer for the storage of data to manipulate
        // use buffered wave provide to echo
        // waveinprovider makes an iwaveprovider from a wave in - 1st stage in the pipeline?
        // look at resamplerDmoStream / simpleCompressorStream - BandPassStream should have same signature.
        // there are sample providers (mostly panner), but no wave providers?
        // RawSourceWaveStream could be useful (just echos a normal stream)


        /* something like:
         *                 while (connected)
                {
                    byte[] b = this.udpListener.Receive(ref endPoint);
                    byte[] decoded = listenerThreadState.Codec.Decode(b, 0, b.Length);
                    waveProvider.AddSamples(decoded, 0, decoded.Length);
                }
         */

        /* need to see what blocksize Read() gets called with, and what an fft on such a small smaple does.
         * - see how altering waveout's desired latency effects this.
         * INetworkCodec is a nice way to wrap up a codec, but doesn't do anything clever like block
         */

        void m_Recorder_NewVolumeEvent(object sender, Recorder.NewVolumeEventArgs e)
        {
            progressBar_Volume.Value = e.NormalisedVolume*100;
        }

        void m_Recorder_NewPrimaryFrequencyEvent(object sender, Recorder.NewPrimaryFrequencyEventArgs e)
        {
            label1.Content = e.PrimaryFrequency;
        }

        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var me = sender as ListBox;
            if (me != null)
            {
                /* TODO: don't think this is working */
                m_WaveIn.DeviceNumber = me.SelectedIndex;
            }
        }
    }
}