using System;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Provides event aggregation services for loosely coupled event-based communication
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        private readonly Dictionary<Type, List<WeakReference>> _subscribers = new Dictionary<Type, List<WeakReference>>();
        private readonly object _lock = new object();
        private bool _disposed;
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EventAggregator"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public EventAggregator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Subscribes to events of the specified type
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="action">The action to execute when the event is published</param>
        public void Subscribe<T>(Action<T> action) where T : EventArgs
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            lock (_lock)
            {
                var type = typeof(T);
                if (!_subscribers.ContainsKey(type))
                {
                    _subscribers[type] = new List<WeakReference>();
                }
                
                _subscribers[type].Add(new WeakReference(action));
                
                _logger.Log(LogLevel.Debug, "EventAggregator:Subscribe", 
                    $"Subscribed to event type {type.Name}, total subscribers: {_subscribers[type].Count}");
            }
        }
        
        /// <summary>
        /// Unsubscribes from events of the specified type
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="action">The action to unsubscribe</param>
        public void Unsubscribe<T>(Action<T> action) where T : EventArgs
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            lock (_lock)
            {
                var type = typeof(T);
                if (!_subscribers.ContainsKey(type))
                    return;
                    
                var references = _subscribers[type];
                for (int i = references.Count - 1; i >= 0; i--)
                {
                    var reference = references[i];
                    if (!reference.IsAlive || reference.Target.Equals(action))
                    {
                        references.RemoveAt(i);
                    }
                }
                
                _logger.Log(LogLevel.Debug, "EventAggregator:Unsubscribe", 
                    $"Unsubscribed from event type {type.Name}, remaining subscribers: {references.Count}");
            }
        }
        
        /// <summary>
        /// Publishes an event to all subscribers
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="eventArgs">The event arguments</param>
        public void Publish<T>(T eventArgs) where T : EventArgs
        {
            if (eventArgs == null)
                throw new ArgumentNullException(nameof(eventArgs));
                
            var type = typeof(T);
            List<WeakReference> references;
            
            lock (_lock)
            {
                if (!_subscribers.ContainsKey(type))
                    return;
                    
                references = new List<WeakReference>(_subscribers[type]);
            }
            
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var reference in references)
            {
                if (reference.Target is Action<T> action)
                {
                    try
                    {
                        action(eventArgs);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.Log(LogLevel.Error, "EventAggregator:Publish", 
                            $"Error publishing event of type {type.Name}: {ex.Message}");
                    }
                }
            }
            
            _logger.Log(LogLevel.Debug, "EventAggregator:Publish", 
                $"Published event of type {type.Name}, successful deliveries: {successCount}, failures: {failureCount}");
            
            // Clean up dead references
            CleanupDeadReferences(type);
        }
        
        /// <summary>
        /// Removes dead references for the specified event type
        /// </summary>
        /// <param name="type">The event type</param>
        private void CleanupDeadReferences(Type type)
        {
            lock (_lock)
            {
                if (_subscribers.ContainsKey(type))
                {
                    int beforeCount = _subscribers[type].Count;
                    _subscribers[type].RemoveAll(r => !r.IsAlive);
                    int afterCount = _subscribers[type].Count;
                    
                    if (beforeCount != afterCount)
                    {
                        _logger.Log(LogLevel.Debug, "EventAggregator:CleanupDeadReferences", 
                            $"Removed {beforeCount - afterCount} dead references for event type {type.Name}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Performs periodic cleanup of all dead references
        /// </summary>
        public void PerformCleanup()
        {
            lock (_lock)
            {
                int totalRemoved = 0;
                
                foreach (var type in _subscribers.Keys.ToList())
                {
                    int beforeCount = _subscribers[type].Count;
                    _subscribers[type].RemoveAll(r => !r.IsAlive);
                    int afterCount = _subscribers[type].Count;
                    
                    totalRemoved += (beforeCount - afterCount);
                }
                
                if (totalRemoved > 0)
                {
                    _logger.Log(LogLevel.Debug, "EventAggregator:PerformCleanup", 
                        $"Removed {totalRemoved} dead references across all event types");
                }
            }
        }
        
        /// <summary>
        /// Disposes resources used by the event aggregator
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes resources used by the event aggregator
        /// </summary>
        /// <param name="disposing">Whether the method is being called from Dispose()</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                lock (_lock)
                {
                    _subscribers.Clear();
                }
                
                _logger.Log(LogLevel.Debug, "EventAggregator:Dispose", "Event aggregator disposed");
            }
            
            _disposed = true;
        }
    }
}
