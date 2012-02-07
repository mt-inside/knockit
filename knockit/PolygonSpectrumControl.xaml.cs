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

        private void DrawFFT(double[] fft /* normalised to [0-1] */)
        {
            const int barWidth = 1;
            int barCount = (int)ActualWidth/barWidth;
            double[] bars = new double[barCount];

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
    }
}
