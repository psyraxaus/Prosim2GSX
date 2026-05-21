using CFIT.AppLogger;
using Prosim2GSX.State;
using ProsimInterface;
using System;
using System.Globalization;

namespace Prosim2GSX.Services
{
    // Reads ProSim fuel datarefs each StateUpdateWorker tick and writes the
    // values into FuelState. Polling rather than dataref-Subscribe matches
    // the project-wide convention (every other state mirror — GsxState,
    // FlightStatusState, WeightBalanceState, LoadsheetState — is populated
    // by polling helpers from the same worker), and the [ObservableProperty]
    // setters give equality-based compare-and-skip so WS broadcasts only
    // fire on actual change. No explicit throttle layer — at the worker's
    // 500ms cadence and the slow rate of fuel transfer / refuel events, the
    // natural debounce is sufficient.
    //
    // Planned fuel is sourced from the EFB INIT cache (LastSimbriefOfp →
    // OFPData.FuelRampKg, kg-normalised at parse time by EfbFlightPlanService).
    // The "fuel.plan_ramp" dataref does NOT exist in ProsimDataref.csv —
    // confirmed by Phase 1 archaeology. Same source WeightBalanceService
    // uses for its own FuelPlannedKg field.
    public class FuelService
    {
        private readonly AppService _app;

        // First-tick priming guard. Subscribe + RegisterPollDataref must run
        // once per SDK connect so the polling loop refreshes the cache —
        // without this, ReadDataRef returns the snapshot taken on first read
        // and never updates, so PropertyChanged never fires and no WS patches
        // ship. Reset on disconnect so reconnects re-prime.
        private bool _primed;

        // Every dataref this service consumes. Prime() Subscribes each one
        // (so the SDK polling loops include it) and RegisterPollDatareffs
        // them (so the user-poll loop covers any that aren't in a tier
        // HashSet — idempotent in either direction).
        private static readonly string[] PolledRefs = new[]
        {
            ProsimConstants.RefFuelTotal,
            ProsimConstants.RefFuelTotalCapacity,
            ProsimConstants.RefFuelCenter,
            ProsimConstants.RefFuelLeft,
            ProsimConstants.RefFuelRight,
            ProsimConstants.RefFuelCenterCapacity,
            ProsimConstants.RefFuelLeftCapacity,
            ProsimConstants.RefFuelRightCapacity,
            ProsimConstants.RefFuelLeftOuter,
            ProsimConstants.RefFuelLeftInner,
            ProsimConstants.RefFuelRightInner,
            ProsimConstants.RefFuelRightOuter,
            ProsimConstants.RefFuelLeftOuterCapacity,
            ProsimConstants.RefFuelLeftInnerCapacity,
            ProsimConstants.RefFuelRightInnerCapacity,
            ProsimConstants.RefFuelRightOuterCapacity,
        };

        public FuelService(AppService app)
        {
            _app = app;
        }

        // Called from StateUpdateWorker.OnTick. Null-safe across degraded
        // modes (no SDK, no GsxService, no aircraft loaded) — leaves the
        // store at whatever it last held instead of forcing zeros that
        // would jitter the panel on transient disconnect.
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

                var fuel = _app.Fuel;
                if (fuel == null) return;

                // Total in tanks + total capacity — RefFuelTotal is the
                // kg-locked alias of aircraft.fuel.total.amount (matching
                // WeightBalanceService's FuelInTanksKg path).
                fuel.FuelInTanksKg = ReadDouble(sdk, ProsimConstants.RefFuelTotal);
                fuel.FuelCapacityKg = ReadDouble(sdk, ProsimConstants.RefFuelTotalCapacity);

