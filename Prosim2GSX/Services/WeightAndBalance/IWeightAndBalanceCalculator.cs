using Prosim2GSX.Models;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services.WeightAndBalance
{
    /// <summary>
    /// Interface for aircraft weight and balance calculations
    /// </summary>
    public interface IWeightAndBalanceCalculator
    {
        /// <summary>
        /// Calculate preliminary loadsheet based on flight plan data
        /// </summary>
        /// <param name="flightPlan">The flight plan data</param>
        /// <returns>Tuple with weight and balance values</returns>
        LoadsheetData CalculatePreliminaryLoadsheet(FlightPlan flightPlan);

        /// <summary>
        /// Calculate final loadsheet based on actual loaded aircraft
        /// </summary>
        /// <returns>Tuple with weight and balance values</returns>
        LoadsheetData CalculateFinalLoadsheet();

        /// <summary>
        /// Convert from absolute CG position to MAC percentage
        /// </summary>
        /// <param name="absoluteCG">CG position in meters from reference</param>
        /// <returns>CG position as percentage MAC</returns>
        double ConvertToMacPercentage(double absoluteCG);
    }

    /// <summary>
    /// Container for loadsheet calculation results
    /// </summary>
    public class LoadsheetData
    {
        public double ZeroFuelWeight { get; set; }
        public double ZeroFuelWeightCG { get; set; }
        public double ZeroFuelWeightMac { get; set; }
        public double TakeoffWeight { get; set; }
        public double TakeoffWeightCG { get; set; }
        public double TakeoffWeightMac { get; set; }
        public double FuelWeight { get; set; }
        public double LandingWeight { get; set; }
        public Dictionary<int, int> PassengersByZone { get; set; } = new Dictionary<int, int>();
        public int TotalPassengers { get; set; }
        public double ForwardCargoWeight { get; set; }
        public double AftCargoWeight { get; set; }
        public Dictionary<string, double> FuelByTank { get; set; } = new Dictionary<string, double>();
    }
}