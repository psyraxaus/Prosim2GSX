using Prosim2GSX.Models;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services.WeightAndBalance
{
    /// <summary>
    /// A320-specific weight and balance calculator
    /// </summary>
    public class A320WeightAndBalance : IWeightAndBalanceCalculator
    {
        private readonly ProsimController _prosimController;
        private readonly MobiSimConnect _simConnect;

        // A320 specific constants
        private const double OperatingEmptyCG = -9.536; // feet from flight_model.cfg
        private const double LeadingEdgeMAC = -5.383; // feet from reference
        private const double MACSize = 13.464; // feet
        private const double PassengerWeight = 77.0; // kg per passenger standard for MSFS

        // Arms (distances from reference datum) for different sections
        private const double Zone1Arm = 35.095; // Business class/cockpit flight_model.cfg
        private const double Zone2Arm = 9.664; // Forward economy flight_model.cfg
        private const double Zone3Arm = 0; // Mid economy flight_model.cfg
        private const double Zone4Arm = -36.362; // Aft economy flight_model.cfg

        private const double FwdCargoArm = 9.333; //Forward cargo from flight_model.cfg
        private const double AftCargoArm = -43.139; // Aft cargo from flight_model.cfg

        private const double CenterTankArm = -8.018100;
        private const double LeftTankArm = -8.396782;
        private const double RightTankArm = -8.396782;

        public A320WeightAndBalance(ProsimController prosimController, MobiSimConnect simConnect)
        {
            _prosimController = prosimController;
            _simConnect = simConnect;
        }

        public LoadsheetData CalculatePreliminaryLoadsheet(FlightPlan flightPlan)
        {
            // Get zone capacities
            int zone1Capacity = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone1.capacity");
            int zone2Capacity = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone2.capacity");
            int zone3Capacity = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone3.capacity");
            int zone4Capacity = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone4.capacity");
            int totalCapacity = zone1Capacity + zone2Capacity + zone3Capacity + zone4Capacity;

            // Get cargo hold capacities
            double fwdCargoCapacity = _prosimController.Interface.ReadProsimVariable("aircraft.cargo.forward.capacity");
            double aftCargoCapacity = _prosimController.Interface.ReadProsimVariable("aircraft.cargo.aft.capacity");

            // Get planned values from flight plan
            //int totalPax = flightPlan.PassengerCount;
            int totalPax = flightPlan.Passenger;
            double loadFactor = Math.Min(1.0, (double)totalPax / totalCapacity);

            // Add randomization for preliminary estimates
            Random rnd = new Random();
            double randomFactor = 0.98 + (rnd.NextDouble() * 0.04); // ±2% variation

            // Distribute passengers by zone capacity with slight randomization
            int zone1Pax = (int)(zone1Capacity * loadFactor * (0.98 + rnd.NextDouble() * 0.04));
            int zone2Pax = (int)(zone2Capacity * loadFactor * (0.98 + rnd.NextDouble() * 0.04));
            int zone3Pax = (int)(zone3Capacity * loadFactor * (0.98 + rnd.NextDouble() * 0.04));
            int zone4Pax = (int)(zone4Capacity * loadFactor * (0.98 + rnd.NextDouble() * 0.04));

            // Adjust to ensure total matches plan
            int allocatedPax = zone1Pax + zone2Pax + zone3Pax + zone4Pax;
            if (allocatedPax != totalPax)
            {
                // Distribute difference proportionally
                int difference = totalPax - allocatedPax;
                zone1Pax += (int)(difference * ((double)zone1Capacity / totalCapacity));
                zone2Pax += (int)(difference * ((double)zone2Capacity / totalCapacity));
                zone3Pax += (int)(difference * ((double)zone3Capacity / totalCapacity));
                zone4Pax = totalPax - zone1Pax - zone2Pax - zone3Pax; // Ensure exact match
            }

            // Rest of cargo and fuel calculations
            double totalCargo = flightPlan.CargoTotal;

            // Distribute cargo according to capacity with slight randomization, but respect capacity limits
            double fwdCargoRatio = 0.45 + (rnd.NextDouble() * 0.05 - 0.025); // 45% ±2.5%
            double fwdCargoWeight = Math.Min(totalCargo * fwdCargoRatio, fwdCargoCapacity);

            // If forward cargo is at capacity, put the rest in aft (up to its capacity)
            double aftCargoWeight = Math.Min(totalCargo - fwdCargoWeight, aftCargoCapacity);

            // Check if we exceeded total capacity and log a warning if needed
            double totalCargoCapacity = fwdCargoCapacity + aftCargoCapacity;
            if (totalCargo > totalCargoCapacity)
            {
                Logger.Log(LogLevel.Warning, "A320WeightAndBalance:CalculatePreliminaryLoadsheet",
                    $"Warning: Total cargo ({totalCargo:F0} kg) exceeds combined cargo hold capacity ({totalCargoCapacity:F0} kg). " +
                    $"Limiting to maximum capacity.");

                // Adjust the actual cargo to fit within limits
                double excessCargo = totalCargo - totalCargoCapacity;
                totalCargo = totalCargoCapacity;
            }

            double plannedFuel = flightPlan.Fuel;

            // Distribute fuel with standard ratios
            double centerTankRatio = 0.35;
            double leftTankRatio = 0.325;
            double rightTankRatio = 0.325;

            double centerTankFuel = plannedFuel * centerTankRatio * randomFactor;
            double leftTankFuel = plannedFuel * leftTankRatio * randomFactor;
            double rightTankFuel = plannedFuel * rightTankRatio * randomFactor;

            // Operating empty weight parameters
            double emptyWeight = _prosimController.Interface.ReadProsimVariable("aircraft.weight.empty");
            double emptyCG = OperatingEmptyCG;

            // Calculate moments
            double emptyMoment = emptyWeight * emptyCG;

            double zone1Moment = zone1Pax * PassengerWeight * Zone1Arm;
            double zone2Moment = zone2Pax * PassengerWeight * Zone2Arm;
            double zone3Moment = zone3Pax * PassengerWeight * Zone3Arm;
            double zone4Moment = zone4Pax * PassengerWeight * Zone4Arm;

            double fwdCargoMoment = fwdCargoWeight * FwdCargoArm;
            double aftCargoMoment = aftCargoWeight * AftCargoArm;

            double centerTankMoment = centerTankFuel * CenterTankArm;
            double leftTankMoment = leftTankFuel * LeftTankArm;
            double rightTankMoment = rightTankFuel * RightTankArm;

            // Calculate ZFW components
            double passengerWeight = (zone1Pax + zone2Pax + zone3Pax + zone4Pax) * PassengerWeight;
            double cargoWeight = fwdCargoWeight + aftCargoWeight;

            double zfwWeight = emptyWeight + passengerWeight + cargoWeight;
            double zfwMoment = emptyMoment + zone1Moment + zone2Moment + zone3Moment + zone4Moment +
                              fwdCargoMoment + aftCargoMoment;

            // Calculate TOW components
            double totalFuel = centerTankFuel + leftTankFuel + rightTankFuel;
            double towWeight = zfwWeight + totalFuel;
            double towMoment = zfwMoment + centerTankMoment + leftTankMoment + rightTankMoment;

            // Calculate CGs
            double zfwCG = zfwMoment / zfwWeight;
            double towCG = towMoment / towWeight;

            // Calculate MAC percentages
            double zfwMacPercentage = ConvertToMacPercentage(zfwCG);
            double towMacPercentage = ConvertToMacPercentage(towCG);

            // Calculate landing weight (simple estimation for preliminary)
            double landingFuelEstimate = (centerTankFuel + leftTankFuel + rightTankFuel) * 0.85; // 15% fuel burn
            double landingWeight = zfwWeight + landingFuelEstimate;

            LoadsheetData result = new LoadsheetData
            {
                ZeroFuelWeight = zfwWeight,
                ZeroFuelWeightCG = zfwCG,
                ZeroFuelWeightMac = zfwMacPercentage,
                TakeoffWeight = towWeight,
                TakeoffWeightCG = towCG,
                TakeoffWeightMac = towMacPercentage,
                FuelWeight = centerTankFuel + leftTankFuel + rightTankFuel,
                LandingWeight = landingWeight,
                TotalPassengers = totalPax,
                ForwardCargoWeight = fwdCargoWeight,
                AftCargoWeight = aftCargoWeight,
                PassengersByZone = new Dictionary<int, int>
                {
                    { 1, zone1Pax },
                    { 2, zone2Pax },
                    { 3, zone3Pax },
                    { 4, zone4Pax }
                },
                FuelByTank = new Dictionary<string, double>
                {
                    { "Center", centerTankFuel },
                    { "Left", leftTankFuel },
                    { "Right", rightTankFuel }
                }
            };

            Logger.Log(LogLevel.Information, "A320WeightAndBalance:CalculatePreliminaryLoadsheet",
                $"Preliminary calculations:\n" +
                $"ZFW: {zfwWeight:F0} kg, CG: {zfwCG:F2} m, MAC: {zfwMacPercentage:F2}%\n" +
                $"TOW: {towWeight:F0} kg, CG: {towCG:F2} m, MAC: {towMacPercentage:F2}%\n" +
                $"Pax zones: 1:{zone1Pax}/{zone1Capacity}, 2:{zone2Pax}/{zone2Capacity}, " +
                $"3:{zone3Pax}/{zone3Capacity}, 4:{zone4Pax}/{zone4Capacity}\n" +
                $"Cargo: Forward:{fwdCargoWeight:F0} kg/{fwdCargoCapacity:F0} kg, Aft:{aftCargoWeight:F0} kg/{aftCargoCapacity:F0} kg\n" +
                $"Fuel: Center:{centerTankFuel:F0} kg, Left:{leftTankFuel:F0} kg, Right:{rightTankFuel:F0} kg");

            return result;
        }

        public LoadsheetData CalculateFinalLoadsheet()
        {
            // Get actual passenger distribution from aircraft datarefs
            int zone1Pax = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone1.amount");
            int zone2Pax = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone2.amount");
            int zone3Pax = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone3.amount");
            int zone4Pax = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone4.amount");

            // Get zone capacities for validation/logging
            int zone1Capacity = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone1.capacity");
            int zone2Capacity = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone2.capacity");
            int zone3Capacity = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone3.capacity");
            int zone4Capacity = _prosimController.Interface.ReadProsimVariable("aircraft.passengers.zone4.capacity");

            // Get cargo capacities for validation
            double fwdCargoCapacity = _prosimController.Interface.ReadProsimVariable("aircraft.cargo.forward.capacity");
            double aftCargoCapacity = _prosimController.Interface.ReadProsimVariable("aircraft.cargo.aft.capacity");

            int totalPax = zone1Pax + zone2Pax + zone3Pax + zone4Pax;

            // Get cargo weights from aircraft datarefs
            double fwdCargoWeight = _prosimController.Interface.ReadProsimVariable("aircraft.cargo.forward.amount"); // kg
            double aftCargoWeight = _prosimController.Interface.ReadProsimVariable("aircraft.cargo.aft.amount"); // kg

            // Validate cargo weights against capacities
            if (fwdCargoWeight > fwdCargoCapacity)
            {
                Logger.Log(LogLevel.Warning, "A320WeightAndBalance:CalculateFinalLoadsheet",
                    $"Warning: Forward cargo ({fwdCargoWeight:F0} kg) exceeds capacity ({fwdCargoCapacity:F0} kg).");
                fwdCargoWeight = fwdCargoCapacity;
            }

            if (aftCargoWeight > aftCargoCapacity)
            {
                Logger.Log(LogLevel.Warning, "A320WeightAndBalance:CalculateFinalLoadsheet",
                    $"Warning: Aft cargo ({aftCargoWeight:F0} kg) exceeds capacity ({aftCargoCapacity:F0} kg).");
                aftCargoWeight = aftCargoCapacity;
            }

            // Get actual fuel quantities
            double centerTankFuel = _prosimController.Interface.ReadProsimVariable("aircraft.fuel.center.amount.kg");
            double leftTankFuel = _prosimController.Interface.ReadProsimVariable("aircraft.fuel.left.amount.kg");
            double rightTankFuel = _prosimController.Interface.ReadProsimVariable("aircraft.fuel.right.amount.kg");
            double totalFuel = centerTankFuel + leftTankFuel + rightTankFuel;

            // Operating empty weight parameters
            double emptyWeight = _prosimController.Interface.ReadProsimVariable("aircraft.weight.empty");
            double emptyCG = OperatingEmptyCG;

            // Calculate moments
            double emptyMoment = emptyWeight * emptyCG;

            double zone1Moment = zone1Pax * PassengerWeight * Zone1Arm;
            double zone2Moment = zone2Pax * PassengerWeight * Zone2Arm;
            double zone3Moment = zone3Pax * PassengerWeight * Zone3Arm;
            double zone4Moment = zone4Pax * PassengerWeight * Zone4Arm;

            double fwdCargoMoment = fwdCargoWeight * FwdCargoArm;
            double aftCargoMoment = aftCargoWeight * AftCargoArm;

            double centerTankMoment = centerTankFuel * CenterTankArm;
            double leftTankMoment = leftTankFuel * LeftTankArm;
            double rightTankMoment = rightTankFuel * RightTankArm;

            // Calculate ZFW (zero fuel weight)
            double passengerWeight = (zone1Pax + zone2Pax + zone3Pax + zone4Pax) * PassengerWeight;
            double cargoWeight = fwdCargoWeight + aftCargoWeight;

            double zfwWeight = emptyWeight + passengerWeight + cargoWeight;
            double zfwMoment = emptyMoment + zone1Moment + zone2Moment + zone3Moment + zone4Moment + fwdCargoMoment + aftCargoMoment;

            // Calculate TOW (take-off weight)
            double towWeight = zfwWeight + totalFuel;
            double towMoment = zfwMoment + centerTankMoment + leftTankMoment + rightTankMoment;

            // Calculate CGs
            double zfwCG = zfwMoment / zfwWeight;
            double towCG = towMoment / towWeight;

            // Calculate MAC percentages
            double zfwMacPercentage = ConvertToMacPercentage(zfwCG);
            double towMacPercentage = ConvertToMacPercentage(towCG);

            // Calculate landing weight (simple estimation)
            double plannedTripFuel = _prosimController.GetPlannedTripFuel();
            double landingWeight = towWeight - plannedTripFuel;

            LoadsheetData result = new LoadsheetData
            {
                ZeroFuelWeight = zfwWeight,
                ZeroFuelWeightCG = zfwCG,
                ZeroFuelWeightMac = zfwMacPercentage,
                TakeoffWeight = towWeight,
                TakeoffWeightCG = towCG,
                TakeoffWeightMac = towMacPercentage,
                FuelWeight = totalFuel,
                LandingWeight = landingWeight,
                TotalPassengers = totalPax,
                ForwardCargoWeight = fwdCargoWeight,
                AftCargoWeight = aftCargoWeight,
                PassengersByZone = new Dictionary<int, int>
                {
                    { 1, zone1Pax },
                    { 2, zone2Pax },
                    { 3, zone3Pax },
                    { 4, zone4Pax }
                },
                FuelByTank = new Dictionary<string, double>
                {
                    { "Center", centerTankFuel },
                    { "Left", leftTankFuel },
                    { "Right", rightTankFuel }
                }
            };

            Logger.Log(LogLevel.Information, "A320WeightAndBalance:CalculateFinalLoadsheet",
                $"Final calculations:\n" +
                $"ZFW: {zfwWeight:F0} kg, CG: {zfwCG:F2} m, MAC: {zfwMacPercentage:F2}%\n" +
                $"TOW: {towWeight:F0} kg, CG: {towCG:F2} m, MAC: {towMacPercentage:F2}%\n" +
                $"Pax zones: 1:{zone1Pax}/{zone1Capacity}, 2:{zone2Pax}/{zone2Capacity}, " +
                $"3:{zone3Pax}/{zone3Capacity}, 4:{zone4Pax}/{zone4Capacity}\n" +
                $"Cargo: Forward:{fwdCargoWeight:F0} kg/{fwdCargoCapacity:F0} kg, Aft:{aftCargoWeight:F0} kg/{aftCargoCapacity:F0} kg\n" +
                $"Fuel: Center:{centerTankFuel:F0} kg, Left:{leftTankFuel:F0} kg, Right:{rightTankFuel:F0} kg");

            return result;
        }

        public double ConvertToMacPercentage(double absoluteCG)
        {
            return -100.0 * (absoluteCG - LeadingEdgeMAC) / MACSize;
        }
    }
}