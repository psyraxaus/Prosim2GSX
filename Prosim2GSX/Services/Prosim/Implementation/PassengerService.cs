using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Linq;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class PassengerService : IPassengerService
    {
        private readonly IProsimInterface _prosimService;
        private readonly ServiceModel _model;
        private readonly ICargoService _cargoService;

        private bool[] _paxPlanned;
        private bool[] _paxCurrent;
        private int[] _paxSeats;
        private int _paxLast;
        private bool _randomizePaxSeat = false;

        public int PassengersZone1 { get; private set; }
        public int PassengersZone2 { get; private set; }
        public int PassengersZone3 { get; private set; }
        public int PassengersZone4 { get; private set; }

        public PassengerService(
            IProsimInterface prosimService,
            ServiceModel model,
            ICargoService cargoService)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _cargoService = cargoService ?? throw new ArgumentNullException(nameof(cargoService));

            _paxCurrent = new bool[132];
            _paxSeats = null;
        }

        public void UpdatePassengerData(FlightPlan flightPlan, bool forceCurrent)
        {
            try
            {
                if (_model.FlightPlanType == "MCDU")
                {
                    if (!_randomizePaxSeat)
                    {
                        _paxPlanned = RandomizePassengerSeating(flightPlan.Passenger);
                        LogService.Log(LogLevel.Debug, nameof(PassengerService),
                            $"seatOccupation bool: {string.Join(", ", _paxPlanned)}");

                        _prosimService.SetProsimVariable("aircraft.passengers.seatOccupation", _paxPlanned);
                        _prosimService.SetProsimVariable("efb.passengers.booked", _paxPlanned);

                        PassengersZone1 = _prosimService.GetProsimVariable("aircraft.passengers.zone1.amount");
                        PassengersZone2 = _prosimService.GetProsimVariable("aircraft.passengers.zone2.amount");
                        PassengersZone3 = _prosimService.GetProsimVariable("aircraft.passengers.zone3.amount");
                        PassengersZone4 = _prosimService.GetProsimVariable("aircraft.passengers.zone4.amount");

                        _randomizePaxSeat = true;
                    }

                    if (forceCurrent)
                        _paxCurrent = _paxPlanned;
                }
                else
                {
                    _paxPlanned = _prosimService.GetProsimVariable("efb.passengers.booked");

                    if (forceCurrent)
                        _paxCurrent = _paxPlanned;
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(PassengerService),
                    $"Exception during UpdatePassengerData: {ex.Message}");
            }
        }

        public int GetPlannedPassengers()
        {
            return _paxPlanned != null ? _paxPlanned.Count(i => i) : 0;
        }

        public int GetCurrentPassengers()
        {
            return _paxCurrent.Count(i => i);
        }

        public bool[] RandomizePassengerSeating(int trueCount)
        {
            bool[] result = new bool[132];
            int actualCount = trueCount;

            // Check if trueCount exceeds maximum capacity
            if (trueCount > 132)
            {
                LogService.Log(LogLevel.Warning, nameof(PassengerService),
                    $"Requested passenger count {trueCount} exceeds maximum capacity. Reducing to 132.");
                actualCount = 132;
            }
            else if (trueCount < 0)
            {
                throw new ArgumentException("The number of passengers must be at least 0.");
            }

            // Initialize all to false
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = false;
            }

            // Fill the array with 'true' values at random positions
            Random rand = new Random();
            int count = 0;
            while (count < actualCount)
            {
                int index = rand.Next(132);
                if (!result[index])
                {
                    result[index] = true;
                    count++;
                }
            }

            return result;
        }

        public void StartBoarding()
        {
            _paxLast = 0;
            _paxSeats = new int[GetPlannedPassengers()];
            int n = 0;
            for (int i = 0; i < _paxPlanned.Length; i++)
            {
                if (_paxPlanned[i])
                {
                    _paxSeats[n] = i;
                    n++;
                }
            }
        }

        public bool ProcessBoarding(int paxCurrent, int cargoCurrent)
        {
            BoardPassengers(paxCurrent - _paxLast);
            _paxLast = paxCurrent;

            _cargoService.UpdateCargoLoading(cargoCurrent);

            return paxCurrent == GetPlannedPassengers() && cargoCurrent == 100;
        }

        public void StopBoarding()
        {
            _paxSeats = null;
            if (_model.FlightPlanType == "EFB")
            {
                _prosimService.SetProsimVariable("efb.efb.boardingStatus", "ended");
            }
        }

        public void StartDeboarding()
        {
            LogService.Log(LogLevel.Debug, nameof(PassengerService),
                $"(planned {GetPlannedPassengers()}) (current {GetCurrentPassengers()})");

            _paxLast = GetPlannedPassengers();

            if (GetCurrentPassengers() != GetPlannedPassengers())
                _paxCurrent = _paxPlanned;
        }

        public bool ProcessDeboarding(int paxCurrent, int cargoCurrent)
        {
            DeboardPassengers(_paxLast - paxCurrent);
            _paxLast = paxCurrent;

            int cargoCurrentValue = 100 - cargoCurrent;
            _cargoService.UpdateCargoLoading(cargoCurrentValue);

            return paxCurrent == 0 && cargoCurrentValue == 0;
        }

        public void StopDeboarding()
        {
            _cargoService.UpdateCargoLoading(0);

            for (int i = 0; i < _paxCurrent.Length; i++)
                _paxCurrent[i] = false;

            LogService.Log(LogLevel.Debug, nameof(PassengerService), "Sending SeatString");
            SendSeatString(true);

            _paxCurrent = new bool[132];
            _paxSeats = null;
        }

        private void BoardPassengers(int num)
        {
            if (num < 0)
            {
                LogService.Log(LogLevel.Debug, nameof(PassengerService), "Passenger Num was below 0!");
                return;
            }
            else if (num > 15)
            {
                LogService.Log(LogLevel.Debug, nameof(PassengerService), "Passenger Num was above 15!");
                return;
            }
            else
                LogService.Log(LogLevel.Debug, nameof(PassengerService),
                    $"(num {num}) (current {GetCurrentPassengers()}) (planned ({GetPlannedPassengers()}))");

            int n = 0;
            for (int i = _paxLast; i < _paxLast + num && i < GetPlannedPassengers(); i++)
            {
                _paxCurrent[_paxSeats[i]] = true;
                n++;
            }

            if (n > 0)
                SendSeatString();
        }

        private void DeboardPassengers(int num)
        {
            if (num < 0)
            {
                LogService.Log(LogLevel.Debug, nameof(PassengerService), "Passenger Num was below 0!");
                return;
            }
            else if (num > 15)
            {
                LogService.Log(LogLevel.Debug, nameof(PassengerService), "Passenger Num was above 15!");
                return;
            }
            else
                LogService.Log(LogLevel.Debug, nameof(PassengerService),
                    $"(num {num}) (current {GetCurrentPassengers()}) (planned ({GetPlannedPassengers()}))");

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
                SendSeatString();
        }

        private void SendSeatString(bool force = false)
        {
            string seatString = "";
            bool first = true;

            if (GetCurrentPassengers() == 0 && !force)
                return;

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

            LogService.Log(LogLevel.Debug, nameof(PassengerService), seatString);
            _prosimService.SetProsimVariable("aircraft.passengers.seatOccupation.string", seatString);
        }

        /// <inheritdoc/>
        public void UpdatePassengerStatistics()
        {
            try
            {
                // Read the zone amounts, catching any exceptions
                int business = 0, economy1 = 0, economy2 = 0, economy3 = 0;

                try { business = _prosimService.GetProsimVariable("aircraft.passengers.zone1.amount"); } catch { }
                try { economy1 = _prosimService.GetProsimVariable("aircraft.passengers.zone2.amount"); } catch { }
                try { economy2 = _prosimService.GetProsimVariable("aircraft.passengers.zone3.amount"); } catch { }
                try { economy3 = _prosimService.GetProsimVariable("aircraft.passengers.zone4.amount"); } catch { }

                int totalZones = business + economy1 + economy2 + economy3;

                // Count the total number of true values in _paxPlanned (occupied seats)
                int totalPlannedPassengers = 0;
                if (_paxPlanned != null && _paxPlanned is bool[])
                {
                    bool[] seatOccupation = _paxPlanned as bool[];
                    totalPlannedPassengers = seatOccupation.Count(seat => seat);
                }

                // If zones aren't populated, distribute passengers evenly
                if (totalZones == 0 && totalPlannedPassengers > 0)
                {
                    LogService.Log(LogLevel.Warning, nameof(PassengerService),
                        "Zone amounts not available. Distributing passengers evenly across zones.");

                    // Simple even distribution algorithm - you can adjust based on your A320 cabin config
                    int remaining = totalPlannedPassengers;

                    // Distribute across zones based on seating capacity ratios
                    // Adjust these percentages based on your A320 model's cabin layout
                    business = (int)(remaining * 0.4); // 40% in first zone
                    remaining -= business;

                    economy1 = (int)(remaining * 0.5); // 50% of remaining in second zone
                    remaining -= economy1;

                    // Split the remainder between Zone3 and Zone4
                    economy2 = (int)(remaining * 0.7); // 70% of remaining to zone3
                    economy3 = remaining - economy2; // Rest to zone4

                    LogService.Log(LogLevel.Debug, nameof(PassengerService),
                        $"Distributed {totalPlannedPassengers} passengers: Business: {business}, Economy 1: {economy1}, Economy 2: {economy2}, Economy 3: {economy3}");
                }

                // Calculate totals
                int totalPax = totalZones > 0 ? totalZones : totalPlannedPassengers;
                int businessPax = business; // Adjust based on your configuration if needed
                int economyPax = totalPax - businessPax;

                // Combine Zone3 and Zone4 for Section3
                int paxSection3 = economy2 + economy3;

                // Create the passenger statistics object
                var passengerStatistics = new
                {
                    NumOfPaxInBusiness = businessPax,
                    NumOfPaxInEconomy = economyPax,
                    NumOfPaxInSection1 = business,
                    NumOfPaxInSection2 = economy1,
                    NumOfPaxInSection3 = paxSection3, // Combined Zone3 and Zone4
                    Total = totalPax
                };

                // Serialize and set in Prosim
                string passengerJson = Newtonsoft.Json.JsonConvert.SerializeObject(passengerStatistics);
                LogService.Log(LogLevel.Debug, nameof(PassengerService),
                    $"Setting efb.passengerStatistics: {passengerJson}");

                _prosimService.SetProsimVariable("efb.passengerStatistics", passengerJson);
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(PassengerService),
                    $"Exception during UpdatePassengerStatistics: {ex.Message}");
            }
        }
    }
}