using System;

namespace Prosim2GSX.Services.Exceptions
{
    /// <summary>
    /// Base exception class for all service-related exceptions
    /// </summary>
    public class ServiceException : Exception
    {
        /// <summary>
        /// Gets the operation that was being performed when the exception occurred
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// Gets additional context information about the exception
        /// </summary>
        public string Context { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException"/> class
        /// </summary>
        public ServiceException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public ServiceException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public ServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException"/> class with a specified error message,
        /// operation, and context information
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        public ServiceException(string message, string operation, string context) : base(message)
        {
            Operation = operation;
            Context = context;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException"/> class with a specified error message,
        /// operation, context information, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public ServiceException(string message, string operation, string context, Exception innerException) 
            : base(message, innerException)
        {
            Operation = operation;
            Context = context;
        }
    }
}
