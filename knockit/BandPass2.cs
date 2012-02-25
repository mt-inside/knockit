using System;
using NAudio.Wave;

namespace knockit
{
    internal class BandPass2 : ISampleProvider
    {
        private readonly ISampleProvider _src;

        internal BandPass2(ISampleProvider src)
        {
            _src = src;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            float[] wavein = new float[count];
            _src.Read(wavein, offset, count);

            smbPitchShift(0.5f, count, 1024, 4, _src.WaveFormat.SampleRate, wavein, buffer);

            return count;
        }

        public WaveFormat WaveFormat
        {
            get { return _src.WaveFormat; }
        }

        public const int MAX_FRAME_LENGTH = 8192;

        static float[] gInFIFO = new float[MAX_FRAME_LENGTH];
        static float[] gOutFIFO = new float[MAX_FRAME_LENGTH];
        static float[] gFFTworksp = new float[2 * MAX_FRAME_LENGTH];
        static float[] gLastPhase = new float[MAX_FRAME_LENGTH / 2 + 1];
        static float[] gSumPhase = new float[MAX_FRAME_LENGTH / 2 + 1];
        static float[] gOutputAccum = new float[2 * MAX_FRAME_LENGTH];
        static float[] gAnaFreq = new float[MAX_FRAME_LENGTH];
        static float[] gAnaMagn = new float[MAX_FRAME_LENGTH];
        static float[] gSynFreq = new float[MAX_FRAME_LENGTH];
        static float[] gSynMagn = new float[MAX_FRAME_LENGTH];
        static int gRover;

        ///<summary>
        /// Routine smbPitchShift(). See top of file for explanation
        /// Purpose: doing pitch shifting while maintaining duration using the Short
        /// Time Fourier Transform.
        /// Author: (c)1999-2009 Stephan M. Bernsee &lt;smb [AT] dspdimension [DOT] com&gt;
        ///</summary>
        ///<param name="pitchShift">Should be 1.0</param>
        ///<param name="numSampsToProcess">Length of indata</param>
        ///<param name="fftFrameSize">Size of fft frame to use. Must be &lt; 8092 and MUST be integral power of 2</param>
        ///<param name="osamp">Over-sampling factor. Should be at least 4, up to 32 for best quality</param>
        ///<param name="sampleRate">Sample rate in Hz of indata</param>
        ///<param name="indata">Input samples. Must be normalised to range [-1,1)</param>
        ///<param name="outdata">Output samples. Will be in range [-1,1). Can be same reference as indata for in-place transform</param>
        public static void smbPitchShift(float pitchShift, int numSampsToProcess, int fftFrameSize, int osamp, float sampleRate, float[] indata, float[] outdata)
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
                        gFFTworksp[2 * k] = (float)(gInFIFO[k] * window);
                        gFFTworksp[2 * k + 1] = 0.0f;
                    }


                    /* ***************** ANALYSIS ******************* */
                    /* do transform */
                    smbFft(gFFTworksp, fftFrameSize, -1);

