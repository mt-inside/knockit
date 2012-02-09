using System;
using System.Diagnostics;
using NAudio.Dsp;
using NAudio.Wave;

namespace knockit
{
    public class BandPassProvider : ISampleProvider
    {
        private readonly ISampleProvider _src;

        public BandPassProvider(ISampleProvider src)
        {
            _src = src;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            // seem to be getting samples 14k at a time. more than enough for an fft :)

            /* Get samples */
            float[] wave = new float[count];
            _src.Read(wave, offset, count);

            /* Convert to frequency domain */
            Complex[] freqDomain = FFTHelper.FFT(wave);

            /* Band-pass filter */
            int startFreqIndex = 50; // TODO
            int endFreqIndex = 200;  // TOOD
            for(int i = 0; i < startFreqIndex; ++i)
            {
                freqDomain[i].X = freqDomain[i].Y = 0;
            }
            for(int i = endFreqIndex; i < freqDomain.Length; ++i)
            {
                freqDomain[i].X = freqDomain[i].Y = 0;
            }

            /* Convert back to time domain */
            float[] fileredSamples = FFTHelper.InverseFFT(freqDomain);

            Debug.Assert(fileredSamples.Length == count);
            Array.Copy(fileredSamples, buffer, count);

            return fileredSamples.Length;
        }

        public WaveFormat WaveFormat
        {
            get { return _src.WaveFormat; }
        }
    }
}