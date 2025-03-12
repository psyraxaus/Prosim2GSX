using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Prosim2GSX.UI.EFB.Notifications
{
    /// <summary>
    /// Provides toast notification functionality for the EFB UI.
    /// </summary>
    public class ToastNotificationService
    {
        private static readonly ToastNotificationService _instance = new();
        private readonly Dictionary<string, ToastNotification> _activeNotifications = new();
        private readonly Queue<ToastNotification> _pendingNotifications = new();
        private readonly object _lock = new();
        private readonly int _maxActiveNotifications = 3;
        private Panel _notificationContainer;

        /// <summary>
        /// Gets the singleton instance of the ToastNotificationService.
        /// </summary>
        public static ToastNotificationService Instance => _instance;

        /// <summary>
        /// Initializes a new instance of the ToastNotificationService class.
        /// </summary>
        private ToastNotificationService()
        {
        }

        /// <summary>
        /// Initializes the notification service with a container panel.
        /// </summary>
        /// <param name="container">The panel that will contain the notifications.</param>
        public void Initialize(Panel container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            _notificationContainer = container;
        }

        /// <summary>
        /// Shows a toast notification.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the notification.</param>
        /// <param name="type">The type of notification.</param>
        /// <param name="duration">The duration in seconds to display the notification.</param>
        /// <returns>The ID of the notification.</returns>
        public string ShowNotification(string message, string title = null, NotificationType type = NotificationType.Information, double duration = 5.0)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));

            if (_notificationContainer == null)
                throw new InvalidOperationException("The notification service has not been initialized.");

            // Create a new notification
            var notification = new ToastNotification
            {
                Id = Guid.NewGuid().ToString(),
                Message = message,
                Title = title,
                Type = type,
                Duration = TimeSpan.FromSeconds(duration),
                Control = CreateNotificationControl(message, title, type)
            };

            // Add the notification to the queue
            lock (_lock)
            {
                _pendingNotifications.Enqueue(notification);
                ProcessPendingNotifications();
            }

            return notification.Id;
        }

        /// <summary>
        /// Dismisses a notification.
        /// </summary>
        /// <param name="id">The ID of the notification to dismiss.</param>
        public void DismissNotification(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty.", nameof(id));

            lock (_lock)
            {
                if (_activeNotifications.TryGetValue(id, out var notification))
                {
                    HideNotification(notification);
                }
            }
        }

        /// <summary>
        /// Dismisses all active notifications.
        /// </summary>
        public void DismissAllNotifications()
        {
            lock (_lock)
            {
                foreach (var notification in _activeNotifications.Values)
                {
                    HideNotification(notification);
                }

                _pendingNotifications.Clear();
            }
        }

        /// <summary>
        /// Updates an existing notification.
        /// </summary>
        /// <param name="id">The ID of the notification to update.</param>
        /// <param name="message">The new message.</param>
        /// <param name="title">The new title.</param>
        /// <param name="type">The new type.</param>
        /// <param name="duration">The new duration in seconds.</param>
        /// <returns>True if the notification was updated, false otherwise.</returns>
        public bool UpdateNotification(string id, string message, string title = null, NotificationType? type = null, double? duration = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty.", nameof(id));

            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));

            lock (_lock)
            {
                if (_activeNotifications.TryGetValue(id, out var notification))
                {
                    // Update the notification
                    notification.Message = message;
                    notification.Title = title ?? notification.Title;
                    notification.Type = type ?? notification.Type;
                    notification.Duration = duration.HasValue ? TimeSpan.FromSeconds(duration.Value) : notification.Duration;

                    // Update the control
                    UpdateNotificationControl(notification);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Processes pending notifications.
        /// </summary>
        private void ProcessPendingNotifications()
        {
            lock (_lock)
            {
                while (_pendingNotifications.Count > 0 && _activeNotifications.Count < _maxActiveNotifications)
                {
                    var notification = _pendingNotifications.Dequeue();
                    ShowNotification(notification);
                }
            }
        }

        /// <summary>
        /// Shows a notification.
        /// </summary>
        /// <param name="notification">The notification to show.</param>
        private void ShowNotification(ToastNotification notification)
        {
            lock (_lock)
            {
                // Add the notification to the active list
                _activeNotifications[notification.Id] = notification;

                // Add the control to the container
                _notificationContainer.Dispatcher.Invoke(() =>
                {
                    _notificationContainer.Children.Add(notification.Control);

                    // Start the show animation
                    var animation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };

                    notification.Control.BeginAnimation(UIElement.OpacityProperty, animation);

                    // Set up the timer to hide the notification
                    var timer = new DispatcherTimer
                    {
                        Interval = notification.Duration
                    };

                    timer.Tick += (sender, e) =>
                    {
                        timer.Stop();
                        HideNotification(notification);
                    };

                    timer.Start();
                });
            }
        }

        /// <summary>
        /// Hides a notification.
        /// </summary>
        /// <param name="notification">The notification to hide.</param>
        private void HideNotification(ToastNotification notification)
        {
            lock (_lock)
            {
                // Remove the notification from the active list
                _activeNotifications.Remove(notification.Id);

                // Start the hide animation
                _notificationContainer.Dispatcher.Invoke(() =>
                {
                    var animation = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };

                    animation.Completed += (sender, e) =>
                    {
                        // Remove the control from the container
                        _notificationContainer.Children.Remove(notification.Control);

                        // Process any pending notifications
                        ProcessPendingNotifications();
                    };

                    notification.Control.BeginAnimation(UIElement.OpacityProperty, animation);
                });
            }
        }

        /// <summary>
        /// Creates a notification control.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the notification.</param>
        /// <param name="type">The type of notification.</param>
        /// <returns>A new notification control.</returns>
        private UIElement CreateNotificationControl(string message, string title, NotificationType type)
        {
            // Create the notification control
            var border = new Border
            {
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(10),
                Background = GetBackgroundBrush(type),
                BorderBrush = GetBorderBrush(type),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    ShadowDepth = 2,
                    Opacity = 0.3,
                    BlurRadius = 5
                },
                Opacity = 0
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Add the icon
            var iconPath = new System.Windows.Shapes.Path
            {
                Data = GetIconGeometry(type),
                Fill = GetIconBrush(type),
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var iconContainer = new Border
            {
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 10, 0)
            };

            iconContainer.Child = iconPath;
            Grid.SetColumn(iconContainer, 0);
            grid.Children.Add(iconContainer);

            // Add the content
            var contentPanel = new StackPanel
            {
                Margin = new Thickness(0)
            };

            if (!string.IsNullOrEmpty(title))
            {
                var titleTextBlock = new TextBlock
                {
                    Text = title,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap
                };

                contentPanel.Children.Add(titleTextBlock);
            }

            var messageTextBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap
            };

            contentPanel.Children.Add(messageTextBlock);

            Grid.SetColumn(contentPanel, 1);
            grid.Children.Add(contentPanel);

            // Add the close button
            var closeButton = new Button
            {
                Content = "×",
                FontSize = 16,
                Padding = new Thickness(5, 0, 5, 0),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = GetIconBrush(type)
            };

            closeButton.Click += (sender, e) =>
            {
                // Find the notification ID
                lock (_lock)
                {
                    foreach (var kvp in _activeNotifications)
                    {
                        if (kvp.Value.Control == border)
                        {
                            DismissNotification(kvp.Key);
                            break;
                        }
                    }
                }
            };

            Grid.SetColumn(closeButton, 2);
            grid.Children.Add(closeButton);

            border.Child = grid;

            return border;
        }

        /// <summary>
        /// Updates a notification control.
        /// </summary>
        /// <param name="notification">The notification to update.</param>
        private void UpdateNotificationControl(ToastNotification notification)
        {
            _notificationContainer.Dispatcher.Invoke(() =>
            {
                // Get the border
                var border = notification.Control as Border;
                if (border == null)
                    return;

                // Update the background and border
                border.Background = GetBackgroundBrush(notification.Type);
                border.BorderBrush = GetBorderBrush(notification.Type);

                // Get the grid
                var grid = border.Child as Grid;
                if (grid == null)
                    return;

                // Update the icon
                var iconContainer = grid.Children[0] as Border;
                if (iconContainer != null)
                {
                    var iconPath = iconContainer.Child as System.Windows.Shapes.Path;
                    if (iconPath != null)
                    {
                        iconPath.Data = GetIconGeometry(notification.Type);
                        iconPath.Fill = GetIconBrush(notification.Type);
                    }
                }

                // Update the content
                var contentPanel = grid.Children[1] as StackPanel;
                if (contentPanel != null)
                {
                    if (contentPanel.Children.Count > 0)
                    {
                        if (contentPanel.Children.Count > 1 && !string.IsNullOrEmpty(notification.Title))
                        {
                            // Update the title
                            var titleTextBlock = contentPanel.Children[0] as TextBlock;
                            if (titleTextBlock != null)
                            {
                                titleTextBlock.Text = notification.Title;
                            }

                            // Update the message
                            var messageTextBlock = contentPanel.Children[1] as TextBlock;
                            if (messageTextBlock != null)
                            {
                                messageTextBlock.Text = notification.Message;
                            }
                        }
                        else
                        {
                            // Update the message
                            var messageTextBlock = contentPanel.Children[0] as TextBlock;
                            if (messageTextBlock != null)
                            {
                                messageTextBlock.Text = notification.Message;
                            }
                        }
                    }
                }

                // Update the close button
                var closeButton = grid.Children[2] as Button;
                if (closeButton != null)
                {
                    closeButton.Foreground = GetIconBrush(notification.Type);
                }
            });
        }

        /// <summary>
        /// Gets the background brush for a notification type.
        /// </summary>
        /// <param name="type">The notification type.</param>
        /// <returns>The background brush.</returns>
        private Brush GetBackgroundBrush(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new SolidColorBrush(Color.FromArgb(255, 223, 240, 216)),
                NotificationType.Warning => new SolidColorBrush(Color.FromArgb(255, 252, 248, 227)),
                NotificationType.Error => new SolidColorBrush(Color.FromArgb(255, 242, 222, 222)),
                NotificationType.Action => new SolidColorBrush(Color.FromArgb(255, 230, 237, 200)),
                _ => new SolidColorBrush(Color.FromArgb(255, 217, 237, 247))
            };
        }

        /// <summary>
        /// Gets the border brush for a notification type.
        /// </summary>
        /// <param name="type">The notification type.</param>
        /// <returns>The border brush.</returns>
        private Brush GetBorderBrush(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new SolidColorBrush(Color.FromArgb(255, 214, 233, 198)),
                NotificationType.Warning => new SolidColorBrush(Color.FromArgb(255, 250, 235, 204)),
                NotificationType.Error => new SolidColorBrush(Color.FromArgb(255, 235, 204, 209)),
                NotificationType.Action => new SolidColorBrush(Color.FromArgb(255, 210, 220, 180)),
                _ => new SolidColorBrush(Color.FromArgb(255, 188, 232, 241))
            };
        }

        /// <summary>
        /// Gets the icon brush for a notification type.
        /// </summary>
        /// <param name="type">The notification type.</param>
        /// <returns>The icon brush.</returns>
        private Brush GetIconBrush(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new SolidColorBrush(Color.FromArgb(255, 60, 118, 61)),
                NotificationType.Warning => new SolidColorBrush(Color.FromArgb(255, 138, 109, 59)),
                NotificationType.Error => new SolidColorBrush(Color.FromArgb(255, 169, 68, 66)),
                NotificationType.Action => new SolidColorBrush(Color.FromArgb(255, 124, 179, 66)),
                _ => new SolidColorBrush(Color.FromArgb(255, 49, 112, 143))
            };
        }

        /// <summary>
        /// Gets the icon geometry for a notification type.
        /// </summary>
        /// <param name="type">The notification type.</param>
        /// <returns>The icon geometry.</returns>
        private Geometry GetIconGeometry(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => Geometry.Parse("M0,8 L3,5 L7,9 L14,2 L16,4 L7,13 L0,8 Z"),
                NotificationType.Warning => Geometry.Parse("M8,0 L16,16 L0,16 L8,0 Z M8,4 L8,12 M8,13 L8,15"),
                NotificationType.Error => Geometry.Parse("M0,0 L16,16 M16,0 L0,16"),
                NotificationType.Action => Geometry.Parse("M8,0 L10,5 L16,6 L12,10 L13,16 L8,13 L3,16 L4,10 L0,6 L6,5 Z"),
                _ => Geometry.Parse("M8,0 C3.6,0 0,3.6 0,8 C0,12.4 3.6,16 8,16 C12.4,16 16,12.4 16,8 C16,3.6 12.4,0 8,0 Z M8,4 L8,12 M8,13 L8,15")
            };
        }
    }

    /// <summary>
    /// Represents a toast notification.
    /// </summary>
    internal class ToastNotification
    {
        /// <summary>
        /// Gets or sets the ID of the notification.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the message of the notification.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the title of the notification.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the type of the notification.
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// Gets or sets the duration of the notification.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the control of the notification.
        /// </summary>
        public UIElement Control { get; set; }
    }

    /// <summary>
    /// Defines the types of notifications.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Information notification.
        /// </summary>
        Information,
        
        /// <summary>
        /// Info notification (alias for Information).
        /// </summary>
        Info = Information,

        /// <summary>
        /// Success notification.
        /// </summary>
        Success,

        /// <summary>
        /// Warning notification.
        /// </summary>
        Warning,

        /// <summary>
        /// Error notification.
        /// </summary>
        Error,
        
        /// <summary>
        /// Action notification.
        /// </summary>
        Action
    }
}
