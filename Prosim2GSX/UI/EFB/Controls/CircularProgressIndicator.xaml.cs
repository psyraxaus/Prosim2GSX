using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Prosim2GSX.UI.EFB.Controls
{
    /// <summary>
    /// Interaction logic for CircularProgressIndicator.xaml
    /// </summary>
    public partial class CircularProgressIndicator : UserControl
    {
        private const double CircleRadius = 50;
        private const double CircleCenter = 50;

        /// <summary>
        /// Dependency property for Progress.
        /// </summary>
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(CircularProgressIndicator),
                new PropertyMetadata(0.0, OnProgressChanged));

        /// <summary>
        /// Dependency property for ProgressBrush.
        /// </summary>
        public static readonly DependencyProperty ProgressBrushProperty =
            DependencyProperty.Register("ProgressBrush", typeof(Brush), typeof(CircularProgressIndicator),
                new PropertyMetadata(null, OnProgressBrushChanged));

        /// <summary>
        /// Dependency property for BackgroundBrush.
        /// </summary>
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register("BackgroundBrush", typeof(Brush), typeof(CircularProgressIndicator),
                new PropertyMetadata(null, OnBackgroundBrushChanged));

        /// <summary>
        /// Dependency property for StrokeThickness.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(CircularProgressIndicator),
                new PropertyMetadata(8.0, OnStrokeThicknessChanged));

        /// <summary>
        /// Dependency property for ShowPercentage.
        /// </summary>
        public static readonly DependencyProperty ShowPercentageProperty =
            DependencyProperty.Register("ShowPercentage", typeof(bool), typeof(CircularProgressIndicator),
                new PropertyMetadata(true, OnShowPercentageChanged));

        /// <summary>
        /// Gets or sets the progress value (0.0 to 1.0).
        /// </summary>
        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        /// <summary>
        /// Gets or sets the progress brush.
        /// </summary>
        public Brush ProgressBrush
        {
            get { return (Brush)GetValue(ProgressBrushProperty); }
            set { SetValue(ProgressBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the background brush.
        /// </summary>
        public Brush BackgroundBrush
        {
            get { return (Brush)GetValue(BackgroundBrushProperty); }
            set { SetValue(BackgroundBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the stroke thickness.
        /// </summary>
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the percentage text.
        /// </summary>
        public bool ShowPercentage
        {
            get { return (bool)GetValue(ShowPercentageProperty); }
            set { SetValue(ShowPercentageProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularProgressIndicator"/> class.
        /// </summary>
        public CircularProgressIndicator()
        {
            InitializeComponent();
            UpdateProgressArc(Progress);
            UpdateProgressText(Progress);
        }

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularProgressIndicator)d;
            var newValue = (double)e.NewValue;
            
            // Clamp the value between 0 and 1
            newValue = Math.Max(0, Math.Min(1, newValue));
            
            control.UpdateProgressArc(newValue);
            control.UpdateProgressText(newValue);
        }

        private static void OnProgressBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularProgressIndicator)d;
            var newValue = (Brush)e.NewValue;
            
            if (newValue != null)
            {
                control.ProgressArc.Stroke = newValue;
            }
        }

        private static void OnBackgroundBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularProgressIndicator)d;
            var newValue = (Brush)e.NewValue;
            
            if (newValue != null)
            {
                control.BackgroundArc.Stroke = newValue;
            }
        }

        private static void OnStrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularProgressIndicator)d;
            var newValue = (double)e.NewValue;
            
            control.ProgressArc.StrokeThickness = newValue;
            control.BackgroundArc.StrokeThickness = newValue;
        }

        private static void OnShowPercentageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularProgressIndicator)d;
            var newValue = (bool)e.NewValue;
            
            control.ProgressText.Visibility = newValue ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateProgressArc(double progress)
        {
            // Calculate the end point of the arc
            double angle = progress * 360;
            double radians = angle * (Math.PI / 180);
            
            double x = CircleCenter + (CircleRadius * Math.Cos(radians));
            double y = CircleCenter + (CircleRadius * Math.Sin(radians));
            
            // Update the arc segment
            ProgressArcSegment.Point = new Point(x, y);
            ProgressArcSegment.IsLargeArc = angle > 180;
        }

        private void UpdateProgressText(double progress)
        {
            // Update the progress text
            int percentage = (int)(progress * 100);
            ProgressText.Text = $"{percentage}%";
        }
    }
}
