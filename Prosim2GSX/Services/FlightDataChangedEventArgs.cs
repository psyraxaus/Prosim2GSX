using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for flight data changes
    /// </summary>
    public class FlightDataChangedEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets the type of operation that caused the data change
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the type of data that changed
        /// </summary>
        public string DataType { get; }
        
        /// <summary>
        /// Gets the current value of the data
        /// </summary>
        public object CurrentValue { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FlightDataChangedEventArgs"/> class
        /// </summary>
        /// <param name="operationType">The type of operation that caused the data change</param>
        public FlightDataChangedEventArgs(string operationType)
        {
            OperationType = operationType;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FlightDataChangedEventArgs"/> class
        /// </summary>
        /// <param name="operationType">The type of operation that caused the data change</param>
        /// <param name="dataType">The type of data that changed</param>
        /// <param name="currentValue">The current value of the data</param>
        public FlightDataChangedEventArgs(string operationType, string dataType, object currentValue)
        {
            OperationType = operationType;
            DataType = dataType;
            CurrentValue = currentValue;
        }
    }
}
