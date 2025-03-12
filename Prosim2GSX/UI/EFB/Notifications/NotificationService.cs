using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Phase;

namespace Prosim2GSX.UI.EFB.Notifications
{
    /// <summary>
    /// Service for managing notifications.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IPhaseContextService _phaseContextService;
        
        private readonly Dictionary<string, Notification> _notifications = new Dictionary<string, Notification>();
        private readonly List<Notification> _activeNotifications = new List<Notification>();
        private readonly List<Notification> _notificationHistory = new List<Notification>();
        
        private readonly Timer _autoDismissTimer;
        private const int AutoDismissCheckInterval = 1000; // 1 second
        
        /// <summary>
        /// Gets the active notifications.
        /// </summary>
        public IReadOnlyList<Notification> ActiveNotifications => _activeNotifications.AsReadOnly();
        
        /// <summary>
        /// Gets the notification history.
        /// </summary>
        public IReadOnlyList<Notification> NotificationHistory => _notificationHistory.AsReadOnly();
        
        /// <summary>
        /// Event raised when a notification is shown.
        /// </summary>
        public event EventHandler<NotificationEventArgs> NotificationShown;
        
        /// <summary>
        /// Event raised when a notification is dismissed.
        /// </summary>
        public event EventHandler<NotificationEventArgs> NotificationDismissed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService"/> class.
        /// </summary>
        /// <param name="eventAggregator">The event aggregator.</param>
        /// <param name="phaseContextService">The phase context service.</param>
        public NotificationService(IEventAggregator eventAggregator, IPhaseContextService phaseContextService)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _phaseContextService = phaseContextService ?? throw new ArgumentNullException(nameof(phaseContextService));
            
