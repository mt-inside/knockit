using System;
using System.Diagnostics;
using NAudio.Dsp;

namespace knockit
{
    public static class FFTHelper
    {
        public static Complex[] FFT(float[] samples)
        {
            //Debug.Assert((samples.Length & (samples.Length - 1)) == 0);
            //FIXME: what happens if I pass non power of 2??

#if false
            Func<int, int, double> window_fn = FastFourierTransform.HammingWindow;
#else
            Func<int, int, double> window_fn = (a, b) => 1;
#endif

            Complex[] fftData = new Complex[samples.Length];
            for (int i = 0; i < samples.Length; ++i)
            {
                fftData[i].X = (float) (samples[i] * window_fn(i, samples.Length));
                fftData[i].Y = 0;
            }

            /* in-place, forwards FFT.
             * Input is a sequence of complex numbers with sample amplitude in the real part and a 0 imaginary part.
             * Output is a sequence of complex numbers, whose modulus is the amplitude at that frequency.
             * Outputs range in frequency from 0 to sample rate
             */
            FastFourierTransform.FFT(true, (int) Math.Log(fftData.Length, 2), fftData);

            return fftData;
        }

        public static float[] ComplexToAmplitude(Complex[] fftData, int count)
        {
            float[] freqDomain = new float[count];
            //float max = 0f; int freqMax = 0;

            for (int i = 0; i < count; ++i)
            {
                float amplitudeDB = (float) (10 * Math.Log(Math.Sqrt(fftData[i].X * fftData[i].X + fftData[i].Y * fftData[i].Y)));
                const float minAmplitude = -96; //dB
                if (amplitudeDB < minAmplitude) amplitudeDB = minAmplitude;
                freqDomain[i] = 1.0f - amplitudeDB / minAmplitude;
                /*if (freqDomain[i] > max)
                {
                    max = freqDomain[i];
                    freqMax = i;
                }*/
            }

            return freqDomain;
        }

        public static float[] InverseFFT(Complex[] frequencyDomain)
        {
            //Debug.Assert((frequencyDomain.Length & (frequencyDomain.Length - 1)) == 0);

            // TODO: need hamming window? Don't think so, think it exists to fade both ends of the time sample to 0 so one can pretend it's repeating

            /* in-place, "backwards" / inverse FFT. */
            FastFourierTransform.FFT(false, (int)Math.Log(frequencyDomain.Length, 2), frequencyDomain);

            float[] timeDomain = new float[frequencyDomain.Length];
            for(int i = 0; i < frequencyDomain.Length; ++i)
            {
                timeDomain[i] = frequencyDomain[i].X;
                //Debug.Assert(frequencyDomain[i].Y == 0);
            }

            return timeDomain;
        }
    }
}