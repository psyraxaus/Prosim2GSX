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
    }
}