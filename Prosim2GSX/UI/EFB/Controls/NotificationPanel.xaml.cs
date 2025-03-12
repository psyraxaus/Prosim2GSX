using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Prosim2GSX.UI.EFB.Notifications;

namespace Prosim2GSX.UI.EFB.Controls
{
    /// <summary>
    /// Interaction logic for NotificationPanel.xaml
    /// </summary>
    public partial class NotificationPanel : UserControl, INotifyPropertyChanged
    {
        private readonly INotificationService _notificationService;
        private readonly List<NotificationControl> _notificationControls = new List<NotificationControl>();
        
        /// <summary>
        /// Event raised when a property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// Gets a value indicating whether there are any notifications.
        /// </summary>
        public bool HasNotifications => _notificationControls.Count > 0;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationPanel"/> class.
        /// </summary>
        /// <param name="notificationService">The notification service.</param>
        public NotificationPanel(INotificationService notificationService)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            
            InitializeComponent();
            
            // Subscribe to notification events
            _notificationService.NotificationShown += OnNotificationShown;
            _notificationService.NotificationDismissed += OnNotificationDismissed;
            
            // Add existing notifications
            foreach (var notification in _notificationService.ActiveNotifications)
            {
                AddNotificationControl(notification);
            }
        }
        
        private void OnNotificationShown(object sender, NotificationEventArgs e)
        {
            // Add the notification to the panel
            AddNotificationControl(e.Notification);
        }
        
        private void OnNotificationDismissed(object sender, NotificationEventArgs e)
        {
            // Remove the notification from the panel
            RemoveNotificationControl(e.Notification);
        }
        
        private void AddNotificationControl(Notification notification)
        {
            // Create a new notification control
            var control = new NotificationControl
            {
                Notification = notification
            };
            
            // Subscribe to the notification dismissed event
            control.NotificationDismissed += OnNotificationControlDismissed;
            
            // Add the control to the panel
            NotificationsPanel.Children.Insert(0, control);
            
            // Add the control to the list
            _notificationControls.Add(control);
            
            // Update the HasNotifications property
            OnPropertyChanged(nameof(HasNotifications));
        }
        
        private void RemoveNotificationControl(Notification notification)
        {
            // Find the control for the notification
            var control = _notificationControls.Find(c => c.Notification == notification);
            if (control != null)
            {
                // Remove the control from the panel
                NotificationsPanel.Children.Remove(control);
                
                // Remove the control from the list
                _notificationControls.Remove(control);
                
                // Update the HasNotifications property
                OnPropertyChanged(nameof(HasNotifications));
            }
        }
        
        private void OnNotificationControlDismissed(object sender, Notification notification)
        {
            // Dismiss the notification
            _notificationService.DismissNotification(notification);
        }
        
        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear all notifications
            _notificationService.ClearNotifications();
        }
        
        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    /// <summary>
    /// Converter that converts a boolean value to a Visibility value.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a Visibility value.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A Visibility value for the specified boolean value.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }
        
        /// <summary>
        /// Converts a Visibility value to a boolean value.
        /// </summary>
        /// <param name="value">The Visibility value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A boolean value for the specified Visibility value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }
    }
    
    /// <summary>
    /// Converter that converts a boolean value to a Visibility value, with the opposite result.
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a Visibility value, with the opposite result.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A Visibility value for the specified boolean value, with the opposite result.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }
        
        /// <summary>
        /// Converts a Visibility value to a boolean value, with the opposite result.
        /// </summary>
        /// <param name="value">The Visibility value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A boolean value for the specified Visibility value, with the opposite result.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (Visibility)value != Visibility.Visible;
        }
    }
}
