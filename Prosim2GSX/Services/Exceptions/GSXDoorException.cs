using System;

namespace Prosim2GSX.Services.Exceptions
{
    /// <summary>
    /// Exception that represents a failure in GSX door operations
    /// </summary>
    public class GSXDoorException : GSXServiceException
    {
        /// <summary>
        /// Gets the door type that caused the error
        /// </summary>
        public string DoorType { get; }

        /// <summary>
        /// Gets the door state when the error occurred
        /// </summary>
        public string DoorState { get; }

        /// <summary>
        /// Gets the door operation that was being performed
        /// </summary>
        public string DoorOperation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXDoorException"/> class
        /// </summary>
        public GSXDoorException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXDoorException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public GSXDoorException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXDoorException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXDoorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXDoorException"/> class with a specified error message,
        /// operation, and context information
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        public GSXDoorException(string message, string operation, string context) 
            : base(message, operation, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXDoorException"/> class with a specified error message,
        /// operation, context information, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXDoorException(string message, string operation, string context, Exception innerException) 
            : base(message, operation, context, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXDoorException"/> class with a specified error message,
        /// operation, context information, service, state, service operation, and transient flag
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        /// <param name="serviceOperation">The service operation that failed</param>
        /// <param name="isTransient">A value indicating whether this is a transient exception that can be retried</param>
        public GSXDoorException(string message, string operation, string context, string service, string state, string serviceOperation, bool isTransient) 
            : base(message, operation, context, service, state, serviceOperation, isTransient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXDoorException"/> class with a specified error message,
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
        public GSXDoorException(string message, string operation, string context, string service, string state, string serviceOperation, bool isTransient, Exception innerException) 
            : base(message, operation, context, service, state, serviceOperation, isTransient, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXDoorException"/> class with a specified error message,
        /// operation, context information, service, state, service operation, transient flag, door type, door state, and door operation
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        /// <param name="serviceOperation">The service operation that failed</param>
        /// <param name="isTransient">A value indicating whether this is a transient exception that can be retried</param>
        /// <param name="doorType">The door type that caused the error</param>
        /// <param name="doorState">The door state when the error occurred</param>
        /// <param name="doorOperation">The door operation that was being performed</param>
        public GSXDoorException(string message, string operation, string context, string service, string state, string serviceOperation, bool isTransient, string doorType, string doorState, string doorOperation) 
            : base(message, operation, context, service, state, serviceOperation, isTransient)
        {
            DoorType = doorType;
            DoorState = doorState;
            DoorOperation = doorOperation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXDoorException"/> class with a specified error message,
        /// operation, context information, service, state, service operation, transient flag, door type, door state, door operation, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        /// <param name="serviceOperation">The service operation that failed</param>
        /// <param name="isTransient">A value indicating whether this is a transient exception that can be retried</param>
        /// <param name="doorType">The door type that caused the error</param>
        /// <param name="doorState">The door state when the error occurred</param>
        /// <param name="doorOperation">The door operation that was being performed</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXDoorException(string message, string operation, string context, string service, string state, string serviceOperation, bool isTransient, string doorType, string doorState, string doorOperation, Exception innerException) 
            : base(message, operation, context, service, state, serviceOperation, isTransient, innerException)
        {
            DoorType = doorType;
            DoorState = doorState;
            DoorOperation = doorOperation;
        }
    }
}
