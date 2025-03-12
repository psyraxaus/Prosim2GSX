using System;

namespace Prosim2GSX.Services.Exceptions
{
    /// <summary>
    /// Exception that represents a failure in a specific GSX service operation
    /// </summary>
    public class GSXServiceException : GSXException
    {
        /// <summary>
        /// Gets the service operation that failed
        /// </summary>
        public string ServiceOperation { get; }

        /// <summary>
        /// Gets a value indicating whether this is a transient exception that can be retried
        /// </summary>
        public bool IsTransient { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXServiceException"/> class
        /// </summary>
        public GSXServiceException() : base()
        {
            IsTransient = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXServiceException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public GSXServiceException(string message) : base(message)
        {
            IsTransient = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXServiceException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXServiceException(string message, Exception innerException) : base(message, innerException)
        {
            IsTransient = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXServiceException"/> class with a specified error message,
        /// operation, and context information
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        public GSXServiceException(string message, string operation, string context) 
            : base(message, operation, context)
        {
            IsTransient = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXServiceException"/> class with a specified error message,
        /// operation, context information, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXServiceException(string message, string operation, string context, Exception innerException) 
            : base(message, operation, context, innerException)
        {
            IsTransient = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXServiceException"/> class with a specified error message,
        /// operation, context information, service, and state
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        public GSXServiceException(string message, string operation, string context, string service, string state) 
            : base(message, operation, context, service, state)
        {
            IsTransient = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXServiceException"/> class with a specified error message,
        /// operation, context information, service, state, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXServiceException(string message, string operation, string context, string service, string state, Exception innerException) 
            : base(message, operation, context, service, state, innerException)
        {
            IsTransient = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXServiceException"/> class with a specified error message,
        /// operation, context information, service, state, service operation, and transient flag
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        /// <param name="serviceOperation">The service operation that failed</param>
        /// <param name="isTransient">A value indicating whether this is a transient exception that can be retried</param>
        public GSXServiceException(string message, string operation, string context, string service, string state, string serviceOperation, bool isTransient) 
            : base(message, operation, context, service, state)
        {
            ServiceOperation = serviceOperation;
            IsTransient = isTransient;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXServiceException"/> class with a specified error message,
        /// operation, context information, service, state, service operation, transient flag, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        /// <param name="serviceOperation">The service operation that failed</param>
        /// <param name="isTransient">A value indicating whether this is a transient exception that can be retried</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXServiceException(string message, string operation, string context, string service, string state, string serviceOperation, bool isTransient, Exception innerException) 
            : base(message, operation, context, service, state, innerException)
        {
            ServiceOperation = serviceOperation;
            IsTransient = isTransient;
        }
    }
}
