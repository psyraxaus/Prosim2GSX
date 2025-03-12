using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Prosim2GSX.UI.EFB.Notifications;
// Using alias to avoid ambiguity
using PhaseNS = Prosim2GSX.UI.EFB.Phase;

namespace Prosim2GSX.UI.EFB.Controls
{
    /// <summary>
    /// Interaction logic for NotificationControl.xaml
    /// </summary>
    public partial class NotificationControl : UserControl
    {
        /// <summary>
        /// Dependency property for Notification.
        /// </summary>
        public static readonly DependencyProperty NotificationProperty =
            DependencyProperty.Register("Notification", typeof(Notification), typeof(NotificationControl),
                new PropertyMetadata(null));
        
        /// <summary>
        /// Event raised when the notification is dismissed.
        /// </summary>
        public event EventHandler<Notification> NotificationDismissed;
        
        /// <summary>
        /// Gets or sets the notification.
        /// </summary>
        public Notification Notification
        {
            get { return (Notification)GetValue(NotificationProperty); }
            set { SetValue(NotificationProperty, value); }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationControl"/> class.
        /// </summary>
        public NotificationControl()
        {
            InitializeComponent();
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Mark the notification as read
            if (Notification != null)
            {
                Notification.IsRead = true;
                
                // Raise the NotificationDismissed event
                NotificationDismissed?.Invoke(this, Notification);
            }
        }
    }
    
    /// <summary>
    /// Converter that converts a NotificationType to a background brush.
    /// </summary>
    public class NotificationTypeToBackgroundConverter : IValueConverter
    {
        /// <summary>
        /// Converts a NotificationType to a background brush.
        /// </summary>
        /// <param name="value">The NotificationType to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A background brush for the specified NotificationType.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Notifications.NotificationType type)
            {
                switch (type)
                {
                    case Notifications.NotificationType.Info:
                        return new SolidColorBrush(Color.FromRgb(0x1E, 0x88, 0xE5)); // Blue
                    case Notifications.NotificationType.Warning:
                        return new SolidColorBrush(Color.FromRgb(0xFF, 0xA0, 0x00)); // Amber
                    case Notifications.NotificationType.Action:
                        return new SolidColorBrush(Color.FromRgb(0x7C, 0xB3, 0x42)); // Green
                    case Notifications.NotificationType.Success:
                        return new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47)); // Green
                    case Notifications.NotificationType.Error:
                        return new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)); // Red
                    default:
                        return new SolidColorBrush(Color.FromRgb(0x42, 0x42, 0x42)); // Grey
                }
            }
            
            return new SolidColorBrush(Color.FromRgb(0x42, 0x42, 0x42)); // Grey
        }
        
        /// <summary>
        /// Converts a background brush to a NotificationType.
        /// </summary>
        /// <param name="value">The background brush to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A NotificationType for the specified background brush.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// Converter that converts a NotificationType to an icon.
    /// </summary>
    public class NotificationTypeToIconConverter : IValueConverter
    {
        /// <summary>
        /// Converts a NotificationType to an icon.
        /// </summary>
        /// <param name="value">The NotificationType to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>An icon for the specified NotificationType.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Notifications.NotificationType type)
            {
                switch (type)
                {
                    case Notifications.NotificationType.Info:
                        return "\uE946"; // Info
                    case Notifications.NotificationType.Warning:
                        return "\uE7BA"; // Warning
                    case Notifications.NotificationType.Action:
                        return "\uE8FB"; // Action
                    case Notifications.NotificationType.Success:
                        return "\uE930"; // Success
                    case Notifications.NotificationType.Error:
                        return "\uE783"; // Error
                    default:
                        return "\uE946"; // Info
                }
            }
            
            return "\uE946"; // Info
        }
        
        /// <summary>
        /// Converts an icon to a NotificationType.
        /// </summary>
        /// <param name="value">The icon to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A NotificationType for the specified icon.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// Converter that converts a NotificationType to a foreground brush.
    /// </summary>
    public class NotificationTypeToForegroundConverter : IValueConverter
    {
        /// <summary>
        /// Converts a NotificationType to a foreground brush.
        /// </summary>
        /// <param name="value">The NotificationType to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A foreground brush for the specified NotificationType.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // All notification types use white foreground
            return new SolidColorBrush(Colors.White);
        }
        
        /// <summary>
        /// Converts a foreground brush to a NotificationType.
        /// </summary>
        /// <param name="value">The foreground brush to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A NotificationType for the specified foreground brush.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
