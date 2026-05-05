using CFIT.AppLogger;
using Prosim2GSX.State;
using ProsimInterface;
using System;
using System.Text.Json;

namespace Prosim2GSX.Services
{
    // Reads the two EFB loadsheet datarefs (efb.prelimLoadsheet,
    // efb.finalLoadsheet) each StateUpdateWorker tick and projects the
    // parsed JSON into LoadsheetState. Polling rather than dataref-Subscribe
    // matches the project-wide convention (W&B, GSX, FlightStatus all poll
    // from the same worker tick).
    //
    // The datarefs hold System.String JSON blobs. The EFB Angular app sets
    // config.prelimLoadsheet / config.finalLoadsheet via JSON.parse(raw); the
    // sync-loadsheet button reads exactly two fields off the parsed object:
    //   - root.macTow      (number, % MAC — already display units)
    //   - root.tow.value   (number, kg)
    // Cargo is NOT in the loadsheet blob — the EFB sources cargo from
    // efb.plannedCargoKg (already surfaced via WeightBalanceState).
    //
    // LoadsheetIdent is sourced from the cached SimBrief OFP, not the
    // loadsheet blob. The EFB sets it at OFP-import time via
    // o.params.request_id.substring(0, 4). Same pattern WeightBalanceService
    // uses for FuelPlannedKg (reads LastSimbriefOfp).
    //
    // Auto-reset: a flight-cycle shutdown edge (on-ground + engines just
    // transitioned running → off) clears both slots back to Type="none",
    // Status="pending". Edge tracking is private to this service so we
    // don't have to add a new event to FlightStatusState.
    public class LoadsheetService
    {
        private readonly AppService _app;

        // Previous raw strings for change detection. A non-null transition
        // (null/empty → non-empty, or different string) triggers parse +
        // state update. Seeded null so the first non-empty read fires.
        private string _prevPrelimRaw;
        private string _prevFinalRaw;

        // Edge tracking for the shutdown-reset trigger. Mirrors the pattern
        // in StateUpdateWorker.UpdateChecklist (_wasOnGround/_wasEnginesRunning).
        private bool _wasOnGround = true;
        private bool _wasEnginesRunning;

        public LoadsheetService(AppService app)
        {
            _app = app;
        }

        public virtual void Tick()
        {
            try
            {
                var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
                if (sdk == null || !sdk.IsConnected) return;

                var ls = _app.Loadsheet;
                if (ls == null) return;

                ProcessShutdownReset(ls);

                var prelimRaw = sdk.GetString(ProsimConstants.RefEfbPrelimLoadsheet, "") ?? "";
                if (!string.Equals(prelimRaw, _prevPrelimRaw, StringComparison.Ordinal))
                {
                    _prevPrelimRaw = prelimRaw;
                    ApplyLoadsheet(ls, "prelim", prelimRaw);
                }

                var finalRaw = sdk.GetString(ProsimConstants.RefEfbFinalLoadsheet, "") ?? "";
                if (!string.Equals(finalRaw, _prevFinalRaw, StringComparison.Ordinal))
                {
                    _prevFinalRaw = finalRaw;
                    ApplyLoadsheet(ls, "final", finalRaw);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Resets both slots when the aircraft transitions from
        // running-engines-on-ground to engines-off-on-ground (i.e. shutdown
        // after a flight). Same edge trigger Checklists uses for ResetAll.
        protected virtual void ProcessShutdownReset(LoadsheetState ls)
        {
            var fs = _app?.FlightStatus;
            if (fs == null) return;

            bool nowOnGround = fs.AppOnGround;
            bool nowEnginesRunning = fs.AppEnginesRunning;

            if (nowOnGround && _wasEnginesRunning && !nowEnginesRunning)
            {
                ResetSlots(ls);
                _prevPrelimRaw = null;
                _prevFinalRaw = null;
                Logger.Information("Loadsheet slots reset on flight-cycle shutdown");
            }

            _wasOnGround = nowOnGround;
            _wasEnginesRunning = nowEnginesRunning;
        }

        // Parses the JSON blob and writes the projected fields into the
        // matching slot. Empty/whitespace raw → reset that slot only. Parse
        // failure → Status="error", RawJson preserved for diagnosis.
        protected virtual void ApplyLoadsheet(LoadsheetState ls, string type, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                ResetSlot(ls, type);
                return;
            }

            double macTow = 0.0;
            double towKg = 0.0;
            bool parseOk = false;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (root.TryGetProperty("macTow", out var mtEl) && mtEl.ValueKind == JsonValueKind.Number)
                    macTow = mtEl.GetDouble();

                if (root.TryGetProperty("tow", out var towEl)
                    && towEl.ValueKind == JsonValueKind.Object
                    && towEl.TryGetProperty("value", out var towVal)
                    && towVal.ValueKind == JsonValueKind.Number)
                {
                    towKg = towVal.GetDouble();
                }

                parseOk = true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to parse {type} loadsheet JSON: {ex.Message}");
            }

            string ident = ResolveLoadsheetIdent();
            bool macError = parseOk && (macTow < ls.MinMacTow || macTow > ls.MaxMacTow);
            string status = parseOk ? "received" : "error";

            if (type == "prelim")
            {
                ls.PrelimType = "prelim";
                ls.PrelimStatus = status;
                ls.PrelimMacTow = macTow;
                ls.PrelimMacTowError = macError;
                ls.PrelimTowKg = towKg;
                ls.PrelimLoadsheetIdent = ident;
                ls.PrelimRawJson = raw;
                ls.PrelimReceivedAt = DateTime.UtcNow;
            }
            else
            {
                ls.FinalType = "final";
                ls.FinalStatus = status;
                ls.FinalMacTow = macTow;
                ls.FinalMacTowError = macError;
                ls.FinalTowKg = towKg;
                ls.FinalLoadsheetIdent = ident;
                ls.FinalRawJson = raw;
                ls.FinalReceivedAt = DateTime.UtcNow;
            }
        }

        // First 4 chars of the cached SimBrief request_id. Empty when no
        // OFP has been imported yet. The EFB does the exact same projection
        // at OFP-import time (config.flight.loadsheetIdent = o.params.request_id.substring(0, 4)).
        protected virtual string ResolveLoadsheetIdent()
        {
            try
            {
                var ofp = _app?.GsxService?.AircraftInterface?.LastSimbriefOfp;
                var rid = ofp?.Params?.RequestId;
                if (string.IsNullOrEmpty(rid)) return "";
                return rid.Length <= 4 ? rid : rid.Substring(0, 4);
            }
            catch
            {
                return "";
            }
        }

        public virtual void ResetSlots(LoadsheetState ls)
        {
            if (ls == null) return;
            ResetSlot(ls, "prelim");
            ResetSlot(ls, "final");
        }

        protected virtual void ResetSlot(LoadsheetState ls, string type)
        {
            if (type == "prelim")
            {
                ls.PrelimType = "none";
                ls.PrelimStatus = "pending";
                ls.PrelimMacTow = 0.0;
                ls.PrelimMacTowError = false;
                ls.PrelimTowKg = 0.0;
                ls.PrelimLoadsheetIdent = "";
                ls.PrelimRawJson = "";
                ls.PrelimReceivedAt = null;
            }
            else if (type == "final")
            {
                ls.FinalType = "none";
                ls.FinalStatus = "pending";
                ls.FinalMacTow = 0.0;
                ls.FinalMacTowError = false;
                ls.FinalTowKg = 0.0;
                ls.FinalLoadsheetIdent = "";
                ls.FinalRawJson = "";
                ls.FinalReceivedAt = null;
            }
        }
    }
}
