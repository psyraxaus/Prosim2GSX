using System;
using System.Threading;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for the ProSim controller that coordinates ProSim integration with GSX
    /// </summary>
    public interface IProsimController : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the controller is connected to ProSim
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Gets a value indicating whether engines are running
        /// </summary>
        bool EnginesRunning { get; }
        
        /// <summary>
        /// Gets the current flight plan ID
        /// </summary>
        string FlightPlanID { get; }
        
        /// <summary>
        /// Gets the current flight number
        /// </summary>
        string FlightNumber { get; }
        
        /// <summary>
        /// Connects to ProSim
        /// </summary>
        /// <param name="model">The service model</param>
        /// <returns>True if connection was successful, false otherwise</returns>
        bool Connect(ServiceModel model);
        
        /// <summary>
        /// Disconnects from ProSim
        /// </summary>
        void Disconnect();
        
        /// <summary>
        /// Updates the controller state
        /// </summary>
        /// <param name="forceCurrent">Whether to force current values to match planned values</param>
        void Update(bool forceCurrent);
        
        /// <summary>
        /// Initializes the flight plan
        /// </summary>
        /// <param name="flightPlan">The flight plan</param>
        void InitializeFlightPlan(FlightPlan flightPlan);
        
        /// <summary>
        /// Checks if a flight plan is loaded
        /// </summary>
        /// <returns>True if a flight plan is loaded, false otherwise</returns>
        bool IsFlightplanLoaded();
        
        /// <summary>
        /// Gets the door service
        /// </summary>
        /// <returns>The door service</returns>
        IProsimDoorService GetDoorService();
        
        /// <summary>
        /// Gets the equipment service
        /// </summary>
        /// <returns>The equipment service</returns>
        IProsimEquipmentService GetEquipmentService();
        
        /// <summary>
        /// Gets the passenger service
        /// </summary>
        /// <returns>The passenger service</returns>
        IProsimPassengerService GetPassengerService();
        
        /// <summary>
        /// Gets the cargo service
        /// </summary>
        /// <returns>The cargo service</returns>
        IProsimCargoService GetCargoService();
        
        /// <summary>
        /// Gets the fuel service
        /// </summary>
        /// <returns>The fuel service</returns>
        IProsimFuelService GetFuelService();
        
        /// <summary>
        /// Gets the flight data service
        /// </summary>
        /// <returns>The flight data service</returns>
        IProsimFlightDataService GetFlightDataService();
        
        /// <summary>
        /// Gets the fluid service
        /// </summary>
        /// <returns>The fluid service</returns>
        IProsimFluidService GetFluidService();
        
        /// <summary>
        /// Gets the planned passenger count
        /// </summary>
        /// <returns>The planned passenger count</returns>
        int GetPaxPlanned();
        
        /// <summary>
        /// Gets the current passenger count
        /// </summary>
        /// <returns>The current passenger count</returns>
        int GetPaxCurrent();
        
        /// <summary>
        /// Gets the planned fuel amount
        /// </summary>
        /// <returns>The planned fuel amount</returns>
        double GetFuelPlanned();
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        /// <returns>The current fuel amount</returns>
        double GetFuelCurrent();
        
        /// <summary>
        /// Gets the Zero Fuel Weight Center of Gravity (MACZFW)
        /// </summary>
        /// <returns>The MACZFW value as a percentage</returns>
        double GetZfwCG();
        
        /// <summary>
        /// Gets the Take Off Weight Center of Gravity (MACTOW)
        /// </summary>
        /// <returns>The MACTOW value as a percentage</returns>
        double GetTowCG();
        
        /// <summary>
        /// Gets loaded data for a loadsheet
        /// </summary>
        /// <param name="loadsheetType">The loadsheet type</param>
        /// <returns>A tuple containing the loaded data</returns>
        (string, string, string, string, string, string, string, double, double, double, double, double, double, int, int, double, double, int, int, int, double) GetLoadedData(string loadsheetType);
        
        /// <summary>
        /// Gets the FMS flight number
        /// </summary>
        /// <returns>The FMS flight number</returns>
        string GetFMSFlightNumber();
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        /// <returns>The current fuel amount</returns>
        double GetFuelAmount();
        
        /// <summary>
        /// Sets the PCA service state
        /// </summary>
        /// <param name="enable">Whether to enable or disable the service</param>
        void SetServicePCA(bool enable);
        
        /// <summary>
        /// Sets the chocks service state
        /// </summary>
        /// <param name="enable">Whether to enable or disable the service</param>
        void SetServiceChocks(bool enable);
        
        /// <summary>
        /// Sets the GPU service state
        /// </summary>
        /// <param name="enable">Whether to enable or disable the service</param>
        void SetServiceGPU(bool enable);
        
        /// <summary>
        /// Triggers the final loadsheet
        /// </summary>
        void TriggerFinal();
        
        /// <summary>
        /// Sets the initial fuel
        /// </summary>
        void SetInitialFuel();
        
        /// <summary>
        /// Sets the initial fluids
        /// </summary>
        void SetInitialFluids();
        
        /// <summary>
        /// Gets the hydraulic fluid values
        /// </summary>
        /// <returns>A tuple containing the hydraulic fluid values</returns>
        (double, double, double) GetHydraulicFluidValues();
        
        /// <summary>
        /// Starts refueling
        /// </summary>
        void RefuelStart();
        
        /// <summary>
        /// Performs refueling
        /// </summary>
        /// <returns>True if refueling is complete, false otherwise</returns>
        bool Refuel();
        
        /// <summary>
        /// Stops refueling
        /// </summary>
        void RefuelStop();
        
        /// <summary>
        /// Randomizes passenger seating
        /// </summary>
        /// <param name="trueCount">The number of passengers</param>
        /// <returns>An array of boolean values representing passenger seating</returns>
        bool[] RandomizePaxSeating(int trueCount);
        
        /// <summary>
        /// Starts boarding
        /// </summary>
        void BoardingStart();
        
        /// <summary>
        /// Performs boarding
        /// </summary>
        /// <param name="paxCurrent">The current passenger count</param>
        /// <param name="cargoCurrent">The current cargo amount</param>
        /// <returns>True if boarding is complete, false otherwise</returns>
        bool Boarding(int paxCurrent, int cargoCurrent);
        
        /// <summary>
        /// Stops boarding
        /// </summary>
        void BoardingStop();
        
        /// <summary>
        /// Starts deboarding
        /// </summary>
        void DeboardingStart();
        
        /// <summary>
        /// Performs deboarding
        /// </summary>
        /// <param name="paxCurrent">The current passenger count</param>
        /// <param name="cargoCurrent">The current cargo amount</param>
        /// <returns>True if deboarding is complete, false otherwise</returns>
        bool Deboarding(int paxCurrent, int cargoCurrent);
        
        /// <summary>
        /// Stops deboarding
        /// </summary>
        void DeboardingStop();
        
        /// <summary>
        /// Sets the aft right door state
        /// </summary>
        /// <param name="open">Whether to open or close the door</param>
        void SetAftRightDoor(bool open);
        
        /// <summary>
        /// Sets the forward right door state
        /// </summary>
        /// <param name="open">Whether to open or close the door</param>
        void SetForwardRightDoor(bool open);
        
        /// <summary>
        /// Sets the forward cargo door state
        /// </summary>
        /// <param name="open">Whether to open or close the door</param>
        void SetForwardCargoDoor(bool open);
        
        /// <summary>
        /// Sets the aft cargo door state
        /// </summary>
        /// <param name="open">Whether to open or close the door</param>
        void SetAftCargoDoor(bool open);
        
        /// <summary>
        /// Gets a value from ProSim using a data reference
        /// </summary>
        /// <param name="dataRef">The data reference</param>
        /// <returns>The value</returns>
        dynamic GetStatusFunction(string dataRef);
        
        /// <summary>
        /// Event raised when the connection state changes
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        
        /// <summary>
        /// Event raised when a flight plan is loaded
        /// </summary>
        event EventHandler<FlightPlanLoadedEventArgs> FlightPlanLoaded;
    }
}
