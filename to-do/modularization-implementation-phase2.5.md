# Prosim2GSX Modularization Implementation - Phase 2.5: ProsimPassengerService

## Overview

This document details the implementation of Phase 2.5 of the Prosim2GSX modularization strategy, which involves extracting passenger-related functionality from the ProsimController into a dedicated ProsimPassengerService.

## Implementation Details

### 1. Create IProsimPassengerService Interface

First, we'll create a new interface `IProsimPassengerService` in the Services folder that defines the contract for passenger-related operations:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for managing passenger operations in ProSim
    /// </summary>
    public interface IProsimPassengerService
    {
        /// <summary>
        /// Event raised when passenger state changes
        /// </summary>
        event EventHandler<PassengerStateChangedEventArgs> PassengerStateChanged;
        
        /// <summary>
        /// Gets the number of passengers in Zone 1
        /// </summary>
        int PaxZone1 { get; }
        
        /// <summary>
        /// Gets the number of passengers in Zone 2
        /// </summary>
        int PaxZone2 { get; }
        
        /// <summary>
        /// Gets the number of passengers in Zone 3
        /// </summary>
        int PaxZone3 { get; }
        
        /// <summary>
        /// Gets the number of passengers in Zone 4
        /// </summary>
        int PaxZone4 { get; }
        
        /// <summary>
        /// Creates a randomized seating arrangement for the specified number of passengers
        /// </summary>
        /// <param name="trueCount">The number of passengers to seat</param>
        /// <returns>A boolean array representing the seating arrangement</returns>
        bool[] RandomizePaxSeating(int trueCount);
        
        /// <summary>
        /// Updates passenger data from a flight plan
        /// </summary>
        /// <param name="passengerCount">The number of passengers from the flight plan</param>
        /// <param name="forceCurrentUpdate">Whether to update current passenger state to match planned</param>
        void UpdateFromFlightPlan(int passengerCount, bool forceCurrentUpdate = false);
        
        /// <summary>
        /// Starts the boarding process
        /// </summary>
        void BoardingStart();
        
        /// <summary>
        /// Processes boarding for the specified number of passengers and cargo percentage
        /// </summary>
        /// <param name="paxCurrent">The current number of boarded passengers</param>
        /// <param name="cargoCurrent">The current cargo percentage</param>
        /// <param name="cargoChangeCallback">Callback to handle cargo changes</param>
        /// <returns>True if boarding is complete, false otherwise</returns>
        bool Boarding(int paxCurrent, int cargoCurrent, Action<int> cargoChangeCallback);
        
        /// <summary>
        /// Stops the boarding process
        /// </summary>
        void BoardingStop();
        
        /// <summary>
        /// Starts the deboarding process
        /// </summary>
        void DeboardingStart();
        
        /// <summary>
        /// Processes deboarding for the specified number of passengers and cargo percentage
        /// </summary>
        /// <param name="paxCurrent">The current number of remaining passengers</param>
        /// <param name="cargoCurrent">The current cargo percentage</param>
        /// <param name="cargoChangeCallback">Callback to handle cargo changes</param>
        /// <returns>True if deboarding is complete, false otherwise</returns>
        bool Deboarding(int paxCurrent, int cargoCurrent, Action<int> cargoChangeCallback);
        
        /// <summary>
        /// Stops the deboarding process
        /// </summary>
        void DeboardingStop();
        
        /// <summary>
        /// Gets the planned number of passengers
        /// </summary>
        /// <returns>The planned number of passengers</returns>
        int GetPaxPlanned();
        
        /// <summary>
        /// Gets the current number of passengers
        /// </summary>
        /// <returns>The current number of passengers</returns>
        int GetPaxCurrent();
        
        /// <summary>
        /// Checks if passenger seating has been randomized
        /// </summary>
        /// <returns>True if seating has been randomized, false otherwise</returns>
        bool HasRandomizedSeating();
        
        /// <summary>
        /// Resets the randomization flag
        /// </summary>
        void ResetRandomization();
    }
    
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
}
```

### 2. Create ProsimPassengerService Implementation

Next, we'll implement the service class that provides the actual functionality:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for managing passenger operations in ProSim
    /// </summary>
    public class ProsimPassengerService : IProsimPassengerService
    {
        private readonly IProsimService _prosimService;
        private bool[] _paxPlanned;
        private bool[] _paxCurrent;
        private int[] _paxSeats;
        private int _paxLast;
        private bool _hasRandomizedSeating = false;
        
        /// <summary>
        /// Gets the number of passengers in Zone 1
        /// </summary>
        public int PaxZone1 { get; private set; }
        
        /// <summary>
        /// Gets the number of passengers in Zone 2
        /// </summary>
        public int PaxZone2 { get; private set; }
        
        /// <summary>
        /// Gets the number of passengers in Zone 3
        /// </summary>
        public int PaxZone3 { get; private set; }
        
        /// <summary>
        /// Gets the number of passengers in Zone 4
        /// </summary>
        public int PaxZone4 { get; private set; }
        
        /// <summary>
        /// Event raised when passenger state changes
        /// </summary>
        public event EventHandler<PassengerStateChangedEventArgs> PassengerStateChanged;
        
        /// <summary>
        /// Creates a new instance of ProsimPassengerService
        /// </summary>
        /// <param name="prosimService">The ProSim service to use for communication with ProSim</param>
        /// <exception cref="ArgumentNullException">Thrown if prosimService is null</exception>
        public ProsimPassengerService(IProsimService prosimService)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _paxCurrent = new bool[132];
            _paxPlanned = new bool[132];
            
            // Initialize passenger zone counts
            UpdatePassengerZones();
        }
        
        /// <summary>
        /// Creates a randomized seating arrangement for the specified number of passengers
        /// </summary>
        /// <param name="trueCount">The number of passengers to seat</param>
        /// <returns>A boolean array representing the seating arrangement</returns>
        /// <exception cref="ArgumentException">Thrown if trueCount is less than 0 or greater than 132</exception>
        public bool[] RandomizePaxSeating(int trueCount)
        {
            if (trueCount < 0 || trueCount > 132)
            {
                throw new ArgumentException("The number of passengers must be between 0 and 132.");
            }
            
            bool[] result = new bool[132];
            
            // Initialize all to false
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = false;
            }
            
            // Fill the array with 'true' values at random positions
            Random rand = new Random();
            int count = 0;
            while (count < trueCount)
            {
                int index = rand.Next(132);
                if (!result[index])
                {
                    result[index] = true;
                    count++;
                }
            }
            
            // Set the flag to indicate seating has been randomized
            _hasRandomizedSeating = true;
            
            return result;
        }
        
        /// <summary>
        /// Updates passenger data from a flight plan
        /// </summary>
        /// <param name="passengerCount">The number of passengers from the flight plan</param>
        /// <param name="forceCurrentUpdate">Whether to update current passenger state to match planned</param>
        public void UpdateFromFlightPlan(int passengerCount, bool forceCurrentUpdate = false)
        {
            try
            {
                _paxPlanned = RandomizePaxSeating(passengerCount);
                
                // Log the updated seating arrangement
                Logger.Log(LogLevel.Debug, "ProsimPassengerService:UpdateFromFlightPlan", 
                    $"Updated passenger seating for {passengerCount} passengers");
                    
                // Update ProSim with the new seating arrangement
                _prosimService.SetVariable("aircraft.passengers.seatOccupation", _paxPlanned);
                
                // Update passenger zone counts
                UpdatePassengerZones();
                
                // If requested, also update current passenger state to match planned
                if (forceCurrentUpdate)
                {
                    _paxCurrent = (bool[])_paxPlanned.Clone();
                    OnPassengerStateChanged("UpdatedFromFlightPlan", GetPaxCurrent(), GetPaxPlanned());
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:UpdateFromFlightPlan", 
                    $"Error updating from flight plan: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Starts the boarding process
        /// </summary>
        public void BoardingStart()
        {
            try
            {
                _paxLast = 0;
                _paxSeats = new int[GetPaxPlanned()];
                int n = 0;
                for (int i = 0; i < _paxPlanned.Length; i++)
                {
                    if (_paxPlanned[i])
                    {
                        _paxSeats[n] = i;
                        n++;
                    }
                }
                
                Logger.Log(LogLevel.Information, "ProsimPassengerService:BoardingStart", 
                    $"Started boarding process for {GetPaxPlanned()} passengers");
                
                OnPassengerStateChanged("BoardingStart", GetPaxCurrent(), GetPaxPlanned());
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:BoardingStart", 
                    $"Error starting boarding: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Processes boarding for the specified number of passengers and cargo percentage
        /// </summary>
        /// <param name="paxCurrent">The current number of boarded passengers</param>
        /// <param name="cargoCurrent">The current cargo percentage</param>
        /// <param name="cargoChangeCallback">Callback to handle cargo changes</param>
        /// <returns>True if boarding is complete, false otherwise</returns>
        public bool Boarding(int paxCurrent, int cargoCurrent, Action<int> cargoChangeCallback)
        {
            try
            {
                BoardPassengers(paxCurrent - _paxLast);
                _paxLast = paxCurrent;
                
                // Call back to the controller to handle cargo changes
                cargoChangeCallback?.Invoke(cargoCurrent);
                
                bool isComplete = paxCurrent == GetPaxPlanned() && cargoCurrent == 100;
                
                if (isComplete)
                {
                    Logger.Log(LogLevel.Information, "ProsimPassengerService:Boarding", 
                        "Boarding process complete");
                }
                
                return isComplete;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:Boarding", 
                    $"Error during boarding: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Stops the boarding process
        /// </summary>
        public void BoardingStop()
        {
            try
            {
                _paxSeats = null;
                
                // If using EFB, update boarding status
                _prosimService.SetVariable("efb.efb.boardingStatus", "ended");
                
                Logger.Log(LogLevel.Information, "ProsimPassengerService:BoardingStop", 
                    "Boarding process stopped");
                
                OnPassengerStateChanged("BoardingStop", GetPaxCurrent(), GetPaxPlanned());
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:BoardingStop", 
                    $"Error stopping boarding: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Starts the deboarding process
        /// </summary>
        public void DeboardingStart()
        {
            try
            {
                Logger.Log(LogLevel.Debug, "ProsimPassengerService:DeboardingStart", 
                    $"(planned {GetPaxPlanned()}) (current {GetPaxCurrent()})");
                
                _paxLast = GetPaxPlanned();
                
                if (GetPaxCurrent() != GetPaxPlanned())
                {
                    _paxCurrent = (bool[])_paxPlanned.Clone();
                }
                
                OnPassengerStateChanged("DeboardingStart", GetPaxCurrent(), GetPaxPlanned());
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:DeboardingStart", 
                    $"Error starting deboarding: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Processes deboarding for the specified number of passengers and cargo percentage
        /// </summary>
        /// <param name="paxCurrent">The current number of remaining passengers</param>
        /// <param name="cargoCurrent">The current cargo percentage</param>
        /// <param name="cargoChangeCallback">Callback to handle cargo changes</param>
        /// <returns>True if deboarding is complete, false otherwise</returns>
        public bool Deboarding(int paxCurrent, int cargoCurrent, Action<int> cargoChangeCallback)
        {
            try
            {
                DeboardPassengers(_paxLast - paxCurrent);
                _paxLast = paxCurrent;
                
                // Call back to the controller to handle cargo changes
                int adjustedCargoValue = 100 - cargoCurrent;
                cargoChangeCallback?.Invoke(adjustedCargoValue);
                
                bool isComplete = paxCurrent == 0 && adjustedCargoValue == 0;
                
                if (isComplete)
                {
                    Logger.Log(LogLevel.Information, "ProsimPassengerService:Deboarding", 
                        "Deboarding process complete");
                }
                
                return isComplete;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:Deboarding", 
                    $"Error during deboarding: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Stops the deboarding process
        /// </summary>
        public void DeboardingStop()
        {
            try
            {
                for (int i = 0; i < _paxCurrent.Length; i++)
                {
                    _paxCurrent[i] = false;
                }
                
                Logger.Log(LogLevel.Debug, "ProsimPassengerService:DeboardingStop", "Sending SeatString");
                SendSeatString(true);
                
                _paxCurrent = new bool[132];
                _paxSeats = null;
                
                OnPassengerStateChanged("DeboardingStop", 0, GetPaxPlanned());
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:DeboardingStop", 
                    $"Error stopping deboarding: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the planned number of passengers
        /// </summary>
        /// <returns>The planned number of passengers</returns>
        public int GetPaxPlanned()
        {
            return _paxPlanned.Count(i => i);
        }
        
        /// <summary>
        /// Gets the current number of passengers
        /// </summary>
        /// <returns>The current number of passengers</returns>
        public int GetPaxCurrent()
        {
            return _paxCurrent.Count(i => i);
        }
        
        /// <summary>
        /// Checks if passenger seating has been randomized
        /// </summary>
        /// <returns>True if seating has been randomized, false otherwise</returns>
        public bool HasRandomizedSeating()
        {
            return _hasRandomizedSeating;
        }
        
        /// <summary>
        /// Resets the randomization flag
        /// </summary>
        public void ResetRandomization()
        {
            _hasRandomizedSeating = false;
        }
        
        /// <summary>
        /// Updates passenger zone counts from ProSim
        /// </summary>
        private void UpdatePassengerZones()
        {
            try
            {
                PaxZone1 = _prosimService.ReadDataRef("aircraft.passengers.zone1.amount");
                PaxZone2 = _prosimService.ReadDataRef("aircraft.passengers.zone2.amount");
                PaxZone3 = _prosimService.ReadDataRef("aircraft.passengers.zone3.amount");
                PaxZone4 = _prosimService.ReadDataRef("aircraft.passengers.zone4.amount");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:UpdatePassengerZones", 
                    $"Error updating passenger zones: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Boards the specified number of passengers
        /// </summary>
        /// <param name="num">The number of passengers to board</param>
        private void BoardPassengers(int num)
        {
            try
            {
                if (num < 0)
                {
                    Logger.Log(LogLevel.Warning, "ProsimPassengerService:BoardPassengers", 
                        "Passenger number was below 0, ignoring request");
                    return;
                }
                else if (num > 15)
                {
                    Logger.Log(LogLevel.Warning, "ProsimPassengerService:BoardPassengers", 
                        "Passenger number was above 15, ignoring request");
                    return;
                }
                else
                {
                    Logger.Log(LogLevel.Debug, "ProsimPassengerService:BoardPassengers", 
                        $"(num {num}) (current {GetPaxCurrent()}) (planned ({GetPaxPlanned()}))");
                }
                
                int n = 0;
                for (int i = _paxLast; i < _paxLast + num && i < GetPaxPlanned(); i++)
                {
                    _paxCurrent[_paxSeats[i]] = true;
                    n++;
                }
                
                if (n > 0)
                {
                    SendSeatString();
                    OnPassengerStateChanged("BoardPassengers", GetPaxCurrent(), GetPaxPlanned());
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:BoardPassengers", 
                    $"Error boarding passengers: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Deboards the specified number of passengers
        /// </summary>
        /// <param name="num">The number of passengers to deboard</param>
        private void DeboardPassengers(int num)
        {
            try
            {
                if (num < 0)
                {
                    Logger.Log(LogLevel.Warning, "ProsimPassengerService:DeboardPassengers", 
                        "Passenger number was below 0, ignoring request");
                    return;
                }
                else if (num > 15)
                {
                    Logger.Log(LogLevel.Warning, "ProsimPassengerService:DeboardPassengers", 
                        "Passenger number was above 15, ignoring request");
                    return;
                }
                else
                {
                    Logger.Log(LogLevel.Debug, "ProsimPassengerService:DeboardPassengers", 
                        $"(num {num}) (current {GetPaxCurrent()}) (planned ({GetPaxPlanned()}))");
                }
                
                int n = 0;
                for (int i = 0; i < _paxCurrent.Length && n < num; i++)
                {
                    if (_paxCurrent[i])
                    {
                        _paxCurrent[i] = false;
                        n++;
                    }
                }
                
                if (n > 0)
                {
                    SendSeatString();
                    OnPassengerStateChanged("DeboardPassengers", GetPaxCurrent(), GetPaxPlanned());
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:DeboardPassengers", 
                    $"Error deboarding passengers: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Sends the current seating arrangement to ProSim
        /// </summary>
        /// <param name="force">Whether to force sending even if there are no passengers</param>
        private void SendSeatString(bool force = false)
        {
            try
            {
                if (GetPaxCurrent() == 0 && !force)
                {
                    return;
                }
                
                string seatString = "";
                bool first = true;
                
                foreach (var pax in _paxCurrent)
                {
                    if (first)
                    {
                        seatString = pax ? "true" : "false";
                        first = false;
                    }
                    else
                    {
                        seatString += pax ? ",true" : ",false";
                    }
                }
                
                Logger.Log(LogLevel.Debug, "ProsimPassengerService:SendSeatString", seatString);
                _prosimService.SetVariable("aircraft.passengers.seatOccupation.string", seatString);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:SendSeatString", 
                    $"Error sending seat string: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Raises the PassengerStateChanged event
        /// </summary>
        /// <param name="operationType">The type of operation that caused the state change</param>
        /// <param name="currentCount">The current number of passengers</param>
        /// <param name="plannedCount">The planned number of passengers</param>
        protected virtual void OnPassengerStateChanged(string operationType, int currentCount, int plannedCount)
        {
            PassengerStateChanged?.Invoke(this, new PassengerStateChangedEventArgs(operationType, currentCount, plannedCount));
        }
    }
}
```

