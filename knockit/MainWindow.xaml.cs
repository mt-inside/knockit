using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace knockit
{
    public class Recorder
    {
        private readonly int _sampleRate;
        private long m_SampleCount;

        public class NewVolumeEventArgs : EventArgs
        {
            private readonly float _normalisedVolume;

            public NewVolumeEventArgs(float normalisedVolume)
            {
                _normalisedVolume = normalisedVolume;
            }

            public float NormalisedVolume
            {
                get { return _normalisedVolume; }
            }
        }

        public class NewWaveformSampleEventArgs : EventArgs
        {
            private readonly float _max;
            private readonly float _min;

            public NewWaveformSampleEventArgs(float max, float min)
            {
                _max = max;
                _min = min;
            }

            public float Max
            {
                get { return _max; }
            }

            public float Min
            {
                get { return _min; }
            }
        }

        public class NewFFTDataEventArgs : EventArgs
        {
            private readonly double[] _fft;

            public NewFFTDataEventArgs(double[] fft)
            {
                _fft = fft;
            }

            public double[] Fft
            {
                /* normalised to [0-1] */
                get { return _fft; }
            }
        }
         public class NewPrimaryFrequencyEventArgs : EventArgs
         {
             private readonly int _primaryFrequency;

             public NewPrimaryFrequencyEventArgs(int primaryFrequency)
             {
                 _primaryFrequency = primaryFrequency;
             }

             public int PrimaryFrequency
             {
                 get { return _primaryFrequency; }
             }
         }

        public event EventHandler<NewVolumeEventArgs> NewVolumeEvent;
        public event EventHandler<NewWaveformSampleEventArgs> NewWaveformSampleEvent;
        public event EventHandler<NewFFTDataEventArgs> NewFFTDataEvent;
        public event EventHandler<NewPrimaryFrequencyEventArgs> NewPrimaryFrequencyEvent;

        private float[] m_FftWindow = new float[1024];
        private int m_FftIndex = 0;

        public Recorder(int sampleRate)
        {
            _sampleRate = sampleRate;
        }

        public int SampleRate
        {
            get { return _sampleRate; }
        }

        public void Update(float sample)
        {
            m_SampleCount++;

            m_FftWindow[m_FftIndex++] = sample;

            if ((m_SampleCount % 4800) == 0)
            {
                /* TODO: update with max of the last peroid */
                RaiseEvent(new NewVolumeEventArgs(sample), NewVolumeEvent);
                /* wat should these values be? */
                RaiseEvent(new NewWaveformSampleEventArgs(-sample, sample), NewWaveformSampleEvent);
            }

            if ((m_SampleCount % 4800) == 0)
            {
                
            }

            if ((m_FftIndex == m_FftWindow.Length))
            {
                m_FftIndex = 0;
                Func<int, int, double> window_fn = FastFourierTransform.HammingWindow;
                Complex[] fftData = new Complex[m_FftWindow.Length];
                for (int i = 0; i < m_FftWindow.Length; ++i)
                {
                    fftData[i].X = (float) (m_FftWindow[i] * window_fn(i, m_FftWindow.Length));
                    fftData[i].Y = 0;
                }
                /* in-place, forwards fft.
                 * Input is a sequence of complex numbers with sample amplitude in the real part and a 0 imaginary part.
                 * Output is a sequence of complex numbers, whose modulus is the amplitude at that frequency.
                 * Outputs range in frequency from 0 to sample rate
                 */
                FastFourierTransform.FFT(true, (int) Math.Log(fftData.Length, 2), fftData);
                /* TODO: chop this to samplerate / 2 here, as anything over that is just mirrored bollocks, and it's pointless wasteing cpu */
                double[] freqDomain = new double[fftData.Length / 2];
                double max = 0f; int freqMax = 0;
                for (int i = 0; i < fftData.Length / 2; ++i )
                {
                    double amplitudeDB = 10 * Math.Log(Math.Sqrt(fftData[i].X * fftData[i].X + fftData[i].Y * fftData[i].Y));
                    const double minAmplitude = -96; //dB
                    if (amplitudeDB < minAmplitude) amplitudeDB = minAmplitude;
                    freqDomain[i] = 1.0 - amplitudeDB/minAmplitude;
                    if (freqDomain[i] > max)
                    {
                        max = freqDomain[i];
                        freqMax = i;
                    }
                }


                /*  */
                float binBandwith = (float)_sampleRate/fftData.Length;
                int firstBin = 0;
                int lastBin = (int) (5000/binBandwith);
                //RaiseEvent(new NewFFTDataEventArgs(freqDomain.Skip(firstBin).Take(lastBin).ToArray()), NewFFTDataEvent);
                RaiseEvent(new NewFFTDataEventArgs(freqDomain), NewFFTDataEvent);

                RaiseEvent(new NewPrimaryFrequencyEventArgs((int) (freqMax*binBandwith)), NewPrimaryFrequencyEvent);
            }
        }

        private void RaiseEvent<T>(T eventArgs, EventHandler<T> handler)
            where T : EventArgs
        {
            if (handler != null)
            {
                handler(this, eventArgs);
            }
        }
    }

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
            //ISampleProvider sampleChannel = new SampleChannel(new WaveFileReader(@"c:\Users\user\Desktop\0-24000.wav"));
            NotifyingSampleProvider sampleStream = new NotifyingSampleProvider(sampleChannel);
            sampleStream.Sample +=sampleStream_Sample;
            m_WaveIn.StartRecording();

            m_WaveOut = new WaveOut
                            {
                                Volume = 0
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
