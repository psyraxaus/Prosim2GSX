using Prosim2GSX.State;
using ProsimInterface;
using System;
using System.Globalization;

namespace Prosim2GSX.Services
{
    // SimbriefResponse → OFPData projector. Centralised here so unit
    // conversion + numeric parsing live in one place — used by both the
    // manual EFB-INIT fetch and the MCDU-triggered auto-fetch.
    //
    // Unit handling: SimBrief profiles configured in lbs return weight and
    // fuel values in lbs (params.units = "lbs"). We always convert to kg
    // here so OFPData consumers never have to worry about units.
    public static class SimbriefOfpMapper
    {
        public static OFPData ToOFPData(SimbriefResponse ofp, double weightConversion)
        {
            if (ofp == null) return null;

            bool isLbs = string.Equals(ofp.Params?.Units, "lbs", StringComparison.OrdinalIgnoreCase);
            double conv = (isLbs && weightConversion > 0) ? weightConversion : 1.0;

            return new OFPData
            {
                OfpId = ofp.Params?.RequestId ?? "",
                DepartureIcao = ofp.Origin?.IcaoCode ?? "",
                ArrivalIcao = ofp.Destination?.IcaoCode ?? "",
                AlternateIcao = ofp.Alternate?.IcaoCode ?? "",
                FlightNumber = string.IsNullOrWhiteSpace(ofp.General?.FlightNumber)
                    ? ""
                    : $"{ofp.General?.IcaoAirline}{ofp.General?.FlightNumber}",
                AirlineIcao = ofp.General?.IcaoAirline ?? "",
                Callsign = ofp.Atc?.Callsign ?? "",

                ZfwKg = ParseKg(ofp.Weights?.EstZfw, conv),
                OewKg = ParseKg(ofp.Weights?.Oew, conv),

                FuelRampKg = ParseKg(ofp.Fuel?.PlanRamp, conv),
                FuelTripKg = ParseKg(ofp.Fuel?.EnrouteBurn, conv),
                FuelContingencyKg = ParseKg(ofp.Fuel?.Contingency, conv),
                FuelAlternateKg = ParseKg(ofp.Fuel?.AlternateBurn, conv),
                FuelMinimumKg = ParseKg(ofp.Fuel?.MinTakeoff, conv),
                FuelExtraKg = ParseKg(ofp.Fuel?.Extra, conv),
                FuelTaxiKg = ParseKg(ofp.Fuel?.Taxi, conv),
                FuelReserveKg = ParseKg(ofp.Fuel?.Reserve, conv),

                PassengerCount = ParseInt(ofp.Weights?.PaxCount),
                CargoKg = ParseKg(ofp.Weights?.Cargo, conv),

                CruiseFlightLevel = ParseFlightLevel(ofp.General?.InitialAltitude),
                CostIndex = ParseInt(ofp.General?.CostIndex),
                Route = ofp.General?.Route ?? "",
                DeparturePlanRwy = ofp.Origin?.PlanRwy ?? "",
                ArrivalPlanRwy = ofp.Destination?.PlanRwy ?? "",

                Std = ParseTime(ofp.Times?.SchedOut),
                Eta = ParseTime(ofp.Times?.EstOn),

                AircraftType = string.IsNullOrWhiteSpace(ofp.Aircraft?.Engines)
                    ? (ofp.Aircraft?.IcaoCode ?? "")
                    : $"{ofp.Aircraft?.IcaoCode} / {ofp.Aircraft?.Engines}",
                AircraftReg = ofp.Aircraft?.Reg ?? "",
                AircraftEngines = ofp.Aircraft?.Engines ?? "",

                FetchedAt = DateTime.UtcNow,
            };
        }

        private static double ParseKg(string raw, double conv)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0;
            if (!double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return 0;
            return conv > 0 ? v / conv : v;
        }

        private static int ParseInt(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0;
            return int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        // SimBrief returns initial_altitude as feet ("37000"). Convert to FL.
        private static int ParseFlightLevel(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0;
            if (!double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var feet)) return 0;
            return (int)Math.Round(feet / 100.0);
        }

        // SimBrief json=1 returns times as Unix epoch strings; json=v2 returns
        // ISO 8601. Try both — the SDK currently fetches json=1 but the model
        // may carry either depending on the entry point.
        private static DateTime? ParseTime(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (long.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var epoch) && epoch > 0)
                return DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var iso))
                return iso;
            return null;
        }
    }
}
