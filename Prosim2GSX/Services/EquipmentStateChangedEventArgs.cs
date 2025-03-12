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
        /// Jetway
        /// </summary>
        Jetway,
        
        /// <summary>
        /// Stairs
        /// </summary>
        Stairs
    }

    /// <summary>
    /// Event arguments for equipment state changes
    /// </summary>
    public class EquipmentStateChangedEventArgs : BaseEventArgs
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
                "Jetway" => EquipmentType.Jetway,
                "Stairs" => EquipmentType.Stairs,
                _ => throw new ArgumentException($"Unknown equipment name: {equipmentName}", nameof(equipmentName))
            };
            IsConnected = isEnabled;
            EquipmentName = equipmentName;
            IsEnabled = isEnabled;
        }
    }
}
