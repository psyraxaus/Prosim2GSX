using System;

namespace Prosim2GSX.Services.Exceptions
{
    /// <summary>
    /// Exception that represents a failure in ProSim operations
    /// </summary>
    public class ProsimException : ServiceException
    {
        /// <summary>
        /// Gets the ProSim error code
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Gets the ProSim component that caused the error
        /// </summary>
        public string Component { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProsimException"/> class
        /// </summary>
        public ProsimException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProsimException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public ProsimException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProsimException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public ProsimException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProsimException"/> class with a specified error message,
        /// operation, and context information
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        public ProsimException(string message, string operation, string context) 
            : base(message, operation, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProsimException"/> class with a specified error message,
        /// operation, context information, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public ProsimException(string message, string operation, string context, Exception innerException) 
            : base(message, operation, context, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProsimException"/> class with a specified error message,
        /// operation, context information, error code, and component
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="errorCode">The ProSim error code</param>
        /// <param name="component">The ProSim component that caused the error</param>
        public ProsimException(string message, string operation, string context, int errorCode, string component) 
            : base(message, operation, context)
        {
            ErrorCode = errorCode;
            Component = component;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProsimException"/> class with a specified error message,
        /// operation, context information, error code, component, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="errorCode">The ProSim error code</param>
        /// <param name="component">The ProSim component that caused the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public ProsimException(string message, string operation, string context, int errorCode, string component, Exception innerException) 
            : base(message, operation, context, innerException)
        {
            ErrorCode = errorCode;
            Component = component;
        }
    }
}
