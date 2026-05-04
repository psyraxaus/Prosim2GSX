using CFIT.AppLogger;
using Prosim2GSX.State;
using ProsimInterface;
using System;
using System.Globalization;

namespace Prosim2GSX.Services
{
    // Reads ProSim datarefs each StateUpdateWorker tick and writes the values
    // into WeightBalanceState. Polling rather than dataref-Subscribe matches
    // the project-wide convention (every other state mirror — GsxState,
    // FlightStatusState — is populated by polling helpers from the same
    // worker), and the [ObservableProperty] setters give equality-based
    // compare-and-skip so WS broadcasts only fire on actual change.
    //
    // Dataref readability notes from ProsimDataref.csv:
    //   - aircraft.fms.init.zfw and aircraft.fms.init.zfwcg are WRITE-only
    //     and cannot be subscribed/read. The readable equivalents are
    //     aircraft.weight.zfw (kg) and aircraft.zfwcg (%MAC), used here.
    //   - aircraft.cargo.bulk has only .capacity readable; there is no
    //     .amount counterpart, so bulk loaded weight is intentionally not
    //     surfaced (the field was dropped from the spec).
    //   - fuel.plan_ramp does not exist; planned fuel is sourced from the
    //     last loaded SimBrief OFP (AircraftInterface.LastSimbriefOfp.Fuel.PlanRamp),
    //     parsed as kg.
    public class WeightBalanceService
    {
        private readonly AppService _app;

        public WeightBalanceService(AppService app)
        {
            _app = app;
        }