                // Per-tank amounts + capacities. The capacity refs were
                // added to ProsimConstants for this feature; the amount
                // refs already existed and are also used by the refuel
                // automation path.
                fuel.FuelCentreKg = ReadDouble(sdk, ProsimConstants.RefFuelCenter);
                fuel.FuelLeftKg = ReadDouble(sdk, ProsimConstants.RefFuelLeft);
                fuel.FuelRightKg = ReadDouble(sdk, ProsimConstants.RefFuelRight);
                fuel.FuelCentreCapacityKg = ReadDouble(sdk, ProsimConstants.RefFuelCenterCapacity);
                fuel.FuelLeftCapacityKg = ReadDouble(sdk, ProsimConstants.RefFuelLeftCapacity);
                fuel.FuelRightCapacityKg = ReadDouble(sdk, ProsimConstants.RefFuelRightCapacity);

                // 5-tank A320 breakdown — inner/outer for each wing. The
                // wing-aggregate refs above are inner+outer combined; these
                // expose the individual tanks ProSim's FUEL EFB page shows.
                fuel.FuelLeftOuterKg = ReadDouble(sdk, ProsimConstants.RefFuelLeftOuter);
                fuel.FuelLeftInnerKg = ReadDouble(sdk, ProsimConstants.RefFuelLeftInner);
                fuel.FuelRightInnerKg = ReadDouble(sdk, ProsimConstants.RefFuelRightInner);
                fuel.FuelRightOuterKg = ReadDouble(sdk, ProsimConstants.RefFuelRightOuter);
                fuel.FuelLeftOuterCapacityKg = ReadDouble(sdk, ProsimConstants.RefFuelLeftOuterCapacity);
                fuel.FuelLeftInnerCapacityKg = ReadDouble(sdk, ProsimConstants.RefFuelLeftInnerCapacity);
                fuel.FuelRightInnerCapacityKg = ReadDouble(sdk, ProsimConstants.RefFuelRightInnerCapacity);
                fuel.FuelRightOuterCapacityKg = ReadDouble(sdk, ProsimConstants.RefFuelRightOuterCapacity);

                // Planned from the EFB INIT cache. Zero when no OFP loaded.
                double planned = _app?.EfbFlightPlan?.CurrentOfp?.FuelRampKg ?? 0;
                fuel.PlannedRampKg = planned;

                // Derived figures. Delta is in-tanks minus planned, so
                // positive = over-fuelled, negative = under-fuelled. Flags
                // are SUPPRESSED when planned <= 0 — without this, a
                // freshly-loaded aircraft with no OFP would always read as
                // "OVER-FUELLED" simply because tanks > 0 and planned = 0.
                double delta = fuel.FuelInTanksKg - planned;
                fuel.FuelDeltaKg = delta;
                if (planned > 0)
                {
                    fuel.IsOverFuelled = delta > 0;
                    fuel.IsUnderFuelled = delta < -FuelState.UnderFuelThresholdKg;
                }
                else
                {
                    fuel.IsOverFuelled = false;
                    fuel.IsUnderFuelled = false;
                }

                // Litre conversion via the airframe-fixed SG. Computed
                // server-side so the wire shape carries both unit families
                // and the React/WPF panels never need to convert.
                fuel.PlannedRampLitres = planned / FuelState.SpecificGravity;
                fuel.FuelInTanksLitres = fuel.FuelInTanksKg / FuelState.SpecificGravity;
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

        // Subscribe + register-for-poll once per SDK connect. NoOpHandler is
        // a placeholder — Tick() polls via ReadDouble, so we don't need
        // event-driven callbacks; the Subscribe call is purely to register
        // the ref in the SDK's _subscriptions dict so its polling loops
        // refresh the cache.
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
            // Diagnostic: confirms the service reached its populate path after
            // an SDK connect (notably under DelayProsimConnection, where the
            // SDK comes up ~10–15 s into the session). If this line is absent
            // from a delay-mode log yet the Fuel tab still shows defaults, the
            // gap is upstream of the store, not in the WS layer.
            Logger.Debug($"FuelService primed {PolledRefs.Length} datarefs — SDK connected, populating FuelState from this tick on");
        }

        private static void NoOpHandler(string name, dynamic newValue, dynamic oldValue) { }
    }
}
