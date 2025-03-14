using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Prosim2GSX.UI.EFB.Themes;

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
            
            // Subscribe to theme changed events
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Find the theme manager and subscribe to theme changed events
            if (Application.Current.Resources.Contains("ThemeManager") && 
                Application.Current.Resources["ThemeManager"] is EFBThemeManager themeManager)
            {
                themeManager.ThemeChanged += OnThemeChanged;
            }
        }
        
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from theme changed events
            if (Application.Current.Resources.Contains("ThemeManager") && 
                Application.Current.Resources["ThemeManager"] is EFBThemeManager themeManager)
            {
                themeManager.ThemeChanged -= OnThemeChanged;
            }
        }
        
        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            // Update the status when the theme changes
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
            try
            {
                // Use the status-specific text resources if available
                switch (status)
                {
                    case StatusType.Success:
                        StatusEllipse.Fill = TryFindResource("EFBSuccessBrush") as SolidColorBrush 
                            ?? TryFindResource("EFBStatusSuccessTextBrush") as SolidColorBrush 
                            ?? new SolidColorBrush(Colors.Green);
                        
                        StatusText.Foreground = TryFindResource("EFBStatusSuccessTextBrush") as SolidColorBrush 
                            ?? TryFindResource("EFBSuccessBrush") as SolidColorBrush 
                            ?? new SolidColorBrush(Colors.Green);
                        break;
                        
                    case StatusType.Warning:
                        StatusEllipse.Fill = TryFindResource("EFBWarningBrush") as SolidColorBrush 
                            ?? TryFindResource("EFBStatusWarningTextBrush") as SolidColorBrush 
                            ?? new SolidColorBrush(Colors.Orange);
                        
                        StatusText.Foreground = TryFindResource("EFBStatusWarningTextBrush") as SolidColorBrush 
                            ?? TryFindResource("EFBWarningBrush") as SolidColorBrush 
                            ?? new SolidColorBrush(Colors.Orange);
                        break;
                        
                    case StatusType.Error:
                        StatusEllipse.Fill = TryFindResource("EFBErrorBrush") as SolidColorBrush 
                            ?? TryFindResource("EFBStatusErrorTextBrush") as SolidColorBrush 
                            ?? new SolidColorBrush(Colors.Red);
                        
                        StatusText.Foreground = TryFindResource("EFBStatusErrorTextBrush") as SolidColorBrush 
                            ?? TryFindResource("EFBErrorBrush") as SolidColorBrush 
                            ?? new SolidColorBrush(Colors.Red);
                        break;
                        
                    case StatusType.Info:
                        StatusEllipse.Fill = TryFindResource("EFBInfoBrush") as SolidColorBrush 
                            ?? TryFindResource("EFBStatusInfoTextBrush") as SolidColorBrush 
                            ?? new SolidColorBrush(Colors.Blue);
                        
                        StatusText.Foreground = TryFindResource("EFBStatusInfoTextBrush") as SolidColorBrush 
                            ?? TryFindResource("EFBInfoBrush") as SolidColorBrush 
                            ?? new SolidColorBrush(Colors.Blue);
                        break;
                        
                    case StatusType.Inactive:
                        StatusEllipse.Fill = TryFindResource("EFBBorderBrush") as SolidColorBrush 
                            ?? TryFindResource("EFBStatusInactiveTextBrush") as SolidColorBrush 
                            ?? new SolidColorBrush(Colors.Gray);
                        
                        StatusText.Foreground = TryFindResource("EFBStatusInactiveTextBrush") as SolidColorBrush 
                            ?? TryFindResource("EFBBorderBrush") as SolidColorBrush 
                            ?? new SolidColorBrush(Colors.Gray);
                        break;
                }
                
                // Ensure label text has appropriate foreground color
                LabelText.Foreground = TryFindResource("EFBForegroundBrush") as SolidColorBrush 
                    ?? TryFindResource("EFBTextPrimaryBrush") as SolidColorBrush 
                    ?? new SolidColorBrush(Colors.Black);
            }
            catch
            {
                // Fallback to default colors if resources can't be found
                switch (status)
                {
                    case StatusType.Success:
                        StatusEllipse.Fill = new SolidColorBrush(Colors.Green);
                        StatusText.Foreground = new SolidColorBrush(Colors.Green);
                        break;
                    case StatusType.Warning:
                        StatusEllipse.Fill = new SolidColorBrush(Colors.Orange);
                        StatusText.Foreground = new SolidColorBrush(Colors.Orange);
                        break;
                    case StatusType.Error:
                        StatusEllipse.Fill = new SolidColorBrush(Colors.Red);
                        StatusText.Foreground = new SolidColorBrush(Colors.Red);
                        break;
                    case StatusType.Info:
                        StatusEllipse.Fill = new SolidColorBrush(Colors.Blue);
                        StatusText.Foreground = new SolidColorBrush(Colors.Blue);
                        break;
                    case StatusType.Inactive:
                        StatusEllipse.Fill = new SolidColorBrush(Colors.Gray);
                        StatusText.Foreground = new SolidColorBrush(Colors.Gray);
                        break;
                }
                
                // Fallback for label text
                LabelText.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
    }
}
