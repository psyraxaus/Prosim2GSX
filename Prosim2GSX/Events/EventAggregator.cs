using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;

namespace Prosim2GSX.Events
{
    public class EventAggregator : IEventAggregator
    {
        private static readonly Lazy<EventAggregator> _instance = new Lazy<EventAggregator>(() => new EventAggregator(), LazyThreadSafetyMode.ExecutionAndPublication);
        private readonly Dictionary<Type, List<object>> _subscriptions = new Dictionary<Type, List<object>>();
        private readonly Dictionary<SubscriptionToken, object> _tokenToSubscriptionMap = new Dictionary<SubscriptionToken, object>();
        private readonly object _lockObject = new object();

        private EventAggregator() { }

        public static EventAggregator Instance => _instance.Value;

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

            foreach (var subscription in subscriptions)
            {
                var action = (Action<TEvent>)subscription;
                try
                {
                    action(eventToPublish);
                }
                catch (Exception ex)
                {
                    LogService.Log(LogLevel.Error, "EventAggregator:Publish", 
                        $"Exception in event handler for {typeof(TEvent).Name}: {ex.Message}");
                }
            }
        }

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
