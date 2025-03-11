using System;

namespace Prosim2GSX.Services.EventArgs
{
    /// <summary>
    /// Generic event arguments for state changes
    /// </summary>
    /// <typeparam name="T">The type of state</typeparam>
    public class StateChangedEventArgs<T> : System.EventArgs
    {
        /// <summary>
        /// Gets the previous state
        /// </summary>
        public T PreviousState { get; }
        
        /// <summary>
        /// Gets the new state
        /// </summary>
        public T NewState { get; }
        
        /// <summary>
        /// Gets the timestamp when the state change occurred
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Gets the correlation ID for tracking related events
        /// </summary>
        public string CorrelationId { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="StateChangedEventArgs{T}"/> class
        /// </summary>
        /// <param name="previousState">The previous state</param>
        /// <param name="newState">The new state</param>
        public StateChangedEventArgs(T previousState, T newState)
            : this(previousState, newState, Guid.NewGuid().ToString())
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="StateChangedEventArgs{T}"/> class
        /// </summary>
        /// <param name="previousState">The previous state</param>
        /// <param name="newState">The new state</param>
        /// <param name="correlationId">The correlation ID for tracking related events</param>
        public StateChangedEventArgs(T previousState, T newState, string correlationId)
        {
            PreviousState = previousState;
            NewState = newState;
            Timestamp = DateTime.UtcNow;
            CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        }
    }
}
