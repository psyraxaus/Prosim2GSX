﻿using Prosim2GSX.Models;
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
            int zone1Capacity = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone1.capacity");
            int zone2Capacity = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone2.capacity");
            int zone3Capacity = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone3.capacity");
            int zone4Capacity = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone4.capacity");
            int totalCapacity = zone1Capacity + zone2Capacity + zone3Capacity + zone4Capacity;

            // Get cargo hold capacities
            double fwdCargoCapacity = _prosimController.Interface.GetProsimVariable("aircraft.cargo.forward.capacity");
            double aftCargoCapacity = _prosimController.Interface.GetProsimVariable("aircraft.cargo.aft.capacity");

            // Get planned values from flight plan
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

            // Get fuel values from flight plan
            double plannedFuel = flightPlan.Fuel;

            // Distribute fuel with standard ratios
            double centerTankRatio = 0.35;
            double leftTankRatio = 0.325;
            double rightTankRatio = 0.325;

            double centerTankFuel = plannedFuel * centerTankRatio * randomFactor;
            double leftTankFuel = plannedFuel * leftTankRatio * randomFactor;
            double rightTankFuel = plannedFuel * rightTankRatio * randomFactor;
            double totalFuel = centerTankFuel + leftTankFuel + rightTankFuel;

            // Operating empty weight parameters
            double emptyWeight = _prosimController.Interface.GetProsimVariable("aircraft.weight.empty");

            // Calculate passenger and cargo weights
            double passengerWeight = (zone1Pax + zone2Pax + zone3Pax + zone4Pax) * PassengerWeight;
            double cargoWeight = fwdCargoWeight + aftCargoWeight;

            // Calculate ZFW and TOW
            double zfwWeight = emptyWeight + passengerWeight + cargoWeight;
            double towWeight = zfwWeight + totalFuel;

            // Use the estimated weights from flight plan if available
            if (flightPlan.EstimatedZeroFuelWeight > 0)
            {
                zfwWeight = flightPlan.EstimatedZeroFuelWeight;
            }
            
            if (flightPlan.EstimatedTakeOffWeight > 0)
            {
                towWeight = flightPlan.EstimatedTakeOffWeight;
            }

            // Calculate TOW MAC percentage - use a reasonable default for preliminary
            const double minCG = 20;  // Minimum CG percentage
            const double maxCG = 39;  // Maximum CG percentage
            
            // For preliminary, use a reasonable TOW MAC percentage in the middle of the range
            double towMacPercentage = 28.5; // Middle of typical range
            
            // Calculate fuel index for CG adjustment
            int adjustmentIndex = (int)Math.Floor(totalFuel / 100.0);
            
            // Calculate ZFW MAC percentage using the same method as final loadsheet
            double zfwMacPercentage = CalculateZFWCG(adjustmentIndex, towMacPercentage);

            // Calculate landing weight using planned trip fuel from flight plan
            double plannedTripFuel = flightPlan.Fuel - flightPlan.FuelLanding;
            double landingWeight = towWeight - plannedTripFuel;

            // If landing weight from flight plan is available, use it
            if (flightPlan.EstimatedLandingWeight > 0)
            {
                landingWeight = flightPlan.EstimatedLandingWeight;
            }

            LoadsheetData result = new LoadsheetData
            {
                ZeroFuelWeight = zfwWeight,
                ZeroFuelWeightMac = zfwMacPercentage,
                TakeoffWeight = towWeight,
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

            Logger.Log(LogLevel.Information, "A320WeightAndBalance:CalculatePreliminaryLoadsheet",
                $"Preliminary calculations:\n" +
                $"ZFW: {zfwWeight:F0} kg, MAC: {zfwMacPercentage:F2}%\n" +
                $"TOW: {towWeight:F0} kg, MAC: {towMacPercentage:F2}%\n" +
                $"Fuel Index: {adjustmentIndex}\n" +
                $"Pax zones: 1:{zone1Pax}/{zone1Capacity}, 2:{zone2Pax}/{zone2Capacity}, " +
                $"3:{zone3Pax}/{zone3Capacity}, 4:{zone4Pax}/{zone4Capacity}\n" +
                $"Cargo: Forward:{fwdCargoWeight:F0} kg/{fwdCargoCapacity:F0} kg, Aft:{aftCargoWeight:F0} kg/{aftCargoCapacity:F0} kg\n" +
                $"Fuel: Center:{centerTankFuel:F0} kg, Left:{leftTankFuel:F0} kg, Right:{rightTankFuel:F0} kg");

            return result;
        }

        public LoadsheetData CalculateFinalLoadsheet()
        {
            // Get actual passenger distribution from aircraft datarefs
            int zone1Pax = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone1.amount");
            int zone2Pax = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone2.amount");
            int zone3Pax = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone3.amount");
            int zone4Pax = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone4.amount");

            // Get zone capacities for validation/logging
            int zone1Capacity = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone1.capacity");
            int zone2Capacity = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone2.capacity");
            int zone3Capacity = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone3.capacity");
            int zone4Capacity = _prosimController.Interface.GetProsimVariable("aircraft.passengers.zone4.capacity");

            // Get cargo capacities for validation
            double fwdCargoCapacity = _prosimController.Interface.GetProsimVariable("aircraft.cargo.forward.capacity");
            double aftCargoCapacity = _prosimController.Interface.GetProsimVariable("aircraft.cargo.aft.capacity");

            int totalPax = zone1Pax + zone2Pax + zone3Pax + zone4Pax;

            // Get cargo weights from aircraft datarefs
            double fwdCargoWeight = _prosimController.Interface.GetProsimVariable("aircraft.cargo.forward.amount"); // kg
            double aftCargoWeight = _prosimController.Interface.GetProsimVariable("aircraft.cargo.aft.amount"); // kg

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
            double centerTankFuel = _prosimController.Interface.GetProsimVariable("aircraft.fuel.center.amount.kg");
            double leftTankFuel = _prosimController.Interface.GetProsimVariable("aircraft.fuel.left.amount.kg");
            double rightTankFuel = _prosimController.Interface.GetProsimVariable("aircraft.fuel.right.amount.kg");
            double totalFuel = centerTankFuel + leftTankFuel + rightTankFuel;

            // Get direct values from Prosim datarefs
            double zfwWeight = _prosimController.Interface.GetProsimVariable("aircraft.weight.zfw");
            double towWeight = _prosimController.Interface.GetProsimVariable("aircraft.weight.gross");

            // Get CG percentage from Prosim - this is what the EFB uses
            double grossWeightCG = _prosimController.Interface.GetProsimVariable("aircraft.grossWeightCG");

            // Calculate aircraft total mass in kg (ZFW + total fuel)
            double acMassKG = zfwWeight + totalFuel;

            // Calculate fuel ratio
            double fuelRatio = totalFuel / towWeight;

            // Calculate TOW MAC percentage - use the value directly from Prosim
            double towMacPercentage = grossWeightCG;

            // Calculate CG percentages based on the EFB code logic
            const double minCG = 20;  // Minimum CG percentage
            const double maxCG = 39;  // Maximum CG percentage

            // Calculate the base ratio
            double baseRatio = 175.0 / 1150.0;

            // Calculate k (intermediate CG position)
            double k = baseRatio + (grossWeightCG - minCG) * (0.6 - baseRatio) / (maxCG - minCG);

            int adjustmentIndex = (int)Math.Floor(totalFuel / 100.0);

            double zfwMacPercentage = CalculateZFWCG(adjustmentIndex, grossWeightCG);

            // Calculate landing weight
            double plannedTripFuel = _prosimController.GetPlannedTripFuel();
            double landingWeight = towWeight - plannedTripFuel;

            LoadsheetData result = new LoadsheetData
            {
                ZeroFuelWeight = zfwWeight,
                ZeroFuelWeightMac = zfwMacPercentage,
                TakeoffWeight = towWeight,
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
                $"ZFW: {zfwWeight:F0} kg, MAC: {zfwMacPercentage:F2}%\n" +
                $"TOW: {towWeight:F0} kg, MAC: {towMacPercentage:F2}%\n" +
                $"Fuel Index: {adjustmentIndex}\n" +
                $"Pax zones: 1:{zone1Pax}/{zone1Capacity}, 2:{zone2Pax}/{zone2Capacity}, " +
                $"3:{zone3Pax}/{zone3Capacity}, 4:{zone4Pax}/{zone4Capacity}\n" +
                $"Cargo: Forward:{fwdCargoWeight:F0} kg/{fwdCargoCapacity:F0} kg, Aft:{aftCargoWeight:F0} kg/{aftCargoCapacity:F0} kg\n" +
                $"Fuel: Center:{centerTankFuel:F0} kg, Left:{leftTankFuel:F0} kg, Right:{rightTankFuel:F0} kg");

            return result;
        }

        public double CalculateZFWCG(int fuelIndex, double grossWeightCG)
        {
            // Get the adjustment value from the array
            double adjustment = GetCGAdjustmentForFuel(fuelIndex);

            // Calculate using the adjustment array: ZFWCG = GWCG - adjustment
            return grossWeightCG - adjustment;
        }

        public double ConvertToMacPercentage(double absoluteCG)
        {
            return -100.0 * (absoluteCG - LeadingEdgeMAC) / MACSize;
        }

        public double GetCGAdjustmentForFuel(int fuelIndex)
        {
            // Use the actual values from the EFB's zfwcgAdjArray
            string[] zfwcgAdjArray = new string[] {
                "0", "0", "-0.0611692667008015", "-0.0611692667008015", "-0.1217812299729", "-0.1217812299729",
                "-0.181853771209703", "-0.181853771209703", "-0.241386890411402", "-0.241386890411402",
                "-0.3003895282746", "-0.3003895282746", "-0.358882546424901", "-0.358882546424901",
                "-0.4168510437012", "-0.4168510437012", "-0.474315881729201", "-0.474315881729201",
                "-0.531265139579801", "-0.531265139579801", "-0.5877315998078", "-0.5877315998078",
                "-0.643709301948601", "-0.643709301948601", "-0.6992042064667", "-0.6992042064667",
                "-0.754219293594399", "-0.754219293594399", "-0.808769464492801", "-0.808769464492801",
                "-0.8628606796265", "-0.8628606796265", "-0.916483998298702", "-0.916483998298702",
                "-0.969660282135003", "-0.969660282135003", "-1.0223835706711", "-1.0223835706711",
                "-1.0746657848358", "-1.0746657848358", "-1.1265188455582", "-1.1265188455582",
                "-1.1779397726059", "-1.1779397726059", "-1.2289375066757", "-1.2289375066757",
                "-1.2795120477677", "-1.2795120477677", "-1.3296782970429", "-1.3296782970429",
                "-1.3794332742691", "-1.3794332742691", "-1.4287739992142", "-1.4287739992142",
                "-1.4777272939682", "-1.4777272939682", "-1.5262752771378", "-1.5262752771378",
                "-1.5744417905808", "-1.5744417905808", "-1.5744417905808", "-1.6222298145294",
                "-1.6696184873581", "-1.6696184873581", "-1.7166495323181", "-1.7166495323181",
                "-1.7632991075516", "-1.7632991075516", "-1.8095850944519", "-1.8095850944519",
                "-1.8555045127869", "-1.8555045127869", "-1.9010722637177", "-1.9010722637177",
                "-1.9462764263153", "-1.9462764263153", "-1.9911348819733", "-1.9911348819733",
                "-2.0356476306916", "-2.0356476306916", "-2.0798236131668", "-2.0798236131668",
                "-2.1236568689347", "-2.1236568689347", "-2.1671563386917", "-2.1671563386917",
                "-2.2103130817414", "-2.2103130817414", "-2.2531598806381", "-2.2531598806381",
                "-2.2956669330597", "-2.2956669330597", "-2.2956669330597", "-2.3378670215607",
                "-2.3797512054444", "-2.3797512054444", "-2.4213135242462", "-2.4213135242462",
                "-2.4625778198242", "-2.4625778198242", "-2.5035381317139", "-2.5035381317139",
                "-2.5441855192185", "-2.5441855192185", "-2.5845408439636", "-2.5845408439636",
                "-2.624598145485", "-2.624598145485", "-2.6643633842469", "-2.6643633842469",
                "-2.7038455009461", "-2.7038455009461", "-2.7430355548859", "-2.7430355548859",
                "-2.781942486763", "-2.781942486763", "-2.8205648064614", "-2.8205648064614",
                "-2.858929336071", "-2.858929336071", "-2.858929336071", "-2.8969958424568",
                "-2.9348060488701", "-2.9348060488701", "-2.9723450541497", "-2.9723450541497",
                "-2.9723450541497", "-3.0413046479225", "-3.1658902764321", "-3.1658902764321",
                "-3.2896220684052", "-3.2896220684052", "-3.4124657511711", "-3.4124657511711",
                "-3.5344690084458", "-3.5344690084458", "-3.6556199193001", "-3.6556199193001",
                "-3.7759393453598", "-3.7759393453598", "-3.8954108953476", "-3.8954108953476",
                "-4.0140777826309", "-4.0140777826309", "-4.1319265961647", "-4.1319265961647",
                "-4.2489781975746", "-4.2489781975746", "-4.3652310967446", "-4.3652310967446",
                "-4.4806927442551", "-4.4806927442551", "-4.5953720808029", "-4.5953720808029",
                "-4.7092869877816", "-4.7092869877816", "-4.822438955307", "-4.822438955307",
                "-4.9348339438439", "-4.9348339438439", "-4.9348339438439", "-5.0464734435082",
                "-5.0464734435082", "-5.157370865345", "-5.2675396203995", "-5.2675396203995",
                "-5.3769871592522", "-5.3769871592522", "-5.3769871592522", "-5.4856985807419",
                "-5.5937111377716", "-5.5937111377716", "-5.7010143995285", "-5.7010143995285",
                "-5.8076098561287", "-5.8076098561287", "-5.9135258197785", "-5.9135258197785",
                "-6.0187488794327", "-6.0187488794327", "-6.0187488794327", "-6.123298406601",
                "-6.2271744012833", "-6.2271744012833", "-6.3303917646408", "-6.3303917646408",
                "-6.4329296350479", "-6.4329296350479", "-6.5348356962204", "-6.5348356962204",
                "-6.636081635952", "-6.636081635952"
            };  

            // If the index is out of range, handle it safely
            if (fuelIndex < 0)
            {
                return double.Parse(zfwcgAdjArray[0]);
            }

            if (fuelIndex >= zfwcgAdjArray.Length)
            {
                return double.Parse(zfwcgAdjArray[zfwcgAdjArray.Length - 1]);
            }

            // Convert the string value to a double
            // Note: these values appear to be negative in the array, but the formula is GWCG - adjustment
            // so we want to return the direct value from the array
            return double.Parse(zfwcgAdjArray[fuelIndex]);
        }
    }
}
