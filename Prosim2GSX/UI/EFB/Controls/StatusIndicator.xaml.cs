using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Prosim2GSX.UI.EFB.Controls
{
    /// <summary>
    /// Interaction logic for StatusIndicator.xaml
    /// </summary>
    public partial class StatusIndicator : UserControl
    {
        /// <summary>
        /// Enum for status types.
        /// </summary>
        public enum StatusType
        {
            /// <summary>
            /// Success status.
            /// </summary>
            Success,
            
            /// <summary>
            /// Warning status.
            /// </summary>
            Warning,
            
            /// <summary>
            /// Error status.
            /// </summary>
            Error,
            
            /// <summary>
            /// Info status.
            /// </summary>
            Info,
            
            /// <summary>
            /// Inactive status.
            /// </summary>
            Inactive
        }
        
        /// <summary>
        /// Dependency property for Label.
        /// </summary>
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(StatusIndicator),
                new PropertyMetadata("Status", OnLabelChanged));
        
        /// <summary>
        /// Dependency property for Status.
        /// </summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(StatusType), typeof(StatusIndicator),
                new PropertyMetadata(StatusType.Success, OnStatusChanged));
        
        /// <summary>
        /// Dependency property for StatusMessage.
        /// </summary>
        public static readonly DependencyProperty StatusMessageProperty =
            DependencyProperty.Register("StatusMessage", typeof(string), typeof(StatusIndicator),
                new PropertyMetadata("Connected", OnStatusMessageChanged));
        
        /// <summary>
        /// Gets or sets the label text.
        /// </summary>
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public StatusType Status
        {
            get { return (StatusType)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        public string StatusMessage
        {
            get { return (string)GetValue(StatusMessageProperty); }
            set { SetValue(StatusMessageProperty, value); }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusIndicator"/> class.
        /// </summary>
        public StatusIndicator()
        {
            InitializeComponent();
            UpdateStatus(Status);
        }
        
        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (StatusIndicator)d;
            var newValue = (string)e.NewValue;
            
            control.LabelText.Text = newValue;
        }
        
        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (StatusIndicator)d;
            var newValue = (StatusType)e.NewValue;
            
            control.UpdateStatus(newValue);
        }
        
        private static void OnStatusMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (StatusIndicator)d;
            var newValue = (string)e.NewValue;
            
            control.StatusText.Text = newValue;
        }
        
        private void UpdateStatus(StatusType status)
        {
            switch (status)
            {
                case StatusType.Success:
                    StatusEllipse.Fill = FindResource("EFBSuccessBrush") as SolidColorBrush;
                    StatusText.Foreground = FindResource("EFBSuccessBrush") as SolidColorBrush;
                    break;
                case StatusType.Warning:
                    StatusEllipse.Fill = FindResource("EFBWarningBrush") as SolidColorBrush;
                    StatusText.Foreground = FindResource("EFBWarningBrush") as SolidColorBrush;
                    break;
                case StatusType.Error:
                    StatusEllipse.Fill = FindResource("EFBErrorBrush") as SolidColorBrush;
                    StatusText.Foreground = FindResource("EFBErrorBrush") as SolidColorBrush;
                    break;
                case StatusType.Info:
                    StatusEllipse.Fill = FindResource("EFBInfoBrush") as SolidColorBrush;
                    StatusText.Foreground = FindResource("EFBInfoBrush") as SolidColorBrush;
                    break;
                case StatusType.Inactive:
                    StatusEllipse.Fill = FindResource("EFBBorderBrush") as SolidColorBrush;
                    StatusText.Foreground = FindResource("EFBBorderBrush") as SolidColorBrush;
                    break;
            }
        }
    }
}
