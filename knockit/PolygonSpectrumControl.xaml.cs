using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace knockit
{
    public partial class PolygonSpectrumControl : UserControl
    {
        private Recorder _recorder;

        public Recorder Recorder
        {
            get { return _recorder; }
            set
            {
                _recorder = value;
                _recorder.NewFFTDataEvent += _recorder_NewFFTDataEvent;
            }
        }

        public PolygonSpectrumControl()
        {
            InitializeComponent();
        }

        void _recorder_NewFFTDataEvent(object sender, Recorder.NewFFTDataEventArgs e)
        {
            if (IsEnabled)
            {
                DrawFFT(e.Fft);
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
    }
}
