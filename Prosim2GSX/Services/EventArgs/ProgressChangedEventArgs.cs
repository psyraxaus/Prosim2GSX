using System;

namespace Prosim2GSX.Services.EventArgs
{
    /// <summary>
    /// Event arguments for progress changes
    /// </summary>
    public class ProgressChangedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage { get; }
        
        /// <summary>
        /// Gets the operation type
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the timestamp when the progress change occurred
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Gets the correlation ID for tracking related events
        /// </summary>
        public string CorrelationId { get; }
        
        /// <summary>
        /// Gets the estimated time remaining, if available
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressChangedEventArgs"/> class
        /// </summary>
        /// <param name="progressPercentage">The progress percentage (0-100)</param>
        /// <param name="operationType">The operation type</param>
        public ProgressChangedEventArgs(int progressPercentage, string operationType)
            : this(progressPercentage, operationType, null, Guid.NewGuid().ToString())
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressChangedEventArgs"/> class
        /// </summary>
        /// <param name="progressPercentage">The progress percentage (0-100)</param>
        /// <param name="operationType">The operation type</param>
        /// <param name="estimatedTimeRemaining">The estimated time remaining, if available</param>
        public ProgressChangedEventArgs(int progressPercentage, string operationType, TimeSpan? estimatedTimeRemaining)
            : this(progressPercentage, operationType, estimatedTimeRemaining, Guid.NewGuid().ToString())
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressChangedEventArgs"/> class
        /// </summary>
        /// <param name="progressPercentage">The progress percentage (0-100)</param>
        /// <param name="operationType">The operation type</param>
        /// <param name="estimatedTimeRemaining">The estimated time remaining, if available</param>
        /// <param name="correlationId">The correlation ID for tracking related events</param>
        public ProgressChangedEventArgs(int progressPercentage, string operationType, TimeSpan? estimatedTimeRemaining, string correlationId)
        {
            if (progressPercentage < 0 || progressPercentage > 100)
                throw new ArgumentOutOfRangeException(nameof(progressPercentage), "Progress percentage must be between 0 and 100");
                
            ProgressPercentage = progressPercentage;
            OperationType = operationType ?? throw new ArgumentNullException(nameof(operationType));
            EstimatedTimeRemaining = estimatedTimeRemaining;
            Timestamp = DateTime.UtcNow;
            CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        }
    }
}
