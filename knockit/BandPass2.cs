using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace knockit
{
    internal class BandPass2 : ISampleProvider
    {
        private readonly ISampleProvider _src;

        public BandPass2(ISampleProvider src)
        {
            _src = src;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            _src.Read(buffer, offset, count);

            // TODO: monitor cpu usage and adjust 3rd parameter.
            bandPass(count, 1024, 4, _src.WaveFormat.SampleRate, buffer, buffer);

            return count;
        }

        public WaveFormat WaveFormat
        {
            get { return _src.WaveFormat; }
        }

        public int MinFreq
        {
            get { return _minFreq; }
            set { _minFreq = value; }
        }

        public int MaxFreq
        {
            get { return _maxFreq; }
            set { _maxFreq = value; }
        }

        private const int MAX_FRAME_LENGTH = 8192;

        private float[] gInFIFO = new float[MAX_FRAME_LENGTH];
        private float[] gOutFIFO = new float[MAX_FRAME_LENGTH];
        private Complex[] gFFTworksp = new Complex[MAX_FRAME_LENGTH];
        private float[] gLastPhase = new float[MAX_FRAME_LENGTH / 2 + 1];
        private float[] gSumPhase = new float[MAX_FRAME_LENGTH / 2 + 1];
        private float[] gOutputAccum = new float[2 * MAX_FRAME_LENGTH];
        private float[] gAnaFreq = new float[MAX_FRAME_LENGTH];
        private float[] gAnaMagn = new float[MAX_FRAME_LENGTH];
        private float[] gSynFreq = new float[MAX_FRAME_LENGTH];
        private float[] gSynMagn = new float[MAX_FRAME_LENGTH];
        private int gRover;

        private int _minFreq;
        private int _maxFreq;

        ///<param name="numSampsToProcess">Length of indata</param>
        ///<param name="fftFrameSize">Size of fft frame to use. Must be &lt; 8092 and MUST be integral power of 2</param>
        ///<param name="osamp">Over-sampling factor. Should be at least 4, up to 32 for best quality</param>
        ///<param name="sampleRate">Sample rate in Hz of indata</param>
        ///<param name="indata">Input samples. Must be normalised to range [-1,1)</param>
        ///<param name="outdata">Output samples. Will be in range [-1,1). Can be same reference as indata for in-place transform</param>
        private void bandPass(int numSampsToProcess, int fftFrameSize, int osamp, float sampleRate, float[] indata, float[] outdata)
        {
            double magn, phase, tmp, window, real, imag;
            double freqPerBin, expct;
            int i, k, qpd, index, inFifoLatency, stepSize, fftFrameSize2;

            /* set up some handy variables */
            fftFrameSize2 = fftFrameSize / 2;
            stepSize = fftFrameSize / osamp;
            freqPerBin = sampleRate / (double)fftFrameSize;
            expct = 2.0 * Math.PI * (double)stepSize / (double)fftFrameSize;
            inFifoLatency = fftFrameSize - stepSize;
            if (gRover == 0) gRover = inFifoLatency;

            /* main processing loop */
            for (i = 0; i < numSampsToProcess; i++)
            {
                /* As long as we have not yet collected enough data just read in */
                gInFIFO[gRover] = indata[i];
                outdata[i] = gOutFIFO[gRover - inFifoLatency];
                gRover++;

                /* now we have enough data for processing */
                if (gRover >= fftFrameSize)
                {
                    gRover = inFifoLatency;

                    /* do windowing and re,im interleave */
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = -.5 * Math.Cos(2.0 * Math.PI * (double)k / (double)fftFrameSize) + .5;
                        gFFTworksp[k].X = (float)(gInFIFO[k] * window);
                        gFFTworksp[k].Y = 0.0f;
                    }


                    /* ***************** ANALYSIS ******************* */
                    /* do transform */
                    FastFourierTransform.FFT(false, (int) Math.Log(fftFrameSize, 2), gFFTworksp);

                    /* this is the analysis step */
                    for (k = 0; k <= fftFrameSize2; k++)
                    {

                        /* de-interlace FFT buffer */
                        real = gFFTworksp[k].X;
                        imag = gFFTworksp[k].Y;

                        /* compute magnitude and phase */
                        magn = 2.0 * Math.Sqrt(real * real + imag * imag);
                        phase = Math.Atan2(imag, real);

                        /* compute phase difference */
                        tmp = phase - gLastPhase[k];
                        gLastPhase[k] = (float)phase;

                        /* subtract expected phase difference */
                        tmp -= (double)k * expct;

                        /* map delta phase into +/- Pi interval */
                        qpd = (int)(tmp / Math.PI);
                        if (qpd >= 0) qpd += (qpd & 1);
                        else qpd -= (qpd & 1);
                        tmp -= Math.PI * (double)qpd;

                        /* get deviation from bin frequency from the +/- Pi interval */
                        tmp = osamp * tmp / (2.0 * Math.PI);

                        /* compute the k-th partials' true frequency */
                        tmp = (double)k * freqPerBin + tmp * freqPerBin;

                        /* store magnitude and true frequency in analysis arrays */
                        gAnaMagn[k] = (float)magn;
                        gAnaFreq[k] = (float)tmp;

                    }

                    /* ***************** PROCESSING ******************* */
                    /* Infinite-rolloff band-pass filter between _minFreq and _maxFreq */
                    Array.Clear(gSynMagn, 0, fftFrameSize);
                    Array.Clear(gSynFreq, 0, fftFrameSize);
                    for (k = 0; k <= fftFrameSize2; k++)
                    {
                        /* Worringly, I have no idea why I have to /2 here */
                        if (gAnaFreq[k] > _minFreq/2.0 && gAnaFreq[k] < _maxFreq/2.0 ||
                            gAnaFreq[k] > _minFreq && gAnaFreq[k] < _maxFreq)
                        {
                            gSynMagn[k] = gAnaMagn[k];
                            gSynFreq[k] = gAnaFreq[k];
                        }
                    }

                    /* ***************** SYNTHESIS ******************* */
                    /* this is the synthesis step */
                    for (k = 0; k <= fftFrameSize2; k++)
                    {

                        /* get magnitude and true frequency from synthesis arrays */
                        magn = gSynMagn[k];
                        tmp = gSynFreq[k];

                        /* subtract bin mid frequency */
                        tmp -= (double)k * freqPerBin;

                        /* get bin deviation from freq deviation */
                        tmp /= freqPerBin;

                        /* take osamp into account */
                        tmp = 2.0 * Math.PI * tmp / osamp;

                        /* add the overlap phase advance back in */
                        tmp += (double)k * expct;

                        /* accumulate delta phase to get bin phase */
                        gSumPhase[k] += (float)tmp;
                        phase = gSumPhase[k];

                        /* get real and imag part and re-interleave */
                        gFFTworksp[k].X = (float)(magn * Math.Cos(phase));
                        gFFTworksp[k].Y = (float)(magn * Math.Sin(phase));
                    }

                    /* zero negative frequencies */
                    for (k = fftFrameSize2 + 1; k < fftFrameSize; k++) gFFTworksp[k].X = gFFTworksp[k].Y = 0.0f;

                    /* do inverse transform */
                    FastFourierTransform.FFT(false, (int) Math.Log(fftFrameSize, 2), gFFTworksp);

                    /* do windowing and add to output accumulator */
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = -.5 * Math.Cos(2.0 * Math.PI * (double)k / (double)fftFrameSize) + .5;
                        gOutputAccum[k] += (float)(2.0 * window * gFFTworksp[k].X / (fftFrameSize2 * osamp));
                    }
                    for (k = 0; k < stepSize; k++) gOutFIFO[k] = gOutputAccum[k];

                    /* shift accumulator */
                    int destOffset = 0;
                    int sourceOffset = stepSize;
                    Array.Copy(gOutputAccum, sourceOffset, gOutputAccum, destOffset, fftFrameSize);
                    //memmove(gOutputAccum, gOutputAccum + stepSize, fftFrameSize * sizeof(float));

                    /* move input FIFO */
                    for (k = 0; k < inFifoLatency; k++) gInFIFO[k] = gInFIFO[k + stepSize];
                }
            }
        }
    }
}
