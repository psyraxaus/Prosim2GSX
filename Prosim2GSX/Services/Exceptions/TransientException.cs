using System;

namespace Prosim2GSX.Services.Exceptions
{
    /// <summary>
    /// Exception that represents a transient failure that may succeed if retried
    /// </summary>
    public class TransientException : ServiceException
    {
        /// <summary>
        /// Gets the recommended retry delay in milliseconds
        /// </summary>
        public int RecommendedRetryDelayMs { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class
        /// </summary>
        public TransientException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public TransientException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public TransientException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class with a specified error message,
        /// operation, and context information
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        public TransientException(string message, string operation, string context) 
            : base(message, operation, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class with a specified error message,
        /// operation, context information, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public TransientException(string message, string operation, string context, Exception innerException) 
            : base(message, operation, context, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class with a specified error message,
        /// operation, context information, and recommended retry delay
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="recommendedRetryDelayMs">The recommended delay in milliseconds before retrying the operation</param>
        public TransientException(string message, string operation, string context, int recommendedRetryDelayMs) 
            : base(message, operation, context)
        {
            RecommendedRetryDelayMs = recommendedRetryDelayMs;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class with a specified error message,
        /// operation, context information, recommended retry delay, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="recommendedRetryDelayMs">The recommended delay in milliseconds before retrying the operation</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public TransientException(string message, string operation, string context, int recommendedRetryDelayMs, Exception innerException) 
            : base(message, operation, context, innerException)
        {
            RecommendedRetryDelayMs = recommendedRetryDelayMs;
        }
    }
}
