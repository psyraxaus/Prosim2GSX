using System;

namespace Prosim2GSX.State
{
    // Plain data carrier for a parsed SimBrief OFP. Lives inside
    // EfbFlightPlanState.CurrentOfp; replaced wholesale on each fetch so the
    // observable property's INPC fires once per OFP load.
    public class OFPData
    {
        public string OfpId { get; set; } = "";
        public string DepartureIcao { get; set; } = "";
        public string ArrivalIcao { get; set; } = "";
        public string AlternateIcao { get; set; } = "";
        public string FlightNumber { get; set; } = "";
        public string AirlineIcao { get; set; } = "";
        public string Callsign { get; set; } = "";

        // All weights in kg. SimBrief profiles configured in lbs are converted
        // at parse time so downstream consumers always see kg.
        public double ZfwKg { get; set; }
        public double OewKg { get; set; }

        public double FuelRampKg { get; set; }
        public double FuelTripKg { get; set; }
        public double FuelContingencyKg { get; set; }
        public double FuelAlternateKg { get; set; }
        public double FuelMinimumKg { get; set; }
        public double FuelExtraKg { get; set; }
        public double FuelTaxiKg { get; set; }
        public double FuelReserveKg { get; set; }

        public int PassengerCount { get; set; }
        public double CargoKg { get; set; }

        public int CruiseFlightLevel { get; set; }
        public int CostIndex { get; set; }
        public string Route { get; set; } = "";
        public string DeparturePlanRwy { get; set; } = "";
        public string ArrivalPlanRwy { get; set; } = "";

        // STD = scheduled out (off blocks). Eta = estimated touchdown
        // (Airbus FMS convention — wheels-down, not in-blocks).
        public DateTime? Std { get; set; }
        public DateTime? Eta { get; set; }

        public string AircraftType { get; set; } = "";
        public string AircraftReg { get; set; } = "";
        public string AircraftEngines { get; set; } = "";

        public DateTime FetchedAt { get; set; }
    }
}
