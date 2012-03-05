using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace knockit
{
    public partial class PolygonSpectrumControl : UserControl
    {
        private BandPass2 _bp2;

        internal BandPass2 bp2
        {
            set
            {
                _bp2 = value;
                _bp2.NewFFTDataEvent += _recorder_NewFFTDataEvent;
            }
        }

        public PolygonSpectrumControl()
        {
            InitializeComponent();
        }

        void _recorder_NewFFTDataEvent(object sender, NewFFTDataEventArgs e)
        {
            if (IsEnabled)
            {
                DrawFFT(e.Freqs, e.Mags, e.Len);
            }
        }

        #if false
        private void DrawFFT(float[] fft /* normalised to [0-1] */)
        {
            const int barWidth = 1;
            int barCount = (int)ActualWidth/barWidth;
            float[] bars = new float[barCount];

            for(int i = 0; i < barCount; ++i)
            {
                int lowerBin = (int) (((float) i/barCount)*fft.Length);
                int upperBin = (int) (((float) (i + 1)/barCount)*fft.Length);
                for(int j = lowerBin; j < upperBin; ++j)
                {
                    bars[i] += fft[j];
                }
                bars[i] /= (upperBin - lowerBin);
            }

            /* clear old */
            mainCanvas.Children.Clear();

            /* draw new */
            for(int i = 0; i < barCount; ++i)
            {
                var rect = new Rectangle();
                rect.Fill = new SolidColorBrush(Colors.Black);
                rect.Width = barWidth;
                rect.Height =bars[i] * ActualHeight;
                Canvas.SetLeft(rect, i * barWidth);
                Canvas.SetTop(rect, ActualHeight - rect.Height);
                mainCanvas.Children.Add(rect);
            }
        }
        #endif

        #if false
        private void DrawFFT(float[] fft /* normalised */)
        {
            const int pointSpacing = 1;
            int pointCOunt = (int) ActualWidth/pointSpacing;

            for(int i = 0; i < pointCOunt; ++i)
            {
                float point = 0;
                int lowerBin = (int) (((float) i/pointCOunt)*fft.Length);
                int upperBin = (int) (((float) (i + 1)/pointCOunt)*fft.Length);
                for(int j = lowerBin; j < upperBin; ++j)
                {
                    point += fft[j];
                }
                point /= (upperBin - lowerBin);
                Point p = new Point(i * pointSpacing, ActualHeight - (point * ActualHeight));
                if(i >= polylineSpectrum.Points.Count)
                {
                    polylineSpectrum.Points.Add(p);
                }
                else
                {
                    polylineSpectrum.Points[i] = p;
                }
            }
        }
        #endif

        private void DrawFFT(float[] freqs, float[] mags, int len)
        {
            const int pointSpacing = 1;
            int pointCount = (int)ActualWidth / pointSpacing;
            int j = 0, old_j;

            float max = mags.Max(); //todo: push me into bp2 and have it record primary freq there too.

            for (int i = 0; i < pointCount; ++i)
            {
                float point = 0;
                int maxFreq = (int)(((float)(i + 1) / pointCount) * MainWindow.SampleRate);
                old_j = j;
                while(j < len && freqs[j] < maxFreq)
                {
                    point += mags[j];
                    j++;
                }

                Point p;
                if (j == old_j)
                {
                    p = new Point(i * pointSpacing, ActualHeight); //should i skip these? can i bring the freqs closer together with a larger fft frame for example?
                    //continue;
                }
                else
                {
                    point /= (j - old_j);
                    point /= max;
                    p = new Point(i * pointSpacing, ActualHeight - (Math.Abs(point) * ActualHeight));
                }
                
                if (i >= polylineSpectrum.Points.Count)
                {
                    polylineSpectrum.Points.Add(p);
                }
                else
                {
                    polylineSpectrum.Points[i] = p;
                }
            }
        }
    }
}
