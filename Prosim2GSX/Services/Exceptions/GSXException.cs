using System;

namespace Prosim2GSX.Services.Exceptions
{
    /// <summary>
    /// Exception that represents a failure in GSX operations
    /// </summary>
    public class GSXException : ServiceException
    {
        /// <summary>
        /// Gets the GSX service that caused the error
        /// </summary>
        public string Service { get; }

        /// <summary>
        /// Gets the GSX state when the error occurred
        /// </summary>
        public string State { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXException"/> class
        /// </summary>
        public GSXException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public GSXException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXException"/> class with a specified error message,
        /// operation, and context information
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        public GSXException(string message, string operation, string context) 
            : base(message, operation, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXException"/> class with a specified error message,
        /// operation, context information, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXException(string message, string operation, string context, Exception innerException) 
            : base(message, operation, context, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXException"/> class with a specified error message,
        /// operation, context information, service, and state
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        public GSXException(string message, string operation, string context, string service, string state) 
            : base(message, operation, context)
        {
            Service = service;
            State = state;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXException"/> class with a specified error message,
        /// operation, context information, service, state, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXException(string message, string operation, string context, string service, string state, Exception innerException) 
            : base(message, operation, context, innerException)
        {
            Service = service;
            State = state;
        }
    }
}
