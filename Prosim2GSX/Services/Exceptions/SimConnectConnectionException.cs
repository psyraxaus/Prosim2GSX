using System;

namespace Prosim2GSX.Services.Exceptions
{
    /// <summary>
    /// Exception that represents a connection failure in SimConnect operations
    /// </summary>
    public class SimConnectConnectionException : SimConnectException
    {
        /// <summary>
        /// Gets a value indicating whether this is a transient exception that can be retried
        /// </summary>
        public bool IsTransient { get; }

        /// <summary>
        /// Gets the recommended retry delay in milliseconds
        /// </summary>
        public int RecommendedRetryDelayMs { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectConnectionException"/> class
        /// </summary>
        public SimConnectConnectionException() : base()
        {
            IsTransient = true;
            RecommendedRetryDelayMs = 5000; // Default 5 seconds
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectConnectionException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public SimConnectConnectionException(string message) : base(message)
        {
            IsTransient = true;
            RecommendedRetryDelayMs = 5000; // Default 5 seconds
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectConnectionException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public SimConnectConnectionException(string message, Exception innerException) : base(message, innerException)
        {
            IsTransient = true;
            RecommendedRetryDelayMs = 5000; // Default 5 seconds
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectConnectionException"/> class with a specified error message,
        /// operation, and context information
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        public SimConnectConnectionException(string message, string operation, string context) 
            : base(message, operation, context)
        {
            IsTransient = true;
            RecommendedRetryDelayMs = 5000; // Default 5 seconds
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectConnectionException"/> class with a specified error message,
        /// operation, context information, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public SimConnectConnectionException(string message, string operation, string context, Exception innerException) 
            : base(message, operation, context, innerException)
        {
            IsTransient = true;
            RecommendedRetryDelayMs = 5000; // Default 5 seconds
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectConnectionException"/> class with a specified error message,
        /// operation, context information, error code, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="errorCode">The SimConnect error code</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public SimConnectConnectionException(string message, string operation, string context, int errorCode, Exception innerException) 
            : base(message, operation, context, errorCode, innerException)
        {
            IsTransient = true;
            RecommendedRetryDelayMs = 5000; // Default 5 seconds
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectConnectionException"/> class with a specified error message,
        /// operation, context information, error code, transient flag, and recommended retry delay
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="errorCode">The SimConnect error code</param>
        /// <param name="isTransient">A value indicating whether this is a transient exception that can be retried</param>
        /// <param name="recommendedRetryDelayMs">The recommended delay in milliseconds before retrying the operation</param>
        public SimConnectConnectionException(string message, string operation, string context, int errorCode, bool isTransient, int recommendedRetryDelayMs) 
            : base(message, operation, context, errorCode)
        {
            IsTransient = isTransient;
            RecommendedRetryDelayMs = recommendedRetryDelayMs;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectConnectionException"/> class with a specified error message,
        /// operation, context information, error code, transient flag, recommended retry delay, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="errorCode">The SimConnect error code</param>
        /// <param name="isTransient">A value indicating whether this is a transient exception that can be retried</param>
        /// <param name="recommendedRetryDelayMs">The recommended delay in milliseconds before retrying the operation</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public SimConnectConnectionException(string message, string operation, string context, int errorCode, bool isTransient, int recommendedRetryDelayMs, Exception innerException) 
            : base(message, operation, context, errorCode, innerException)
        {
            IsTransient = isTransient;
            RecommendedRetryDelayMs = recommendedRetryDelayMs;
        }
    }
}