        // Called from StateUpdateWorker.OnTick. Null-safe across degraded modes
        // (no SDK, no GsxService, no aircraft loaded) — leaves the store at
        // whatever it last held instead of forcing zeros that would jitter the
        // chart on transient disconnect.
        public virtual void Tick()
        {
            try
            {
                var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
                if (sdk == null || !sdk.IsConnected) return;

                var s = _app.WeightBalance;
                if (s == null) return;

                // ZFW + MACZFW. zfwcg is the readable equivalent of fms.init.zfwcg.
                s.ZfwKg = ReadDouble(sdk, "aircraft.weight.zfw");
                s.MaczfwPercent = ReadDouble(sdk, "aircraft.zfwcg");

                // Fuel — current vs capacity. aircraft.fuel.total.amount.kg is
                // the kg-locked alias of aircraft.fuel.total.amount.
                s.FuelInTanksKg = ReadDouble(sdk, "aircraft.fuel.total.amount.kg");
                s.FuelCapacityKg = ReadDouble(sdk, "aircraft.fuel.total.capacity");

                // GW = ZFW + fuel. Compute even when one side is zero so a
                // freshly-loaded aircraft (zero fuel) still shows a sensible
                // GW for the chart.
                s.GwKg = s.ZfwKg + s.FuelInTanksKg;

                // MACGW lookup — index = floor(fuel / 100kg), clamped to
                // [0, 199]. The 200-entry array's paired values produce
                // intentional flat segments; do NOT collapse.
                int idx = (int)Math.Floor(s.FuelInTanksKg / WeightBalanceState.FuelStepKg);
                if (idx < 0) idx = 0;
                if (idx >= WeightBalanceState.ZfwcgAdjArray.Length)
                    idx = WeightBalanceState.ZfwcgAdjArray.Length - 1;
                s.MacgwPercent = s.MaczfwPercent + WeightBalanceState.ZfwcgAdjArray[idx];

                // MACTOW currently mirrors MACGW until a separate take-off
                // weight calculation is available. MacTowError stays false
                // until we wire envelope-bounds checking.
                s.MactowPercent = s.MacgwPercent;
                s.MacTowError = false;

                // Cargo holds.
                s.CargoFwdLoadedKg = ReadDouble(sdk, "aircraft.cargo.forward.amount");
                s.CargoFwdCapacityKg = ReadDouble(sdk, "aircraft.cargo.forward.capacity");
                s.CargoAftLoadedKg = ReadDouble(sdk, "aircraft.cargo.aft.amount");
                s.CargoAftCapacityKg = ReadDouble(sdk, "aircraft.cargo.aft.capacity");
                s.CargoBulkCapacityKg = ReadDouble(sdk, "aircraft.cargo.bulk.capacity");
                s.CargoPlannedKg = ReadDouble(sdk, "efb.plannedCargoKg");

                // Cargo doors.
                s.FwdCargoDoorOpen = ReadBool(sdk, "doors.cargo.forward");
                s.AftCargoDoorOpen = ReadBool(sdk, "doors.cargo.aft");

                // Passengers — capacities are static per aircraft, but reading
                // each tick is cheap (SDK-side cache) and adapts when the
                // aircraft is reloaded with a different config.
                s.Zone1Capacity = ReadInt(sdk, "aircraft.passengers.zone1.capacity");
                s.Zone2Capacity = ReadInt(sdk, "aircraft.passengers.zone2.capacity");
                s.Zone3Capacity = ReadInt(sdk, "aircraft.passengers.zone3.capacity");
                s.Zone4Capacity = ReadInt(sdk, "aircraft.passengers.zone4.capacity");

                // Seat occupation drives the SVG cabin layout. The "boarded"
                // count is the number of 'T' characters in that string —
                // matches how the EFB derives boarded count from the same
                // dataref.
                var seats = sdk.GetString("aircraft.passengers.seatOccupation.string", "") ?? "";
                s.SeatOccupation = seats;
                s.PassengersBoarded = CountTrueChars(seats);

                // Planned pax count — efb.passengerStatistics is an Object
                // with a "totalNumOfPaxs" or similar field; the simpler
                // fallback is aircraft.passengers.booked.string which holds
                // the planned occupation pattern. The 'T' count there is the
                // booked total.
                var booked = sdk.GetString("aircraft.passengers.booked.string", "") ?? "";
                s.PassengersPlanned = CountTrueChars(booked);

                // Planned fuel from SimBrief OFP. The cached LastSimbriefOfp
                // refreshes when the user re-imports an OFP; null when no
                // OFP has been loaded yet.
                //
                // Unit handling: SimBrief returns fuel in whatever unit the
                // user's airline profile is configured for, advertised on
                // ofp.Params.Units ("kgs" or "lbs"). SimbriefService converts
                // before pushing to Prosim, but LastSimbriefOfp keeps the raw
                // string, so we have to apply the same conversion here. All
                // other reads in this service are kg-locked by ProsimDataref.csv
                // (the columns marked "kg" do not flip with the avionics
                // display-unit setting — only aircraft.fuel.total.amount has
                // a unit-dependent base name, and we use the .kg alias).
                var ofp = _app?.GsxService?.AircraftInterface?.LastSimbriefOfp;
                double plannedRaw = ParseDoubleOrZero(ofp?.Fuel?.PlanRamp);
                bool simbriefIsLbs = string.Equals(ofp?.Params?.Units, "lbs", StringComparison.OrdinalIgnoreCase);
                double conv = _app?.Config?.WeightConversion ?? 2.2046226218;
                s.FuelPlannedKg = simbriefIsLbs && conv > 0 ? plannedRaw / conv : plannedRaw;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private static double ReadDouble(ProsimSdkInterface sdk, string dataRef)
        {
            try
            {
                var raw = sdk.ReadDataRef(dataRef);
                if (raw == null) return 0.0;
                return Convert.ToDouble(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0.0;
            }
        }

        private static int ReadInt(ProsimSdkInterface sdk, string dataRef)
        {
            try
            {
                var raw = sdk.ReadDataRef(dataRef);
                if (raw == null) return 0;
                return Convert.ToInt32(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        private static bool ReadBool(ProsimSdkInterface sdk, string dataRef)
        {
            try
            {
                return sdk.GetBool(dataRef, false);
            }
            catch
            {
                return false;
            }
        }

        private static double ParseDoubleOrZero(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0.0;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0.0;
        }

        private static int CountTrueChars(string seats)
        {
            if (string.IsNullOrEmpty(seats)) return 0;
            int n = 0;
            for (int i = 0; i < seats.Length; i++)
            {
                if (seats[i] == 'T' || seats[i] == 't' || seats[i] == '1') n++;
            }
            return n;
        }
    }
}
