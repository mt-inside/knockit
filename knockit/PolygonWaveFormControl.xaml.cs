﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace knockit
{
    public partial class PolygonWaveFormControl : UserControl
    {
        private Recorder _recorder;

        public Recorder Recorder
        {
            get { return _recorder; }
            set
            {
                _recorder = value;
                _recorder.NewWaveformSampleEvent += _recorder_NewWaveformSampleEvent;
            }
        }

        private int renderPosition;
        private float yTranslate = 40;
        private float yScale = 40;
        private float xScale = 2;
        private int blankZone = 10;

        Polygon waveForm = new Polygon();

        public PolygonWaveFormControl()
        {
            SizeChanged += OnSizeChanged;
            InitializeComponent();
            waveForm.Stroke = this.Foreground;
            waveForm.StrokeThickness = 1;
            waveForm.Fill = new SolidColorBrush(Colors.Bisque);
            mainCanvas.Children.Add(waveForm);            
        }

        void _recorder_NewWaveformSampleEvent(object sender, Recorder.NewWaveformSampleEventArgs e)
        {
            if (IsEnabled)
            {
                AddValue(e.Max, e.Min);
            }
        }
        
        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // We will remove everything as we are going to rescale vertically
            renderPosition = 0;
            ClearAllPoints();

            yTranslate = (float) (ActualHeight / 2);
            yScale = (float) (ActualHeight / 2);
        }

        private void ClearAllPoints()
        {
            waveForm.Points.Clear();
        }

        private int Points
        {
            get { return waveForm.Points.Count / 2; }
        }

        public void AddValue(float maxValue, float minValue)
        {
            int visiblePixels = (int)(ActualWidth / xScale);
            if (visiblePixels > 0)
            {
                CreatePoint(maxValue, minValue);

                if (renderPosition > visiblePixels)
                {
                    renderPosition = 0;
                }
                int erasePosition = (renderPosition + blankZone) % visiblePixels;
                if (erasePosition < Points)
                {
                    float yPos = SampleToYPosition(0);
                    waveForm.Points[erasePosition] = new Point(erasePosition * xScale, yPos);
                    waveForm.Points[BottomPointIndex(erasePosition)] = new Point(erasePosition * xScale, yPos);
                }
            }
        }

        private int BottomPointIndex(int position)
        {
            return waveForm.Points.Count - position - 1;
        }

        private float SampleToYPosition(float value)
        {
            return yTranslate + value * yScale;
        }

        private void CreatePoint(float topValue, float bottomValue)
        {
            float topYPos = SampleToYPosition(topValue);
            float bottomYPos = SampleToYPosition(bottomValue);
            float xPos = renderPosition * xScale;
            if (renderPosition >= Points)
            {
                int insertPos = Points;
                waveForm.Points.Insert(insertPos, new Point(xPos, topYPos));
                waveForm.Points.Insert(insertPos + 1, new Point(xPos, bottomYPos));
            }
            else
            {
                waveForm.Points[renderPosition] = new Point(xPos, topYPos);
                waveForm.Points[BottomPointIndex(renderPosition)] = new Point(xPos, bottomYPos);
            }
            renderPosition++;
        }

        /// <summary>
        /// Clears the waveform and repositions on the left
        /// </summary>
        public void Reset()
        {
            renderPosition = 0;
            ClearAllPoints();
        }
    }
}