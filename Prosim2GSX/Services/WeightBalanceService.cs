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

        // First-tick priming guard. Subscribe + RegisterPollDataref must run
        // once per SDK connect so the polling loop refreshes the cache —
        // without this, ReadDataRef returns the snapshot taken on first read
        // and never updates, so PropertyChanged never fires and no WS patches
        // ship. Reset on disconnect so reconnects re-prime.
        private bool _primed;

        // Every dataref this service consumes. Some overlap with FuelService
        // (RefFuelTotal, RefFuelTotalCapacity) is intentional — both
        // services Subscribe; the SDK keeps a single subscription list so
        // priming twice is harmless and the cache is shared.
        private static readonly string[] PolledRefs = new[]
        {
            // ZFW + ZFW-CG drive the W&B chart.
            ProsimConstants.RefWeightZfw,
            ProsimConstants.RefAircraftZfwcg,
            // Fuel total — sum used to compute GW = ZFW + fuel.
            ProsimConstants.RefFuelTotal,
            ProsimConstants.RefFuelTotalCapacity,
            // Cargo holds.
            ProsimConstants.RefCargoForward,
            ProsimConstants.RefCargoForwardCapacity,
            ProsimConstants.RefCargoRear,
            ProsimConstants.RefCargoAftCapacity,
            ProsimConstants.RefCargoBulkCapacity,
            ProsimConstants.RefEfbPlannedCargoKg,
            // Cargo doors.
            ProsimConstants.RefDoorCargoForward,
            ProsimConstants.RefDoorCargoAft,
            ProsimConstants.RefDoorCargoBulk,
            // Entry / overwing doors.
            ProsimConstants.RefDoor1L, ProsimConstants.RefDoor1R,
            ProsimConstants.RefDoor2L, ProsimConstants.RefDoor2R,
            ProsimConstants.RefDoor3L, ProsimConstants.RefDoor3R,
            ProsimConstants.RefDoor4L, ProsimConstants.RefDoor4R,
            // Pax-zone capacities + occupation strings.
            ProsimConstants.RefPaxZone1Capacity,
            ProsimConstants.RefPaxZone2Capacity,
            ProsimConstants.RefPaxZone3Capacity,
            ProsimConstants.RefPaxZone4Capacity,
            ProsimConstants.RefPaxCurrentString,
            ProsimConstants.RefPaxBookedString,
        };

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
                if (sdk == null || !sdk.IsConnected)
                {
                    _primed = false;
                    return;
                }

                Prime(sdk);

                var wbState = _app.WeightBalance;
                if (wbState == null) return;

                // ZFW + MACZFW. RefAircraftZfwcg is the readable equivalent
                // of RefFmsInitZfwcg (which is write-only).
                wbState.ZfwKg = ReadDouble(sdk, ProsimConstants.RefWeightZfw);
                wbState.MaczfwPercent = ReadDouble(sdk, ProsimConstants.RefAircraftZfwcg);

                // Fuel — current vs capacity. RefFuelTotal is the kg-locked
                // alias of aircraft.fuel.total.amount.
                wbState.FuelInTanksKg = ReadDouble(sdk, ProsimConstants.RefFuelTotal);
                wbState.FuelCapacityKg = ReadDouble(sdk, ProsimConstants.RefFuelTotalCapacity);

                // GW = ZFW + fuel. Compute even when one side is zero so a
                // freshly-loaded aircraft (zero fuel) still shows a sensible
                // GW for the chart.
                wbState.GwKg = wbState.ZfwKg + wbState.FuelInTanksKg;

                // MACGW lookup — index = floor(fuel / 100kg), clamped to
                // [0, 199]. The 200-entry array's paired values produce
                // intentional flat segments; do NOT collapse.
                int idx = (int)Math.Floor(wbState.FuelInTanksKg / WeightBalanceState.FuelStepKg);
                if (idx < 0) idx = 0;
                if (idx >= WeightBalanceState.ZfwcgAdjArray.Length)
                    idx = WeightBalanceState.ZfwcgAdjArray.Length - 1;
                wbState.MacgwPercent = wbState.MaczfwPercent + WeightBalanceState.ZfwcgAdjArray[idx];

                // MACTOW resolves to the loadsheet value when available
                // (final → prelim) and falls back to the live MACGW mirror
                // otherwise. MacTowError is computed against the A320
                // envelope bounds (LoadsheetState.MinMacTow/MaxMacTow). The
                // live mirror is written first so the resolver can read it
                // as the "computed" fallback in the same tick.
                wbState.MactowPercent = wbState.MacgwPercent;
                wbState.MacTowSource = "computed";
                var mactowSvc = _app?.MactowValidationService;
                if (mactowSvc != null)
                {
                    var (resolved, source) = mactowSvc.ResolveCurrentMacTow();
                    wbState.MactowPercent = resolved;
                    wbState.MacTowSource = source;
                    wbState.MacTowError = mactowSvc.IsOutOfRange(resolved);
                    mactowSvc.UpdateStaleness();
                }
                else
                {
                    wbState.MacTowError = false;
                }

                // Cargo holds.
                wbState.CargoFwdLoadedKg = ReadDouble(sdk, ProsimConstants.RefCargoForward);
                wbState.CargoFwdCapacityKg = ReadDouble(sdk, ProsimConstants.RefCargoForwardCapacity);
                wbState.CargoAftLoadedKg = ReadDouble(sdk, ProsimConstants.RefCargoRear);
                wbState.CargoAftCapacityKg = ReadDouble(sdk, ProsimConstants.RefCargoAftCapacity);
                wbState.CargoBulkCapacityKg = ReadDouble(sdk, ProsimConstants.RefCargoBulkCapacity);
                wbState.CargoPlannedKg = ReadDouble(sdk, ProsimConstants.RefEfbPlannedCargoKg);

                // Cargo doors. Bulk is read regardless of whether the airframe
                // has a bulk hold — the panel gates display on
                // CargoBulkCapacityKg > 0 so a non-bulk airframe shows N/A.
                wbState.FwdCargoDoorOpen = ReadBool(sdk, ProsimConstants.RefDoorCargoForward);
                wbState.AftCargoDoorOpen = ReadBool(sdk, ProsimConstants.RefDoorCargoAft);
                wbState.BulkCargoDoorOpen = ReadBool(sdk, ProsimConstants.RefDoorCargoBulk);

                // Entry / overwing doors L1..R4. L1/R1 = forward pax,
                // L2/R2 + L3/R3 = overwing emergency exits, L4/R4 = aft pax.
                // All eight render on the "Aircraft Status" silhouette so the
                // user can see boarding/catering door state at a glance.
                wbState.Door1LOpen = ReadBool(sdk, ProsimConstants.RefDoor1L);
                wbState.Door1ROpen = ReadBool(sdk, ProsimConstants.RefDoor1R);
                wbState.Door2LOpen = ReadBool(sdk, ProsimConstants.RefDoor2L);
                wbState.Door2ROpen = ReadBool(sdk, ProsimConstants.RefDoor2R);
                wbState.Door3LOpen = ReadBool(sdk, ProsimConstants.RefDoor3L);
                wbState.Door3ROpen = ReadBool(sdk, ProsimConstants.RefDoor3R);
                wbState.Door4LOpen = ReadBool(sdk, ProsimConstants.RefDoor4L);
                wbState.Door4ROpen = ReadBool(sdk, ProsimConstants.RefDoor4R);

                // Departure readiness — mirrors the GSX state machine's
                // Aircraft.HasOpenDoors predicate (covers entry + cargo +
                // bulk) so the banner reflects what would actually gate
                // CloseAllDoors() on final.
                wbState.AllDoorsClosed = !(_app?.GsxService?.AircraftInterface?.HasOpenDoors ?? false);

                // Passengers — capacities are static per aircraft, but reading
                // each tick is cheap (SDK-side cache) and adapts when the
                // aircraft is reloaded with a different config.
                wbState.Zone1Capacity = ReadInt(sdk, ProsimConstants.RefPaxZone1Capacity);
                wbState.Zone2Capacity = ReadInt(sdk, ProsimConstants.RefPaxZone2Capacity);
                wbState.Zone3Capacity = ReadInt(sdk, ProsimConstants.RefPaxZone3Capacity);
                wbState.Zone4Capacity = ReadInt(sdk, ProsimConstants.RefPaxZone4Capacity);

                // Seat occupation drives the SVG cabin layout. The "boarded"
                // count is the number of 'T' characters in that string —
                // matches how the EFB derives boarded count from the same
                // dataref.
                var seats = sdk.GetString(ProsimConstants.RefPaxCurrentString, "") ?? "";
                wbState.SeatOccupation = seats;
                wbState.PassengersBoarded = CountTrueChars(seats);

                // Planned pax count — efb.passengerStatistics is an Object
                // with a "totalNumOfPaxs" or similar field; the simpler
                // fallback is aircraft.passengers.booked.string (RefPaxBookedString),
                // which holds the planned occupation pattern. The 'T' count
                // there is the booked total.
                var booked = sdk.GetString(ProsimConstants.RefPaxBookedString, "") ?? "";
                wbState.PassengersPlanned = CountTrueChars(booked);

                // Planned fuel from the EFB INIT cache (CurrentOfp). The cache
                // is populated by EfbFlightPlanService — it normalises lbs →
                // kg at parse time, so we read a kg double directly. Null
                // when no OFP has been fetched yet.
                wbState.FuelPlannedKg = _app?.EfbFlightPlan?.CurrentOfp?.FuelRampKg ?? 0;
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

        // Subscribe + register-for-poll once per SDK connect. NoOpHandler is
        // a placeholder — Tick() polls via ReadDouble/ReadInt/ReadBool, so we
        // don't need event-driven callbacks; the Subscribe call is purely to
        // register the ref in the SDK's _subscriptions dict so its polling
        // loops refresh the cache.
        private void Prime(ProsimSdkInterface sdk)
        {
            if (_primed) return;
            foreach (var r in PolledRefs)
            {
                try { sdk.Subscribe(r, NoOpHandler); }
                catch (Exception ex) { Logger.LogException(ex); }
                sdk.RegisterPollDataref(r);
            }
            _primed = true;
        }

        private static void NoOpHandler(string name, dynamic newValue, dynamic oldValue) { }
    }
}
