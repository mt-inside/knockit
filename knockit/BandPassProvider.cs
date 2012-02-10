using System;
using System.Diagnostics;
using NAudio.Dsp;
using NAudio.Wave;

namespace knockit
{
    public class BandPassProvider : ISampleProvider
    {
        private readonly ISampleProvider _src;
        private float _minFreqNormalised;
        private float _maxFreqNormalised;
        private WaveFileWriter _outFile;


        public BandPassProvider(ISampleProvider src)
        {
            _src = src;
            _outFile = new WaveFileWriter(@"c:\Users\user\Desktop\lol.wav", WaveFormat);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            // seem to be getting samples 14k at a time. more than enough for an fft :)

            /* Get samples */
            float[] wave = new float[count];
            _src.Read(wave, offset, count);

#if true
            /* Convert to frequency domain */
            Complex[] freqDomain = FFTHelper.FFT(wave);
#endif

            int startFreqIndex, endFreqIndex;
#if true
            /* Band-pass filter */
            startFreqIndex = (int) (((float)freqDomain.Length / 2) * _minFreqNormalised);
            endFreqIndex = (int) (((float)freqDomain.Length / 2) * _maxFreqNormalised);
            for(int i = 0; i < startFreqIndex; ++i)
            {
                freqDomain[i].X = freqDomain[i].Y = 0;
            }
            for(int i = endFreqIndex; i < freqDomain.Length - endFreqIndex; ++i)
            {
                freqDomain[i].X = freqDomain[i].Y = 0;
            }
            for (int i = freqDomain.Length - startFreqIndex; i < freqDomain.Length; ++i)
            {
                freqDomain[i].X = freqDomain[i].Y = 0;
            }
#endif

            /* Convert back to time domain */
            float[] fileredSamples;
#if true
            fileredSamples = FFTHelper.InverseFFT(freqDomain);
#else
            fileredSamples = wave;
#endif

#if false
            /* Band-pass filter */
            startFreqIndex = (int) (fileredSamples.Length * _minFreqNormalised);
            endFreqIndex = (int) (fileredSamples.Length * _maxFreqNormalised);
            for(int i = 0; i < startFreqIndex; ++i)
            {
                fileredSamples[i] = 0;
            }
            for(int i = endFreqIndex; i < freqDomain.Length - endFreqIndex; ++i)
            {
                fileredSamples[i] = 0;
            }
            for (int i = freqDomain.Length - startFreqIndex; i < freqDomain.Length; ++i)
            {
                fileredSamples[i] = 0;
            }
#endif

            Debug.Assert(fileredSamples.Length == count);
            //fileredSamples.CopyTo(buffer, 0);
            for (int i = 0; i < fileredSamples.Length; ++i)
            {
                buffer[i] = fileredSamples[i];
            }

            _outFile.WriteSamples(buffer, 0, count);

            return fileredSamples.Length;
        }

        public WaveFormat WaveFormat
        {
            get { return _src.WaveFormat; }
        }

        public float MinFreqNormalised
        {
            get { return _minFreqNormalised; }
            set { _minFreqNormalised = value; }
        }

        public float MaxFreqNormalised
        {
            get { return _maxFreqNormalised; }
            set { _maxFreqNormalised = value; }
        }
    }
}