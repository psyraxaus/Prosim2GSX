using CFIT.AppLogger;
using Prosim2GSX.State;
using ProsimInterface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    // Owns the EFB Flight Planning (INIT) tab workflow.
    //
    //   1. Manual fetch (REST POST /api/efb/fetch-ofp + WPF FETCH OFP button)
    //      calls the SDK's SimbriefService.FetchAndImportSimbriefOfp, then
    //      projects the parsed response into OFPData. Source = Manual.
    //
    //   2. MCDU-triggered observation (Tick from StateUpdateWorker) watches
    //      AircraftInterface.LastSimbriefOfp for a new RequestId. The SDK
    //      auto-fetches when MCDU dep/arr is entered (its own MCDU watcher);
    //      we just pick up the result. Source = Mcdu.
    //
    //   3. Overrides — SetOverride/ClearOverride/ClearAllOverrides mutate
    //      the override dicts AND push the change to ProSim:
    //        - zfwKg          → aircraft.fms.init.zfw
    //        - fuelRampKg     → aircraft.fms.init.block (rounded up to 100 kg)
    //        - cargoKg        → efb.plannedCargoKg
    //        - passengerCount → efb.passengers.booked.string (seat map)
    //      Override commits always write (explicit pilot intent). Clearing
    //      an override re-writes the OFP value. Other override fields stay
    //      display-only — there's no FMS init dataref for trip / reserve /
    //      contingency / extra fuel.
    //
    //   4. Auto-sync to FMS init on fetch is gated by Config.EfbAutoSyncToFmsOnFetch
    //      (default off — pilot retains positive control via the manual SYNC
    //      TO FMS button). The SDK's import already wrote the EFB-side fields
    //      (planned pax/cargo/fuel); this gate covers the FMS-init writes
    //      (zfw, block) that the SDK doesn't touch.
    //
    // RequestId-based change detection means both fetch paths converge on
    // ApplyOfp. After a manual fetch our cache's OfpId equals what the SDK
    // has, so the next Tick is a no-op.
    public class EfbFlightPlanService
    {
        private readonly AppService _app;

        // Override field names that map to a writable ProSim dataref. Other
        // override fields (e.g. fuelTripKg, fuelContingencyKg) are display-only
        // — they ride in the override map for UI, but there's no FMS init
        // dataref to push them to.
        private static readonly HashSet<string> WritableOverrideFields =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "zfwKg",
                "fuelRampKg",
                "cargoKg",
                "passengerCount",
            };

        public EfbFlightPlanService(AppService app)
        {
            _app = app;
        }

        // Polled from StateUpdateWorker on the background tick. Cheap when
        // there is no change. Skipped while a manual fetch is in flight so
        // the two paths can't race on the same LastResponse.
        public virtual void Tick()
        {
            try
            {
                var state = _app?.EfbFlightPlan;
                if (state == null) return;
                if (state.IsBusy) return;

                var ai = _app?.GsxService?.AircraftInterface;
                var ofp = ai?.LastSimbriefOfp;
                if (ofp == null) return;

                var newId = ofp.Params?.RequestId ?? "";
                var currentId = state.CurrentOfp?.OfpId ?? "";
                if (string.IsNullOrEmpty(newId) ||
                    string.Equals(newId, currentId, StringComparison.Ordinal))
                    return;

                ApplyOfp(ofp, OfpSource.Mcdu);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Manual fetch entry point used by the FETCH OFP button. Source is
        // an arg so the same path serves the manual REST call and any
        // future programmatic re-fetch.
        public virtual async Task<bool> FetchAsync(OfpSource source, CancellationToken ct)
        {
            var state = _app?.EfbFlightPlan;
            if (state == null) return false;

            var ai = _app?.GsxService?.AircraftInterface;
            var simbrief = ai?.ProsimInterface?.SimbriefService;
            var user = ai?.SimbriefUser ?? "";

            if (simbrief == null)
            {
                state.LastFetchError = "SimBrief service not available — ProSim SDK not connected.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(user))
            {
                state.LastFetchError = "SimBrief user not configured — set the user ID on ProSim's EFB.";
                return false;
            }

            state.IsBusy = true;
            state.LastFetchError = "";

            try
            {
                var (success, _) = await simbrief.FetchAndImportSimbriefOfp(user);
                if (!success)
                {
                    state.LastFetchError = "SimBrief fetch failed — check network and user ID.";
                    return false;
                }

                var ofp = simbrief.LastResponse;
                if (ofp == null)
                {
                    state.LastFetchError = "SimBrief returned no data.";
                    return false;
                }

                ApplyOfp(ofp, source);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                state.LastFetchError = $"Fetch error: {ex.Message}";
                return false;
            }
            finally
            {
                state.IsBusy = false;
            }
        }

        public virtual void SetOverride(string field, object value)
        {
            if (string.IsNullOrWhiteSpace(field)) return;
            var state = _app?.EfbFlightPlan;
            if (state == null) return;

            var flags = new Dictionary<string, bool>(state.OverrideFlags) { [field] = true };
            var values = new Dictionary<string, object>(state.OverrideValues) { [field] = value };
            state.OverrideFlags = flags;
            state.OverrideValues = values;

            // Override commits always write — they are explicit pilot intent
            // and the user expects the FMS / EFB to reflect the override
            // immediately, regardless of the auto-sync flag (which only
            // gates the initial fetch-time push).
            WriteOverrideToProSim(field, value);
        }

        public virtual void ClearOverride(string field)
        {
            if (string.IsNullOrWhiteSpace(field)) return;
            var state = _app?.EfbFlightPlan;
            if (state == null) return;

            if (!state.OverrideFlags.ContainsKey(field) && !state.OverrideValues.ContainsKey(field))
                return;

            var flags = new Dictionary<string, bool>(state.OverrideFlags);
            var values = new Dictionary<string, object>(state.OverrideValues);
            flags.Remove(field);
            values.Remove(field);
            state.OverrideFlags = flags;
            state.OverrideValues = values;

            // Reverting an override pushes the OFP value back to ProSim so
            // the FMS / EFB stops showing the overridden number.
            WriteOfpValueForField(field, state.CurrentOfp);
        }

        public virtual void ClearAllOverrides()
        {
            var state = _app?.EfbFlightPlan;
            if (state == null) return;

            if (state.OverrideFlags.Count == 0 && state.OverrideValues.Count == 0) return;

            // Snapshot the previously-overridden writable fields BEFORE we
            // clear, so we can restore each one to its OFP value.
            var fieldsToRevert = new List<string>();
            foreach (var f in state.OverrideFlags.Keys)
            {
                if (WritableOverrideFields.Contains(f))
                    fieldsToRevert.Add(f);
            }

            state.OverrideFlags = new Dictionary<string, bool>();
            state.OverrideValues = new Dictionary<string, object>();

            foreach (var f in fieldsToRevert)
                WriteOfpValueForField(f, state.CurrentOfp);
        }

        public virtual void ResetFlight()
        {
            var state = _app?.EfbFlightPlan;
            if (state == null) return;

            state.CurrentOfp = null;
            state.OverrideFlags = new Dictionary<string, bool>();
            state.OverrideValues = new Dictionary<string, object>();
            state.Status = OfpStatus.Empty;
            state.Source = OfpSource.None;
            state.FetchedAt = null;
            state.LastFetchError = "";
            state.IsBusy = false;
        }

        protected virtual void ApplyOfp(SimbriefResponse ofp, OfpSource source)
        {
            var state = _app?.EfbFlightPlan;
            if (state == null) return;

            double conv = _app?.Config?.WeightConversion ?? 2.2046226218;
            var data = SimbriefOfpMapper.ToOFPData(ofp, conv);
            if (data == null) return;

            state.CurrentOfp = data;
            state.FetchedAt = data.FetchedAt;
            state.Source = source;
            state.Status = OfpStatus.Loaded;
            state.LastFetchError = "";

            // Optional auto-push to FMS init the moment the OFP loads. Off by
            // default — pilot opts in via Config.EfbAutoSyncToFmsOnFetch. The
            // SDK's import (called by FetchAsync) already wrote the EFB-side
            // values (pax/cargo/planned-fuel); this is the FMS-side push that
            // the SDK doesn't do.
            if (_app?.Config?.EfbAutoSyncToFmsOnFetch == true)
                WriteFmsInitFromOfp(data);
        }

        // ── ProSim writes ───────────────────────────────────────────────────

        // Writes ZFW + block fuel to the FMS init datarefs from the supplied
        // OFP. ZFW CG is intentionally skipped — the OFP doesn't carry it,
        // and MactowValidationService writes it on final-loadsheet receipt
        // from the W&B-computed value. Cargo + pax + planned fuel are
        // already written by the SDK's import path.
        protected virtual void WriteFmsInitFromOfp(OFPData ofp)
        {
            if (ofp == null) return;
            var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
            if (sdk == null || !sdk.IsConnected) return;

            try
            {
                sdk.SetDouble(ProsimConstants.RefFmsInitZfw, ofp.ZfwKg);
                var block = MactowValidationService.RoundBlockFuelKg(ofp.FuelRampKg);
                sdk.SetDouble(ProsimConstants.RefFmsInitBlock, block);
                Logger.Information(
                    $"EFB INIT auto-sync: zfw={ofp.ZfwKg:F0} blockFuel={block:F0}");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Writes a single override value to its mapped ProSim dataref.
        // Unmapped fields are display-only — they remain in the override map
        // but no dataref is touched.
        protected virtual void WriteOverrideToProSim(string field, object value)
        {
            if (!WritableOverrideFields.Contains(field)) return;
            var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
            if (sdk == null || !sdk.IsConnected) return;

            try
            {
                switch (field)
                {
                    case "zfwKg":
                        if (TryToDouble(value, out var zfw))
                            sdk.SetDouble(ProsimConstants.RefFmsInitZfw, zfw);
                        break;
                    case "fuelRampKg":
                        if (TryToDouble(value, out var ramp))
                            sdk.SetDouble(ProsimConstants.RefFmsInitBlock,
                                MactowValidationService.RoundBlockFuelKg(ramp));
                        break;
                    case "cargoKg":
                        if (TryToDouble(value, out var cargo))
                            sdk.SetDouble(ProsimConstants.RefEfbPlannedCargoKg, cargo);
                        break;
                    case "passengerCount":
                        if (TryToInt(value, out var pax))
                            WritePaxToProSim(sdk, pax);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Writes the OFP's value for a given field — used by ClearOverride
        // to revert a single field to its SimBrief number.
        protected virtual void WriteOfpValueForField(string field, OFPData ofp)
        {
            if (ofp == null || !WritableOverrideFields.Contains(field)) return;
            switch (field)
            {
                case "zfwKg": WriteOverrideToProSim(field, ofp.ZfwKg); break;
                case "fuelRampKg": WriteOverrideToProSim(field, ofp.FuelRampKg); break;
                case "cargoKg": WriteOverrideToProSim(field, ofp.CargoKg); break;
                case "passengerCount": WriteOverrideToProSim(field, ofp.PassengerCount); break;
            }
        }

        // Builds a comma-separated true/false seat-occupation string of the
        // right length and writes it to RefEfbPaxBookedString. Mirrors the
        // SDK's import-time pattern (SimbriefService:135). Capacity is
        // sourced from WeightBalanceState (already populated each tick) so
        // we don't re-read the four zone-capacity datarefs here.
        protected virtual void WritePaxToProSim(ProsimSdkInterface sdk, int paxCount)
        {
            var wb = _app?.WeightBalance;
            int capacity = (wb?.Zone1Capacity ?? 0) + (wb?.Zone2Capacity ?? 0)
                         + (wb?.Zone3Capacity ?? 0) + (wb?.Zone4Capacity ?? 0);
            if (capacity <= 0) capacity = 132; // A320 default zone sum
            if (paxCount < 0) paxCount = 0;
            if (paxCount > capacity) paxCount = capacity;

            bool[] map = new bool[capacity];
            for (int i = 0; i < paxCount; i++) map[i] = true;

            sdk.SetString(ProsimConstants.RefEfbPaxBookedString, ProsimSeatMap.SeatString(map));
        }

        // Coerce a System.Text.Json-deserialized override value (which arrives
        // as JsonElement when the destination is `object`) into a primitive.
        private static bool TryToDouble(object v, out double result)
        {
            result = 0;
            switch (v)
            {
                case null: return false;
                case double d: result = d; return true;
                case float f: result = f; return true;
                case int i: result = i; return true;
                case long l: result = l; return true;
                case decimal m: result = (double)m; return true;
                case string s:
                    return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                case JsonElement je:
                    if (je.ValueKind == JsonValueKind.Number) { result = je.GetDouble(); return true; }
                    if (je.ValueKind == JsonValueKind.String)
                        return double.TryParse(je.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                    return false;
                default:
                    try { result = Convert.ToDouble(v, CultureInfo.InvariantCulture); return true; }
                    catch { return false; }
            }
        }

        private static bool TryToInt(object v, out int result)
        {
            if (TryToDouble(v, out var d)) { result = (int)Math.Round(d); return true; }
            result = 0;
            return false;
        }
    }
}
