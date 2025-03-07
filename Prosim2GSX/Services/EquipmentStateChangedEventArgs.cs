using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Defines the types of ground equipment
    /// </summary>
    public enum EquipmentType
    {
        /// <summary>
        /// Ground Power Unit
        /// </summary>
        GPU,
        
        /// <summary>
        /// Preconditioned Air
        /// </summary>
        PCA,
        
        /// <summary>
        /// Wheel Chocks
        /// </summary>
        Chocks,
        
        /// <summary>
        /// Jetway/Stairs
        /// </summary>
        Jetway
    }

    /// <summary>
    /// Event arguments for equipment state changes
    /// </summary>
    public class EquipmentStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of equipment that changed state
        /// </summary>
        public EquipmentType EquipmentType { get; }

        /// <summary>
        /// Gets a value indicating whether the equipment is connected
        /// </summary>
        public bool IsConnected { get; }
        
        /// <summary>
        /// Gets the name of the equipment that changed state
        /// </summary>
        public string EquipmentName { get; }
        
        /// <summary>
        /// Gets a value indicating whether the equipment is enabled
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// Gets the timestamp when the state change occurred
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EquipmentStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="equipmentType">The type of equipment that changed state</param>
        /// <param name="isConnected">A value indicating whether the equipment is connected</param>
        public EquipmentStateChangedEventArgs(EquipmentType equipmentType, bool isConnected)
        {
            EquipmentType = equipmentType;
            IsConnected = isConnected;
            EquipmentName = equipmentType.ToString();
            IsEnabled = isConnected;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EquipmentStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="equipmentName">The name of the equipment that changed state</param>
        /// <param name="isEnabled">A value indicating whether the equipment is enabled</param>
        public EquipmentStateChangedEventArgs(string equipmentName, bool isEnabled)
        {
            // Map string identifiers to enum values
            EquipmentType = equipmentName switch
            {
                "GPU" => EquipmentType.GPU,
                "PCA" => EquipmentType.PCA,
                "Chocks" => EquipmentType.Chocks,
                _ => throw new ArgumentException($"Unknown equipment name: {equipmentName}", nameof(equipmentName))
            };
            IsConnected = isEnabled;
            EquipmentName = equipmentName;
            IsEnabled = isEnabled;
            Timestamp = DateTime.Now;
        }
    }
}
