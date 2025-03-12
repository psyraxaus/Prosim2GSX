using System;
using System.Linq;

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
        /// Updates passenger data from EFB
        /// </summary>
        /// <param name="paxPlanned">The passenger seating arrangement from EFB</param>
        /// <param name="forceCurrentUpdate">Whether to update current passenger state to match planned</param>
        public void UpdateFromEFB(bool[] paxPlanned, bool forceCurrentUpdate = false)
        {
            try
            {
                if (paxPlanned == null)
                {
                    throw new ArgumentNullException(nameof(paxPlanned));
                }
                
                // Copy the seating arrangement
                _paxPlanned = (bool[])paxPlanned.Clone();
                
                // Log the updated seating arrangement
                int passengerCount = _paxPlanned.Count(i => i);
                Logger.Log(LogLevel.Debug, "ProsimPassengerService:UpdateFromEFB", 
                    $"Updated passenger seating for {passengerCount} passengers from EFB");
                    
                // Update ProSim with the new seating arrangement
                _prosimService.SetVariable("aircraft.passengers.seatOccupation", _paxPlanned);
                
                // Update passenger zone counts
                UpdatePassengerZones();
                
                // Set the flag to indicate seating has been randomized
                _hasRandomizedSeating = true;
                
                // If requested, also update current passenger state to match planned
                if (forceCurrentUpdate)
                {
                    _paxCurrent = (bool[])_paxPlanned.Clone();
                    OnPassengerStateChanged("UpdatedFromEFB", GetPaxCurrent(), GetPaxPlanned());
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimPassengerService:UpdateFromEFB", 
                    $"Error updating from EFB: {ex.Message}");
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