            // Initialize auto-dismiss timer
            _autoDismissTimer = new Timer(AutoDismissCheckInterval);
            _autoDismissTimer.Elapsed += OnAutoDismissTimerElapsed;
        }
        
        /// <summary>
        /// Initializes the notification service.
        /// </summary>
        public void Initialize()
        {
            // Subscribe to phase context changes
            _phaseContextService.ContextChanged += OnPhaseContextChanged;
            
            // Start the auto-dismiss timer
            _autoDismissTimer.Start();
            
            Logger.Log(LogLevel.Information, "NotificationService:Initialize", "Notification service initialized");
        }
        
        /// <summary>
        /// Registers a notification.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <param name="type">The type of the notification.</param>
        /// <param name="message">The message for the notification.</param>
        /// <param name="action">The action to execute when the notification is clicked.</param>
        /// <returns>The registered notification.</returns>
        public Notification RegisterNotification(string id, NotificationType type, string message, Action action = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
                
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));
                
            // Create the notification
            var notification = new Notification(id, type, message, action);
            
            // Register the notification
            _notifications[id] = notification;
            
            Logger.Log(LogLevel.Information, "NotificationService:RegisterNotification", 
                $"Registered notification: {id}");
                
            return notification;
        }
        
        /// <summary>
        /// Shows a notification.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <returns>True if the notification was shown, false otherwise.</returns>
        public bool ShowNotification(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
                
            // Find the notification
            if (!_notifications.TryGetValue(id, out var notification))
            {
                Logger.Log(LogLevel.Warning, "NotificationService:ShowNotification", 
                    $"Notification not found: {id}");
                return false;
            }
            
            return ShowNotification(notification);
        }
        
        /// <summary>
        /// Shows a notification.
        /// </summary>
        /// <param name="notification">The notification to show.</param>
        /// <returns>True if the notification was shown, false otherwise.</returns>
        public bool ShowNotification(Notification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));
                
            // Check if the notification is already active
            if (notification.IsActive)
                return true;
                
            // Show the notification
            notification.IsActive = true;
            notification.ShownAt = DateTime.Now;
            notification.IsRead = false;
            
            // Add to active notifications
            _activeNotifications.Add(notification);
            
            // Raise event
            OnNotificationShown(notification);
            
            Logger.Log(LogLevel.Information, "NotificationService:ShowNotification", 
                $"Showed notification: {notification.Id}");
                
            return true;
        }
        
        /// <summary>
        /// Dismisses a notification.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <returns>True if the notification was dismissed, false otherwise.</returns>
        public bool DismissNotification(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
                
            // Find the notification
            if (!_notifications.TryGetValue(id, out var notification))
            {
                Logger.Log(LogLevel.Warning, "NotificationService:DismissNotification", 
                    $"Notification not found: {id}");
                return false;
            }
            
            return DismissNotification(notification);
        }
        
        /// <summary>
        /// Dismisses a notification.
        /// </summary>
        /// <param name="notification">The notification to dismiss.</param>
        /// <returns>True if the notification was dismissed, false otherwise.</returns>
        public bool DismissNotification(Notification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));
                
            // Check if the notification is active
            if (!notification.IsActive)
                return false;
                
            // Dismiss the notification
            notification.IsActive = false;
            notification.DismissedAt = DateTime.Now;
            
            // Remove from active notifications
            _activeNotifications.Remove(notification);
            
            // Add to notification history
            _notificationHistory.Add(notification);
            
            // Raise event
            OnNotificationDismissed(notification);
            
            Logger.Log(LogLevel.Information, "NotificationService:DismissNotification", 
                $"Dismissed notification: {notification.Id}");
                
            return true;
        }
        
        /// <summary>
        /// Clears all active notifications.
        /// </summary>
        public void ClearNotifications()
        {
            // Dismiss all active notifications
            foreach (var notification in _activeNotifications.ToList())
            {
                DismissNotification(notification);
            }
            
            Logger.Log(LogLevel.Information, "NotificationService:ClearNotifications", 
                "Cleared all active notifications");
        }
        
        /// <summary>
        /// Clears the notification history.
        /// </summary>
        public void ClearHistory()
        {
            _notificationHistory.Clear();
            
            Logger.Log(LogLevel.Information, "NotificationService:ClearHistory", 
                "Cleared notification history");
        }
        
        /// <summary>
        /// Gets a notification by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <returns>The notification, or null if not found.</returns>
        public Notification GetNotification(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
                
            // Find the notification
            if (_notifications.TryGetValue(id, out var notification))
                return notification;
                
            return null;
        }
        
        /// <summary>
        /// Checks if a notification is active.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <returns>True if the notification is active, false otherwise.</returns>
        public bool IsNotificationActive(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
                
            // Find the notification
            if (_notifications.TryGetValue(id, out var notification))
                return notification.IsActive;
                
            return false;
        }
        
        /// <summary>
        /// Shows notifications for a specific phase context.
        /// </summary>
        /// <param name="context">The phase context.</param>
        public void ShowNotificationsForPhaseContext(PhaseContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
                
            // Show notifications for the phase context
            foreach (var phaseNotification in context.Notifications)
            {
                // Map the Phase.NotificationType to Notifications.NotificationType
                Phase.NotificationType phaseType = phaseNotification.Type;
                NotificationType notificationType = MapNotificationType(phaseType);
                
                // Register the notification if it doesn't exist
                if (!_notifications.ContainsKey(phaseNotification.Id))
                {
                    RegisterNotification(phaseNotification.Id, notificationType, phaseNotification.Message, phaseNotification.Action);
                }
                
                // Show the notification
                ShowNotification(phaseNotification.Id);
                
                // Set auto-dismiss if specified
                if (phaseNotification.DismissAfter.HasValue)
                {
                    var autoDismissAt = DateTime.Now.Add(phaseNotification.DismissAfter.Value);
                    var registeredNotification = _notifications[phaseNotification.Id];
                    registeredNotification.AutoDismissAt = autoDismissAt;
                }
            }
        }
        
        /// <summary>
        /// Maps a Phase.NotificationType to a Notifications.NotificationType
        /// </summary>
        /// <param name="phaseType">The Phase.NotificationType to map</param>
        /// <returns>The corresponding Notifications.NotificationType</returns>
        private NotificationType MapNotificationType(Phase.NotificationType phaseType)
        {
            return phaseType switch
            {
                Phase.NotificationType.Info => NotificationType.Information,
                Phase.NotificationType.Warning => NotificationType.Warning,
                Phase.NotificationType.Action => NotificationType.Information, // Map Action to Information
                Phase.NotificationType.Success => NotificationType.Success,
                Phase.NotificationType.Error => NotificationType.Error,
                _ => NotificationType.Information
            };
        }
        
        private void OnPhaseContextChanged(object sender, PhaseContextChangedEventArgs e)
        {
            // Show notifications for the new phase context
            ShowNotificationsForPhaseContext(e.NewContext);
        }
        
        private void OnAutoDismissTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            
            // Check for notifications that should be auto-dismissed
            foreach (var notification in _activeNotifications.ToList())
            {
                if (notification.AutoDismissAt.HasValue && now >= notification.AutoDismissAt.Value)
                {
                    DismissNotification(notification);
                }
            }
        }
        
        protected virtual void OnNotificationShown(Notification notification)
        {
            var args = new NotificationEventArgs(notification, DateTime.Now);
            
            NotificationShown?.Invoke(this, args);
            
            // Also publish through event aggregator
            _eventAggregator.Publish(args);
        }
        
        protected virtual void OnNotificationDismissed(Notification notification)
        {
            var args = new NotificationEventArgs(notification, DateTime.Now);
            
            NotificationDismissed?.Invoke(this, args);
            
            // Also publish through event aggregator
            _eventAggregator.Publish(args);
        }
    }
}
