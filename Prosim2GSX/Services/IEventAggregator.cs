using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for event aggregation services
    /// </summary>
    public interface IEventAggregator : IDisposable
    {
        /// <summary>
        /// Subscribes to events of the specified type
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="action">The action to execute when the event is published</param>
        void Subscribe<T>(Action<T> action) where T : BaseEventArgs;
        
        /// <summary>
        /// Unsubscribes from events of the specified type
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="action">The action to unsubscribe</param>
        void Unsubscribe<T>(Action<T> action) where T : BaseEventArgs;
        
        /// <summary>
        /// Publishes an event to all subscribers
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="eventArgs">The event arguments</param>
        void Publish<T>(T eventArgs) where T : BaseEventArgs;
        
        /// <summary>
        /// Performs periodic cleanup of all dead references
        /// </summary>
        void PerformCleanup();
    }
}
