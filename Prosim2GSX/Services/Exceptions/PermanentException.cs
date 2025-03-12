using System;

namespace Prosim2GSX.Services.Exceptions
{
    /// <summary>
    /// Exception that represents a permanent failure that will not succeed if retried
    /// </summary>
    public class PermanentException : ServiceException
    {
        /// <summary>
        /// Gets a value indicating whether this exception represents a critical failure
        /// </summary>
        public bool IsCritical { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PermanentException"/> class
        /// </summary>
        public PermanentException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PermanentException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public PermanentException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PermanentException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public PermanentException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PermanentException"/> class with a specified error message,
        /// operation, and context information
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        public PermanentException(string message, string operation, string context) 
            : base(message, operation, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PermanentException"/> class with a specified error message,
        /// operation, context information, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public PermanentException(string message, string operation, string context, Exception innerException) 
            : base(message, operation, context, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PermanentException"/> class with a specified error message,
        /// operation, context information, and criticality flag
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="isCritical">A value indicating whether this exception represents a critical failure</param>
        public PermanentException(string message, string operation, string context, bool isCritical) 
            : base(message, operation, context)
        {
            IsCritical = isCritical;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PermanentException"/> class with a specified error message,
        /// operation, context information, criticality flag, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="isCritical">A value indicating whether this exception represents a critical failure</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public PermanentException(string message, string operation, string context, bool isCritical, Exception innerException) 
            : base(message, operation, context, innerException)
        {
            IsCritical = isCritical;
        }
    }
}
