using System;

namespace Prosim2GSX.Services.Exceptions
{
    /// <summary>
    /// Exception that represents a failure in GSX fuel operations
    /// </summary>
    public class GSXFuelException : GSXServiceException
    {
        /// <summary>
        /// Gets the current fuel amount when the error occurred
        /// </summary>
        public double CurrentFuelAmount { get; }

        /// <summary>
        /// Gets the target fuel amount when the error occurred
        /// </summary>
        public double TargetFuelAmount { get; }

        /// <summary>
        /// Gets the refueling state when the error occurred
        /// </summary>
        public string RefuelingState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXFuelException"/> class
        /// </summary>
        public GSXFuelException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXFuelException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public GSXFuelException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXFuelException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXFuelException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXFuelException"/> class with a specified error message,
        /// operation, and context information
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        public GSXFuelException(string message, string operation, string context) 
            : base(message, operation, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXFuelException"/> class with a specified error message,
        /// operation, context information, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXFuelException(string message, string operation, string context, Exception innerException) 
            : base(message, operation, context, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXFuelException"/> class with a specified error message,
        /// operation, context information, service, state, service operation, and transient flag
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        /// <param name="serviceOperation">The service operation that failed</param>
        /// <param name="isTransient">A value indicating whether this is a transient exception that can be retried</param>
        public GSXFuelException(string message, string operation, string context, string service, string state, string serviceOperation, bool isTransient) 
            : base(message, operation, context, service, state, serviceOperation, isTransient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXFuelException"/> class with a specified error message,
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
        public GSXFuelException(string message, string operation, string context, string service, string state, string serviceOperation, bool isTransient, Exception innerException) 
            : base(message, operation, context, service, state, serviceOperation, isTransient, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXFuelException"/> class with a specified error message,
        /// operation, context information, service, state, service operation, transient flag, current fuel amount, target fuel amount, and refueling state
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        /// <param name="serviceOperation">The service operation that failed</param>
        /// <param name="isTransient">A value indicating whether this is a transient exception that can be retried</param>
        /// <param name="currentFuelAmount">The current fuel amount when the error occurred</param>
        /// <param name="targetFuelAmount">The target fuel amount when the error occurred</param>
        /// <param name="refuelingState">The refueling state when the error occurred</param>
        public GSXFuelException(string message, string operation, string context, string service, string state, string serviceOperation, bool isTransient, double currentFuelAmount, double targetFuelAmount, string refuelingState) 
            : base(message, operation, context, service, state, serviceOperation, isTransient)
        {
            CurrentFuelAmount = currentFuelAmount;
            TargetFuelAmount = targetFuelAmount;
            RefuelingState = refuelingState;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GSXFuelException"/> class with a specified error message,
        /// operation, context information, service, state, service operation, transient flag, current fuel amount, target fuel amount, refueling state, and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="operation">The operation that was being performed when the exception occurred</param>
        /// <param name="context">Additional context information about the exception</param>
        /// <param name="service">The GSX service that caused the error</param>
        /// <param name="state">The GSX state when the error occurred</param>
        /// <param name="serviceOperation">The service operation that failed</param>
        /// <param name="isTransient">A value indicating whether this is a transient exception that can be retried</param>
        /// <param name="currentFuelAmount">The current fuel amount when the error occurred</param>
        /// <param name="targetFuelAmount">The target fuel amount when the error occurred</param>
        /// <param name="refuelingState">The refueling state when the error occurred</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public GSXFuelException(string message, string operation, string context, string service, string state, string serviceOperation, bool isTransient, double currentFuelAmount, double targetFuelAmount, string refuelingState, Exception innerException) 
            : base(message, operation, context, service, state, serviceOperation, isTransient, innerException)
        {
            CurrentFuelAmount = currentFuelAmount;
            TargetFuelAmount = targetFuelAmount;
            RefuelingState = refuelingState;
        }
    }
}