### 3. Update ProsimController

Now we'll update the ProsimController to use the new ProsimPassengerService:

```csharp
// Add a private field for the passenger service
private readonly IProsimPassengerService _passengerService;

// Initialize the passenger service in the constructor
public ProsimController(ServiceModel model)
{
    Interface = new(model, _connection);
    paxCurrent = new bool[132];
    paxSeats = null;
    Model = model;
    
    // Initialize door service with the ProsimService from Interface
    _doorService = new ProsimDoorService(Interface.ProsimService);
    
    // Optionally subscribe to door state change events
    _doorService.DoorStateChanged += (sender, args) => {
        // Handle door state changes if needed
        Logger.Log(LogLevel.Debug, "ProsimController:DoorStateChanged", 
            $"{args.DoorName} is now {(args.IsOpen ? "open" : "closed")}");
    };
    
    // Initialize equipment service with the ProsimService from Interface
    _equipmentService = new ProsimEquipmentService(Interface.ProsimService);
    
    // Optionally subscribe to equipment state change events
    _equipmentService.EquipmentStateChanged += (sender, args) => {
        // Handle equipment state changes if needed
        Logger.Log(LogLevel.Debug, "ProsimController:EquipmentStateChanged", 
            $"{args.EquipmentName} is now {(args.IsEnabled ? "enabled" : "disabled")}");
    };
    
    // Initialize passenger service with the ProsimService from Interface
    _passengerService = new ProsimPassengerService(Interface.ProsimService);
    
    // Optionally subscribe to passenger state change events
    _passengerService.PassengerStateChanged += (sender, args) => {
        // Handle passenger state changes if needed
        Logger.Log(LogLevel.Debug, "ProsimController:PassengerStateChanged", 
            $"{args.OperationType}: Current: {args.CurrentCount}, Planned: {args.PlannedCount}");
    };
}

// Update the Update method to use the passenger service
public void Update(bool forceCurrent)
{
    try
    {
        double engine1 = Interface.ReadDataRef("aircraft.engine1.raw");
        double engine2 = Interface.ReadDataRef("aircraft.engine2.raw");
        enginesRunning = engine1 > 18.0D || engine2 > 18.0D;

        fuelCurrent = Interface.ReadDataRef("aircraft.fuel.total.amount.kg");

        useZeroFuel = Model.SetZeroFuel;

        fuelUnits = Interface.ReadDataRef("system.config.Units.Weight");
        if (fuelUnits == "LBS")
            fuelPlanned /= weightConversion;

        if (Model.FlightPlanType == "MCDU")
        {
            cargoPlanned = FlightPlan.CargoTotal;
            fuelPlanned = FlightPlan.Fuel;

            if (!_passengerService.HasRandomizedSeating())
            {
                _passengerService.UpdateFromFlightPlan(FlightPlan.Passenger, forceCurrent);
                
                // Update local variables for zone counts
                paxZone1 = _passengerService.PaxZone1;
                paxZone2 = _passengerService.PaxZone2;
                paxZone3 = _passengerService.PaxZone3;
                paxZone4 = _passengerService.PaxZone4;

                Interface.SetProsimVariable("aircraft.cargo.aft.amount", Convert.ToDouble(FlightPlan.CargoTotal / 2));
                Interface.SetProsimVariable("aircraft.cargo.forward.amount", Convert.ToDouble(FlightPlan.CargoTotal / 2));
                Logger.Log(LogLevel.Debug, "ProsimController:Update", $"Temp Cargo set: forward {Interface.GetProsimVariable("aircraft.cargo.forward.amount")} aft {Interface.GetProsimVariable("aircraft.cargo.aft.amount")}");
            }

            if (flightPlanID != FlightPlan.FlightPlanID)
            {
                Logger.Log(LogLevel.Information, "ProsimController:Update", $"New FlightPlan with ID {FlightPlan.FlightPlanID} detected!");
                flightPlanID = FlightPlan.FlightPlanID;
                flightNumber = FlightPlan.Flight;
            }
        }
        else
        {
            fuelPlanned = Interface.ReadDataRef("aircraft.refuel.fuelTarget");

            string str = (string)Interface.ReadDataRef("efb.loading");
            if (!string.IsNullOrWhiteSpace(str))
                int.TryParse(str[1..], out cargoPlanned);

            // Update passenger data from EFB
            bool[] paxPlannedFromEfb = Interface.ReadDataRef("efb.passengers.booked");
            if (paxPlannedFromEfb != null)
            {
                // Use the passenger service to update from EFB data
                _passengerService.UpdateFromFlightPlan(paxPlannedFromEfb.Count(p => p), forceCurrent);
            }

            JObject result = JObject.Parse((string)Interface.ReadDataRef("efb.flightTimestampJSON"));
            string innerJson = result.ToString();
            result = JObject.Parse(innerJson);
            innerJson = result["ProsimTimes"]["PRELIM_EDNO"].ToString();

            if (flightPlanID != innerJson)
            {
                Logger.Log(LogLevel.Information, "ProsimController:Update", $"New FlightPlan with ID {innerJson} detected!");
                flightPlanID = innerJson;
            }
        }
    }
    catch (Exception ex)
    {
        Logger.Log(LogLevel.Error, "ProsimController:Update", $"Exception during Update {ex.Message}");
    }
}

// Update passenger-related methods to use the passenger service
public bool[] RandomizePaxSeating(int trueCount)
{
    return _passengerService.RandomizePaxSeating(trueCount);
}

public int GetPaxPlanned()
{
    return _passengerService.GetPaxPlanned();
}

public int GetPaxCurrent()
{
    return _passengerService.GetPaxCurrent();
}

public void BoardingStart()
{
    _passengerService.BoardingStart();
}

public bool Boarding(int paxCurrent, int cargoCurrent)
{
    return _passengerService.Boarding(paxCurrent, cargoCurrent, (cargoValue) => {
        ChangeCargo(cargoValue);
        cargoLast = cargoValue;
    });
}

public void BoardingStop()
{
    _passengerService.BoardingStop();
}

public void DeboardingStart()
{
    _passengerService.DeboardingStart();
}

public bool Deboarding(int paxCurrent, int cargoCurrent)
{
    return _passengerService.Deboarding(paxCurrent, cargoCurrent, (cargoValue) => {
        ChangeCargo(cargoValue);
        cargoLast = cargoValue;
    });
}

public void DeboardingStop()
{
    _passengerService.DeboardingStop();
}

// The ChangeCargo method remains in ProsimController for now
// It will be moved to ProsimCargoService in Phase 2.6
private void ChangeCargo(int cargoCurrent)
{
    if (cargoCurrent == cargoLast)
        return;

    float cargo = (float)cargoPlanned * (float)(cargoCurrent / 100.0f);
    Interface.SetProsimVariable("aircraft.cargo.forward.amount", (float)cargo * cargoDistMain);
    Interface.SetProsimVariable("aircraft.cargo.aft.amount", (float)cargo * cargoDistMain);
}
```

