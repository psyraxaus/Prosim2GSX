using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Prosim2GSX.UI.EFB.Controls
{
    /// <summary>
    /// Interaction logic for LinearProgressIndicator.xaml
    /// </summary>
    public partial class LinearProgressIndicator : UserControl
    {
        /// <summary>
        /// Dependency property for Progress.
        /// </summary>
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(LinearProgressIndicator),
                new PropertyMetadata(0.0, OnProgressChanged));

        /// <summary>
        /// Dependency property for ProgressBrush.
        /// </summary>
        public static readonly DependencyProperty ProgressBrushProperty =
            DependencyProperty.Register("ProgressBrush", typeof(Brush), typeof(LinearProgressIndicator),
                new PropertyMetadata(null, OnProgressBrushChanged));

        /// <summary>
        /// Dependency property for BackgroundBrush.
        /// </summary>
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register("BackgroundBrush", typeof(Brush), typeof(LinearProgressIndicator),
                new PropertyMetadata(null, OnBackgroundBrushChanged));

        /// <summary>
        /// Dependency property for BorderBrush.
        /// </summary>
        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register("BorderBrush", typeof(Brush), typeof(LinearProgressIndicator),
                new PropertyMetadata(null, OnBorderBrushChanged));

        /// <summary>
        /// Dependency property for Label.
        /// </summary>
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(LinearProgressIndicator),
                new PropertyMetadata("Progress", OnLabelChanged));

        /// <summary>
        /// Dependency property for ShowPercentage.
        /// </summary>
        public static readonly DependencyProperty ShowPercentageProperty =
            DependencyProperty.Register("ShowPercentage", typeof(bool), typeof(LinearProgressIndicator),
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
        /// Gets or sets the border brush.
        /// </summary>
        public new Brush BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the label text.
        /// </summary>
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
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
        /// Initializes a new instance of the <see cref="LinearProgressIndicator"/> class.
        /// </summary>
        public LinearProgressIndicator()
        {
            InitializeComponent();
            UpdateProgressBar(Progress);
            UpdateProgressText(Progress);
        }

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LinearProgressIndicator)d;
            var newValue = (double)e.NewValue;
            
            // Clamp the value between 0 and 1
            newValue = Math.Max(0, Math.Min(1, newValue));
            
            control.UpdateProgressBar(newValue);
            control.UpdateProgressText(newValue);
        }

        private static void OnProgressBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LinearProgressIndicator)d;
            var newValue = (Brush)e.NewValue;
            
            if (newValue != null)
            {
                control.ProgressBorder.Background = newValue;
                control.ProgressBorder.BorderBrush = newValue;
            }
        }

        private static void OnBackgroundBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LinearProgressIndicator)d;
            var newValue = (Brush)e.NewValue;
            
            if (newValue != null)
            {
                control.BackgroundBorder.Background = newValue;
            }
        }

        private static void OnBorderBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LinearProgressIndicator)d;
            var newValue = (Brush)e.NewValue;
            
            if (newValue != null)
            {
                control.BackgroundBorder.BorderBrush = newValue;
            }
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LinearProgressIndicator)d;
            var newValue = (string)e.NewValue;
            
            control.LabelText.Text = newValue;
        }

        private static void OnShowPercentageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LinearProgressIndicator)d;
            var newValue = (bool)e.NewValue;
            
            control.ProgressText.Visibility = newValue ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateProgressBar(double progress)
        {
            // Update the progress bar width
            ProgressBorder.Width = progress * ActualWidth;
        }

        private void UpdateProgressText(double progress)
        {
            // Update the progress text
            int percentage = (int)(progress * 100);
            ProgressText.Text = $"{percentage}%";
        }

        /// <summary>
        /// Called when the size of the control changes.
        /// </summary>
        /// <param name="sizeInfo">Size change information.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            
            // Update the progress bar width when the control size changes
            UpdateProgressBar(Progress);
        }
    }
}
