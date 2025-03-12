using System;
using System.Collections.Generic;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Phase;

namespace Prosim2GSX.UI.EFB.Notifications
{
    /// <summary>
    /// Interface for the notification service.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Gets the active notifications.
        /// </summary>
        IReadOnlyList<Notification> ActiveNotifications { get; }
        
        /// <summary>
        /// Gets the notification history.
        /// </summary>
        IReadOnlyList<Notification> NotificationHistory { get; }
        
        /// <summary>
        /// Event raised when a notification is shown.
        /// </summary>
        event EventHandler<NotificationEventArgs> NotificationShown;
        
        /// <summary>
        /// Event raised when a notification is dismissed.
        /// </summary>
        event EventHandler<NotificationEventArgs> NotificationDismissed;
        
        /// <summary>
        /// Initializes the notification service.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Registers a notification.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <param name="type">The type of the notification.</param>
        /// <param name="message">The message for the notification.</param>
        /// <param name="action">The action to execute when the notification is clicked.</param>
        /// <returns>The registered notification.</returns>
        Notification RegisterNotification(string id, NotificationType type, string message, Action action = null);
        
        /// <summary>
        /// Shows a notification.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <returns>True if the notification was shown, false otherwise.</returns>
        bool ShowNotification(string id);
        
        /// <summary>
        /// Shows a notification.
        /// </summary>
        /// <param name="notification">The notification to show.</param>
        /// <returns>True if the notification was shown, false otherwise.</returns>
        bool ShowNotification(Notification notification);
        
        /// <summary>
        /// Dismisses a notification.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <returns>True if the notification was dismissed, false otherwise.</returns>
        bool DismissNotification(string id);
        
        /// <summary>
        /// Dismisses a notification.
        /// </summary>
        /// <param name="notification">The notification to dismiss.</param>
        /// <returns>True if the notification was dismissed, false otherwise.</returns>
        bool DismissNotification(Notification notification);
        
        /// <summary>
        /// Clears all active notifications.
        /// </summary>
        void ClearNotifications();
        
        /// <summary>
        /// Clears the notification history.
        /// </summary>
        void ClearHistory();
        
        /// <summary>
        /// Gets a notification by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <returns>The notification, or null if not found.</returns>
        Notification GetNotification(string id);
        
        /// <summary>
        /// Checks if a notification is active.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <returns>True if the notification is active, false otherwise.</returns>
        bool IsNotificationActive(string id);
        
        /// <summary>
        /// Shows notifications for a specific phase context.
        /// </summary>
        /// <param name="context">The phase context.</param>
        void ShowNotificationsForPhaseContext(PhaseContext context);
    }
    
    /// <summary>
    /// Event arguments for notification events.
    /// </summary>
    public class NotificationEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets the notification.
        /// </summary>
        public Notification Notification { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="timestamp">The timestamp when the event occurred.</param>
        public NotificationEventArgs(Notification notification, DateTime timestamp)
            : base() // Call the base constructor
        {
            Notification = notification ?? throw new ArgumentNullException(nameof(notification));
            // Timestamp is already set by the base constructor
        }
    }
    
    /// <summary>
    /// Represents a notification.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Gets the unique identifier for the notification.
        /// </summary>
        public string Id { get; }
        
        /// <summary>
        /// Gets the type of the notification.
        /// </summary>
        public NotificationType Type { get; }
        
        /// <summary>
        /// Gets the message for the notification.
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// Gets the action to execute when the notification is clicked.
        /// </summary>
        public Action Action { get; }
        
        /// <summary>
        /// Gets or sets the timestamp when the notification was shown.
        /// </summary>
        public DateTime? ShownAt { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the notification was dismissed.
        /// </summary>
        public DateTime? DismissedAt { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the notification is active.
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the notification has been read.
        /// </summary>
        public bool IsRead { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the notification should be automatically dismissed.
        /// </summary>
        public DateTime? AutoDismissAt { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <param name="type">The type of the notification.</param>
        /// <param name="message">The message for the notification.</param>
        /// <param name="action">The action to execute when the notification is clicked.</param>
        public Notification(string id, NotificationType type, string message, Action action = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Type = type;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Action = action;
        }
        
        /// <summary>
        /// Executes the notification action.
        /// </summary>
        public void ExecuteAction()
        {
            Action?.Invoke();
        }
    }
}
