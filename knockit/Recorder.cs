using System;
using NAudio.Dsp;

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
            private readonly float[] _fft;

            public NewFFTDataEventArgs(float[] fft)
            {
                _fft = fft;
            }

            public float[] Fft
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

        private float _minSample = 0;
        private float _maxSample = 0;
        public void Update(float sample)
        {
            m_SampleCount++;

            _minSample = Math.Min(_minSample, sample);
            _maxSample = Math.Max(_maxSample, sample);
            m_FftWindow[m_FftIndex++] = sample;

            if ((m_SampleCount % 4800) == 0)
            {
                /* TODO: update with max of the last peroid */
                RaiseEvent(new NewVolumeEventArgs(Math.Max(-_minSample, _maxSample)), NewVolumeEvent);
                /* wat should these values be? */
                RaiseEvent(new NewWaveformSampleEventArgs(_minSample, _maxSample), NewWaveformSampleEvent);

                _minSample = _maxSample = 0;
            }

            if ((m_FftIndex == m_FftWindow.Length))
            {
                m_FftIndex = 0;

                Complex[] fftResult = FFTHelper.FFT(m_FftWindow);
                float[] freqDomain = FFTHelper.ComplexToAmplitude(fftResult, fftResult.Length / 2);

                /*  */
                float binBandwith = (float)_sampleRate/m_FftWindow.Length;
                int firstBin = 0;
                int lastBin = (int) (5000/binBandwith);
                RaiseEvent(new NewFFTDataEventArgs(freqDomain), NewFFTDataEvent);

                // TODO: fft helper to raise struct with max etc in.
                //RaiseEvent(new NewPrimaryFrequencyEventArgs((int) (freqMax*binBandwith)), NewPrimaryFrequencyEvent);
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
}