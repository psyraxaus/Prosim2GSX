using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for passenger state changes
    /// </summary>
    public class PassengerStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of operation that caused the state change
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the current number of passengers
        /// </summary>
        public int CurrentCount { get; }
        
        /// <summary>
        /// Gets the planned number of passengers
        /// </summary>
        public int PlannedCount { get; }
        
        /// <summary>
        /// Creates a new instance of PassengerStateChangedEventArgs
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentCount">The current number of passengers</param>
        /// <param name="plannedCount">The planned number of passengers</param>
        public PassengerStateChangedEventArgs(string operationType, int currentCount, int plannedCount)
        {
            OperationType = operationType;
            CurrentCount = currentCount;
            PlannedCount = plannedCount;
        }
    }
    
    /// <summary>
    /// Event arguments for cargo state changes
    /// </summary>
    public class CargoStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of operation that caused the state change
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the current cargo percentage
        /// </summary>
        public int CurrentPercentage { get; }
        
        /// <summary>
        /// Gets the planned cargo amount
        /// </summary>
        public int PlannedAmount { get; }
        
        /// <summary>
        /// Creates a new instance of CargoStateChangedEventArgs
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentPercentage">The current cargo percentage</param>
        /// <param name="plannedAmount">The planned cargo amount</param>
        public CargoStateChangedEventArgs(string operationType, int currentPercentage, int plannedAmount)
        {
            OperationType = operationType;
            CurrentPercentage = currentPercentage;
            PlannedAmount = plannedAmount;
        }
    }
    
    /// <summary>
    /// Event arguments for fuel state changes
    /// </summary>
    public class FuelStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of operation that caused the state change
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        public double CurrentAmount { get; }
        
        /// <summary>
        /// Gets the planned fuel amount
        /// </summary>
        public double PlannedAmount { get; }
        
        /// <summary>
        /// Gets the fuel units (KG or LBS)
        /// </summary>
        public string FuelUnits { get; }
        
        /// <summary>
        /// Creates a new instance of FuelStateChangedEventArgs
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentAmount">The current fuel amount</param>
        /// <param name="plannedAmount">The planned fuel amount</param>
        /// <param name="fuelUnits">The fuel units (KG or LBS)</param>
        public FuelStateChangedEventArgs(string operationType, double currentAmount, double plannedAmount, string fuelUnits)
        {
            OperationType = operationType;
            CurrentAmount = currentAmount;
            PlannedAmount = plannedAmount;
            FuelUnits = fuelUnits;
        }
    }
    
    /// <summary>
    /// Event arguments for door state changes
    /// </summary>
    public class DoorStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the name of the door
        /// </summary>
        public string DoorName { get; }
        
        /// <summary>
        /// Gets whether the door is open
        /// </summary>
        public bool IsOpen { get; }
        
        /// <summary>
        /// Creates a new instance of DoorStateChangedEventArgs
        /// </summary>
        /// <param name="doorName">The name of the door</param>
        /// <param name="isOpen">Whether the door is open</param>
        public DoorStateChangedEventArgs(string doorName, bool isOpen)
        {
            DoorName = doorName;
            IsOpen = isOpen;
        }
    }
    
    /// <summary>
    /// Event arguments for equipment state changes
    /// </summary>
    public class EquipmentStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the name of the equipment
        /// </summary>
        public string EquipmentName { get; }
        
        /// <summary>
        /// Gets whether the equipment is enabled
        /// </summary>
        public bool IsEnabled { get; }
        
        /// <summary>
        /// Creates a new instance of EquipmentStateChangedEventArgs
        /// </summary>
        /// <param name="equipmentName">The name of the equipment</param>
        /// <param name="isEnabled">Whether the equipment is enabled</param>
        public EquipmentStateChangedEventArgs(string equipmentName, bool isEnabled)
        {
            EquipmentName = equipmentName;
            IsEnabled = isEnabled;
        }
    }
    
    /// <summary>
    /// Event arguments for fluid state changes
    /// </summary>
    public class FluidStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of operation that caused the state change
        /// </summary>
        public string OperationType { get; }
        
        /// <summary>
        /// Gets the current blue hydraulic fluid amount
        /// </summary>
        public double BlueAmount { get; }
        
        /// <summary>
        /// Gets the current green hydraulic fluid amount
        /// </summary>
        public double GreenAmount { get; }
        
        /// <summary>
        /// Gets the current yellow hydraulic fluid amount
        /// </summary>
        public double YellowAmount { get; }
        
        /// <summary>
        /// Creates a new instance of FluidStateChangedEventArgs
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="blueAmount">The current blue hydraulic fluid amount</param>
        /// <param name="greenAmount">The current green hydraulic fluid amount</param>
        /// <param name="yellowAmount">The current yellow hydraulic fluid amount</param>
        public FluidStateChangedEventArgs(string operationType, double blueAmount, double greenAmount, double yellowAmount)
        {
            OperationType = operationType;
            BlueAmount = blueAmount;
            GreenAmount = greenAmount;
            YellowAmount = yellowAmount;
        }
    }
    
    /// <summary>
    /// Event arguments for flight data changes
    /// </summary>
    public class FlightDataChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of data that changed
        /// </summary>
        public string DataType { get; }
        
        /// <summary>
        /// Gets the current value
        /// </summary>
        public object CurrentValue { get; }
        
        /// <summary>
        /// Gets the previous value
        /// </summary>
        public object PreviousValue { get; }
        
        /// <summary>
        /// Creates a new instance of FlightDataChangedEventArgs
        /// </summary>
        /// <param name="dataType">The type of data that changed</param>
        /// <param name="currentValue">The current value</param>
        /// <param name="previousValue">The previous value</param>
        public FlightDataChangedEventArgs(string dataType, object currentValue, object previousValue = null)
        {
            DataType = dataType;
            CurrentValue = currentValue;
            PreviousValue = previousValue;
        }
    }
}
