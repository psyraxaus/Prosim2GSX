using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace Prosim2GSX.Events
{
    /// <summary>
    /// Provides a central hub for publishing and subscribing to events within the application.
    /// Implements the Publish-Subscribe pattern to enable loose coupling between components.
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        private static readonly Lazy<EventAggregator> _instance = new Lazy<EventAggregator>(() => new EventAggregator(), LazyThreadSafetyMode.ExecutionAndPublication);
        private readonly Dictionary<Type, List<object>> _subscriptions = new Dictionary<Type, List<object>>();
        private readonly Dictionary<SubscriptionToken, object> _tokenToSubscriptionMap = new Dictionary<SubscriptionToken, object>();
        private readonly object _lockObject = new object();
        private static ILogger _logger;

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// Attempts to obtain a logger from ServiceLocator if it's already initialized.
        /// </summary>
        private EventAggregator()
        {
            // Try to get a logger from ServiceLocator if it's initialized
            try
            {
                _logger = Services.ServiceLocator.GetLogger("EventAggregator");
            }
            catch
            {
                // ServiceLocator may not be initialized yet, we'll initialize the logger later
                _logger = null;
            }
        }

        /// <summary>
        /// Initializes the logger for the EventAggregator.
        /// This method should be called during application startup if the EventAggregator
        /// is used before the ServiceLocator is fully initialized.
        /// </summary>
        /// <param name="logger">The logger to use for logging events and errors</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided logger is null</exception>
        public static void InitializeLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the singleton instance of the EventAggregator.
        /// </summary>
        public static EventAggregator Instance => _instance.Value;

        /// <summary>
        /// Publishes an event to all subscribers. If the current thread is not the UI thread
        /// and the Application.Current is not null, the event will be dispatched to the UI thread.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to publish</typeparam>
        /// <param name="eventToPublish">The event instance to publish</param>
        /// <exception cref="ArgumentNullException">Thrown if the eventToPublish is null</exception>
        public void Publish<TEvent>(TEvent eventToPublish) where TEvent : EventBase
        {
            if (eventToPublish == null) throw new ArgumentNullException(nameof(eventToPublish));

            List<object> subscriptions;
            lock (_lockObject)
            {
                if (!_subscriptions.TryGetValue(typeof(TEvent), out subscriptions))
                    return;

                subscriptions = subscriptions.ToList();
            }

            // Always dispatch UI-related events to the UI thread
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => PublishToSubscribers(eventToPublish, subscriptions));
            }
            else
            {
                PublishToSubscribers(eventToPublish, subscriptions);
            }
        }

        /// <summary>
        /// Publishes an event to each subscriber in the provided list.
        /// Catches and logs any exceptions that occur during event handling.
        /// </summary>
        /// <typeparam name="TEvent">The type of event being published</typeparam>
        /// <param name="eventToPublish">The event instance to publish</param>
        /// <param name="subscriptions">The list of subscribers to notify</param>
        private void PublishToSubscribers<TEvent>(TEvent eventToPublish, List<object> subscriptions) where TEvent : EventBase
        {
            foreach (var subscription in subscriptions)
            {
                var action = (Action<TEvent>)subscription;
                try
                {
                    action(eventToPublish);
                }
                catch (Exception ex)
                {
                    // Use the logger with null-conditional operator to handle the case where it hasn't been initialized yet
                    _logger?.LogError(ex, "Exception in event handler for {EventType}: {ErrorMessage}",
                        typeof(TEvent).Name, ex.Message);
                }
            }
        }

        /// <summary>
        /// Subscribes to events of the specified type with the provided action.
        /// Returns a token that can be used to unsubscribe.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to subscribe to</typeparam>
        /// <param name="action">The action to execute when the event is published</param>
        /// <returns>A subscription token that can be used to unsubscribe</returns>
        /// <exception cref="ArgumentNullException">Thrown if the action is null</exception>
        public SubscriptionToken Subscribe<TEvent>(Action<TEvent> action) where TEvent : EventBase
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            lock (_lockObject)
            {
                if (!_subscriptions.TryGetValue(typeof(TEvent), out var subscriptions))
                {
                    subscriptions = new List<object>();
                    _subscriptions[typeof(TEvent)] = subscriptions;
                }

                subscriptions.Add(action);

                var token = new SubscriptionToken();
                _tokenToSubscriptionMap[token] = action;

                return token;
            }
        }

        /// <summary>
        /// Unsubscribes from events of the specified type using the provided token.
        /// If the token is not found or is not associated with the specified event type,
        /// this method has no effect.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to unsubscribe from</typeparam>
        /// <param name="token">The subscription token returned from the Subscribe method</param>
        /// <exception cref="ArgumentNullException">Thrown if the token is null</exception>
        public void Unsubscribe<TEvent>(SubscriptionToken token) where TEvent : EventBase
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            lock (_lockObject)
            {
                if (!_tokenToSubscriptionMap.TryGetValue(token, out var subscription))
                    return;

                if (_subscriptions.TryGetValue(typeof(TEvent), out var subscriptions))
                {
                    subscriptions.Remove(subscription);

                    if (subscriptions.Count == 0)
                        _subscriptions.Remove(typeof(TEvent));
                }

                _tokenToSubscriptionMap.Remove(token);
            }
        }
    }
}
