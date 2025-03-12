using System;

namespace Prosim2GSX.Services.Exceptions
{
    /// <summary>
    /// Exception that represents a failure in SimConnect operations
    /// </summary>
    public class SimConnectException : ServiceException
    {
        /// <summary>
        /// Gets the SimConnect error code
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectException"/> class
        /// </summary>
        public SimConnectException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public SimConnectException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public SimConnectException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectException"/> class with a specified error message,
        /// operation, and context information
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        public SimConnectException(string message, string operation, string context) 
            : base(message, operation, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectException"/> class with a specified error message,
        /// operation, context information, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public SimConnectException(string message, string operation, string context, Exception innerException) 
            : base(message, operation, context, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectException"/> class with a specified error message,
        /// operation, context information, and error code
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="errorCode">The SimConnect error code</param>
        public SimConnectException(string message, string operation, string context, int errorCode) 
            : base(message, operation, context)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimConnectException"/> class with a specified error message,
        /// operation, context information, error code, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="errorCode">The SimConnect error code</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public SimConnectException(string message, string operation, string context, int errorCode, Exception innerException) 
            : base(message, operation, context, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