## Benefits

1. **Improved Separation of Concerns**: Passenger-related functionality is now isolated in a dedicated service.
2. **Enhanced Testability**: The service can be tested independently of the controller.
3. **Better Maintainability**: Changes to passenger functionality only require modifications to the service.
4. **Consistent Pattern**: Follows the same pattern as other services in the application.
5. **Event-Based Communication**: Provides events for passenger state changes, enabling loose coupling.
6. **Improved Error Handling**: Comprehensive error handling and logging throughout the service.
7. **Better State Management**: Passenger state is properly encapsulated within the service.

## Next Steps

1. Add unit tests for ProsimPassengerService (deferred for now)
2. Proceed with Phase 2.6: ProsimCargoService implementation

## Completed Tasks

- [x] Create `IProsimPassengerService.cs` interface file
- [x] Create `ProsimPassengerService.cs` implementation file
- [x] Move passenger-related methods from ProsimController to ProsimPassengerService
  - [x] Move `RandomizePaxSeating` method
  - [x] Move `BoardingStart` method
  - [x] Move `Boarding` method
  - [x] Move `BoardPassengers` method
  - [x] Move `SendSeatString` method
  - [x] Move `BoardingStop` method
  - [x] Move `DeboardingStart` method
  - [x] Move `DeboardPassengers` method
  - [x] Move `Deboarding` method
  - [x] Move `DeboardingStop` method
- [x] Update ProsimController to use ProsimPassengerService
- [ ] Add unit tests for ProsimPassengerService (deferred)
- [ ] Test the implementation to ensure it works correctly