                    /* this is the analysis step */
                    for (k = 0; k <= fftFrameSize2; k++)
                    {

                        /* de-interlace FFT buffer */
                        real = gFFTworksp[2 * k];
                        imag = gFFTworksp[2 * k + 1];

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
                    /* this does the actual pitch shifting */
                    Array.Clear(gSynMagn, 0, fftFrameSize);
                    Array.Clear(gSynFreq, 0, fftFrameSize);
                    for (k = 0; k <= fftFrameSize2; k++)
                    {
                        index = (int)(k * pitchShift);
                        if (index <= fftFrameSize2)
                        {
                            gSynMagn[index] += gAnaMagn[k];
                            gSynFreq[index] = gAnaFreq[k] * pitchShift;
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
                        gFFTworksp[2 * k] = (float)(magn * Math.Cos(phase));
                        gFFTworksp[2 * k + 1] = (float)(magn * Math.Sin(phase));
                    }

                    /* zero negative frequencies */
                    for (k = fftFrameSize + 2; k < 2 * fftFrameSize; k++) gFFTworksp[k] = 0.0f;

                    /* do inverse transform */
                    smbFft(gFFTworksp, fftFrameSize, 1);

                    /* do windowing and add to output accumulator */
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = -.5 * Math.Cos(2.0 * Math.PI * (double)k / (double)fftFrameSize) + .5;
                        gOutputAccum[k] += (float)(2.0 * window * gFFTworksp[2 * k] / (fftFrameSize2 * osamp));
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

        // -----------------------------------------------------------------------------------------------------------------


        /* 
            FFT routine, (C)1996 S.M.Bernsee. Sign = -1 is FFT, 1 is iFFT (inverse)
            Fills fftBuffer[0...2*fftFrameSize-1] with the Fourier transform of the
            time domain data in fftBuffer[0...2*fftFrameSize-1]. The FFT array takes
            and returns the cosine and sine parts in an interleaved manner, ie.
            fftBuffer[0] = cosPart[0], fftBuffer[1] = sinPart[0], asf. fftFrameSize
            must be a power of 2. It expects a complex input signal (see footnote 2),
            ie. when working with 'common' audio signals our input signal has to be
            passed as {in[0],0.,in[1],0.,in[2],0.,...} asf. In that case, the transform
            of the frequencies of interest is in fftBuffer[0...fftFrameSize].
        */
        public static void smbFft(float[] fftBuffer, int fftFrameSize, int sign)
        {
            float wr, wi, arg, temp;
            int p1, p2; // MRH: were float*
            float tr, ti, ur, ui;
            int p1r, p1i, p2r, p2i; // MRH: were float*
            int i, bitm, j, le, le2, k;
            int fftFrameSize2 = fftFrameSize * 2;

            for (i = 2; i < fftFrameSize2 - 2; i += 2)
            {
                for (bitm = 2, j = 0; bitm < fftFrameSize2; bitm <<= 1)
                {
                    if ((i & bitm) != 0) j++;
                    j <<= 1;
                }
                if (i < j)
                {
                    p1 = i; p2 = j;
                    temp = fftBuffer[p1];
                    fftBuffer[p1++] = fftBuffer[p2];
                    fftBuffer[p2++] = temp;
                    temp = fftBuffer[p1];
                    fftBuffer[p1] = fftBuffer[p2];
                    fftBuffer[p2] = temp;
                }
            }
            int kmax = (int)(Math.Log(fftFrameSize) / Math.Log(2.0) + 0.5);
            for (k = 0, le = 2; k < kmax; k++)
            {
                le <<= 1;
                le2 = le >> 1;
                ur = 1.0f;
                ui = 0.0f;
                arg = (float)(Math.PI / (le2 >> 1));
                wr = (float)Math.Cos(arg);
                wi = (float)(sign * Math.Sin(arg));
                for (j = 0; j < le2; j += 2)
                {
                    p1r = j; p1i = p1r + 1;
                    p2r = p1r + le2; p2i = p2r + 1;
                    for (i = j; i < fftFrameSize2; i += le)
                    {
                        float p2rVal = fftBuffer[p2r];
                        float p2iVal = fftBuffer[p2i];
                        tr = p2rVal * ur - p2iVal * ui;
                        ti = p2rVal * ui + p2iVal * ur;
                        fftBuffer[p2r] = fftBuffer[p1r] - tr;
                        fftBuffer[p2i] = fftBuffer[p1i] - ti;
                        fftBuffer[p1r] += tr;
                        fftBuffer[p1i] += ti;
                        p1r += le;
                        p1i += le;
                        p2r += le;
                        p2i += le;
                    }
                    tr = ur * wr - ui * wi;
                    ui = ur * wi + ui * wr;
                    ur = tr;
                }
            }
        }

        /// <summary>
        ///    12/12/02, smb
        ///
        ///    PLEASE NOTE:
        ///
        ///    There have been some reports on domain errors when the atan2() function was used
        ///    as in the above code. Usually, a domain error should not interrupt the program flow
        ///    (maybe except in Debug mode) but rather be handled "silently" and a global variable
        ///    should be set according to this error. However, on some occasions people ran into
        ///    this kind of scenario, so a replacement atan2() function is provided here.
        ///    If you are experiencing domain errors and your program stops, simply replace all
        ///    instances of atan2() with calls to the smbAtan2() function below.
        /// </summary>
        double smbAtan2(double x, double y)
        {
            double signx;
            if (x > 0.0) signx = 1.0;
            else signx = -1.0;

            if (x == 0.0) return 0.0;
            if (y == 0.0) return signx * Math.PI / 2.0;

            return Math.Atan2(x, y);
        }
    }
}
