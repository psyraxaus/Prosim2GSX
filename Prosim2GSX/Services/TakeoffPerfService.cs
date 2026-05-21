using CFIT.AppLogger;
using Prosim2GSX.State;
using ProsimInterface;
using ProsimInterface.Performance;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    // Orchestrates the TAKEOFF perf tab. Proxies the ProSim gateway's
    // /efb/airport/* + /efb/calculate/vspeeds endpoints onto TakeoffPerfState
    // and owns the FMS uplink action that writes V1/VR/V2 + FLAPS + FLEX +
    // THS + SHIFT into aircraft.fms.perf.takeOff.*.
    //
    // Tick() is called from StateUpdateWorker each timer beat. Heavy work —
    // runway loads, METAR fetches, /calculate calls, FMS writes — happens
    // off-tick from REST handlers via the async methods below. The tick
    // path only does the cheap once-per-connect priming, the ICAO prefill
    // from aircraft.fms.origin, the engine-variant resolution, and the
    // shutdown-reset edge.
    public class TakeoffPerfService
    {
        private readonly AppService _app;

        // Edge tracking for the shutdown-reset trigger. Mirrors the pattern
        // in LoadsheetService.ProcessShutdownReset.
        private bool _wasOnGround = true;
        private bool _wasEnginesRunning;

        // First-tick priming guard — registers the perf-adjacent datarefs
        // (origin, destination, EPR, shift-unit) with the SDK's user-poll
        // loop so GetString reads land warm values. Reset on disconnect.
        private bool _primed;

        // One-shot guard for the tick-driven runway auto-load. Holds the
        // ICAO we last kicked an auto-load for so we don't re-fire every
        // tick (Tick runs each StateUpdateWorker beat). Cleared on reset so
        // a re-prefill to the same airport reloads; cleared on a failed
        // load so a later tick can retry once the EFB gateway is ready.
        private string _autoLoadedRunwaysForIcao = "";

        private static readonly string[] PolledRefs = new[]
        {
            ProsimConstants.RefFmsOrigin,
            ProsimConstants.RefConfigEngineType,
            ProsimConstants.RefUnitTakeoffShift,
        };

        public TakeoffPerfService(AppService app)
        {
            _app = app;
        }

        // -----------------------------------------------------------------
        //  Tick — cheap per-tick housekeeping only
        // -----------------------------------------------------------------

        public virtual void Tick()
        {
            try
            {
                var st = _app?.TakeoffPerf;
                if (st == null) return;

                var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
                if (sdk == null || !sdk.IsConnected)
                {
                    _primed = false;
                    return;
                }

                Prime(sdk);
                ProcessShutdownReset(st);

                // Pre-fill Icao from aircraft.fms.origin while the field is
                // empty (post-reset or first launch). Idempotent — once the
                // user has typed something, leaves it alone.
                if (string.IsNullOrWhiteSpace(st.Icao))
                {
                    var origin = sdk.GetString(ProsimConstants.RefFmsOrigin, "") ?? "";
                    if (origin.Length == 4 && !string.Equals(origin, "Null", StringComparison.OrdinalIgnoreCase))
                        st.Icao = origin.ToUpperInvariant();
                }

                // Engine-variant resolution — display-only field, refreshed
                // each tick. system.config.Config.EPR ⇒ "CFM" | "IAE" |
                // "CFM-Leap". The gateway repository only knows CFM/IAE, so
                // Leap collapses to CFM. Empty or unknown falls back to CFM
                // with a one-shot warning (D7).
                st.EngineVariant = ResolveEngineVariant(sdk);

                // Once an ICAO is present (prefilled above or set by the
                // user) but no runways have been loaded for it yet, kick a
                // one-shot runway load so the dropdown is populated without
                // the user having to re-commit the ICAO field.
                MaybeAutoLoadRunways(st);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private void Prime(ProsimSdkInterface sdk)
        {
            if (_primed) return;
            foreach (var r in PolledRefs)
            {
                try { sdk.Subscribe(r, NoOpHandler); } catch (Exception ex) { Logger.LogException(ex); }
                sdk.RegisterPollDataref(r);
            }
            _primed = true;
            Logger.Debug($"TakeoffPerfService primed {PolledRefs.Length} datarefs");
        }

        private static void NoOpHandler(string name, dynamic newValue, dynamic oldValue) { }

        // True one-shot warning the first time we have to fall back to "CFM"
        // because the dataref read came back unrecognised. Quiets the log on
        // subsequent ticks where the same fallback applies.
        private bool _engineVariantFallbackLogged;

        private string ResolveEngineVariant(ProsimSdkInterface sdk)
        {
            var raw = sdk.GetString(ProsimConstants.RefConfigEngineType, "") ?? "";
            if (string.Equals(raw, "IAE", StringComparison.OrdinalIgnoreCase)) return "IAE";
            if (string.Equals(raw, "CFM", StringComparison.OrdinalIgnoreCase)) return "CFM";
            if (string.Equals(raw, "CFM-Leap", StringComparison.OrdinalIgnoreCase)) return "CFM";
            if (!_engineVariantFallbackLogged)
            {
                Logger.Warning($"TakeoffPerfService: unknown engine type '{raw}' — defaulting EngineVariant to CFM");
                _engineVariantFallbackLogged = true;
            }
            return "CFM";
        }

        protected virtual void ProcessShutdownReset(TakeoffPerfState st)
        {
            var fs = _app?.FlightStatus;
            if (fs == null) return;

            bool nowOnGround = fs.AppOnGround;
            bool nowEnginesRunning = fs.AppEnginesRunning;

            if (nowOnGround && _wasEnginesRunning && !nowEnginesRunning)
            {
                st.Reset();
                _autoLoadedRunwaysForIcao = "";
                Logger.Information("TakeoffPerfState reset on flight-cycle shutdown");
            }

            _wasOnGround = nowOnGround;
            _wasEnginesRunning = nowEnginesRunning;
        }

        // Fire-and-forget runway load triggered from the tick path. The
        // per-ICAO guard is set synchronously before launching so a second
        // tick can't double-fire while the load is in flight; it's released
        // on failure so a later tick retries once the EFB gateway is up.
        private void MaybeAutoLoadRunways(TakeoffPerfState st)
        {
            if (st.IsBusy) return;
            var icao = st.Icao;
            if (string.IsNullOrWhiteSpace(icao) || icao.Length != 4) return;
            if (st.Runways.Count > 0) return;
            if (string.Equals(_autoLoadedRunwaysForIcao, icao, StringComparison.OrdinalIgnoreCase)) return;

            _autoLoadedRunwaysForIcao = icao;
            _ = Task.Run(async () =>
            {
                try
                {
                    bool ok = await LoadRunwaysAsync(icao);
                    if (!ok) _autoLoadedRunwaysForIcao = "";
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    _autoLoadedRunwaysForIcao = "";
                }
            });
        }

        // -----------------------------------------------------------------
        //  REST-driven actions
        // -----------------------------------------------------------------

        public virtual async Task<bool> LoadRunwaysAsync(string icao, CancellationToken ct = default)
        {
            var st = _app?.TakeoffPerf;
            if (st == null) return false;

            if (string.IsNullOrWhiteSpace(icao) || icao.Length != 4)
            {
                st.LastError = "ICAO must be 4 characters";
                st.Runways = new List<RunwayOption>();
                return false;
            }

            try
            {
                st.IsBusy = true;
                st.LastError = "";
                st.Icao = icao.ToUpperInvariant();

                var efb = _app?.GsxService?.AircraftInterface?.ProsimInterface?.Efb;
                if (efb == null)
                {
                    st.LastError = "ProSim EFB unavailable";
                    return false;
                }

                var runways = await efb.GetRunways(st.Icao, includeIntersections: true);
                var options = PerfProjection.ProjectRunways(runways);
                st.Runways = options;
                if (options.Count == 0)
                {
                    st.LastError = $"No runways found for {st.Icao}";
                    st.RunwayId = "";
                }
                else if (string.IsNullOrEmpty(st.RunwayId) || options.All(o => o.RunwayId != st.RunwayId))
                {
                    st.RunwayId = options[0].RunwayId;
                    st.IntersectionName = "";
                }

                // Loading runways implicitly refreshes weather too.
                await LoadMetarAsync(st.Icao, ct);
                return options.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                st.LastError = ex.Message;
                return false;
            }
            finally
            {
                st.IsBusy = false;
            }
        }

        public virtual async Task<bool> LoadMetarAsync(string icao, CancellationToken ct = default)
        {
            var st = _app?.TakeoffPerf;
            if (st == null) return false;

            var efb = _app?.GsxService?.AircraftInterface?.ProsimInterface?.Efb;
            if (efb == null) return false;

            try
            {
                var metar = await efb.GetMetar(icao);
                if (metar == null)
                {
                    st.MetarText = "";
                    st.MetarFetchedAt = DateTime.UtcNow;
                    return false;
                }
                st.MetarText = metar.MetarText ?? "";
                st.MetarFetchedAt = DateTime.UtcNow;

                // Auto-fill wind / OAT / QNH from the METAR — matches the
                // EFB's "Apply weather" behaviour. WindDir is "VRB" or a
                // 3-char numeric. WindSpeed is a string; parse defensively.
                if (!string.IsNullOrWhiteSpace(metar.WindDir))
                    st.WindDir = metar.WindDir;
                if (!string.IsNullOrWhiteSpace(metar.WindSpeed)
                    && int.TryParse(metar.WindSpeed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ws))
                    st.WindKt = ws;
                st.OatC = metar.Temperature;
                if (metar.Altim > 0)
                    st.QnhHpa = metar.Altim;
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        // Pulls TOW + MAC from the active loadsheet slot (final preferred,
        // prelim if no final yet). Per the loadsheet-authority memory: the
        // LoadsheetState is the truth for "final received" — not the SDK's
        // WasFinalTransmitted.
        public virtual void SyncFromLoadsheet()
        {
            var st = _app?.TakeoffPerf;
            var ls = _app?.Loadsheet;
            if (st == null || ls == null) return;

            if (string.Equals(ls.FinalType, "final", StringComparison.OrdinalIgnoreCase)
                && string.Equals(ls.FinalStatus, "received", StringComparison.OrdinalIgnoreCase))
            {
                st.TowKg = ls.FinalTowKg;
                st.MactowPercent = ls.FinalMacTow;
            }
            else if (string.Equals(ls.PrelimType, "prelim", StringComparison.OrdinalIgnoreCase)
                  && string.Equals(ls.PrelimStatus, "received", StringComparison.OrdinalIgnoreCase))
            {
                st.TowKg = ls.PrelimTowKg;
                st.MactowPercent = ls.PrelimMacTow;
            }
        }

        public virtual async Task<bool> CalculateAsync(CancellationToken ct = default)
        {
            var st = _app?.TakeoffPerf;
            if (st == null) return false;

            if (string.IsNullOrWhiteSpace(st.RunwayId))
            {
                st.LastError = "Select a runway before calculating";
                return false;
            }
            if (st.TowKg <= 0)
            {
                st.LastError = "TOW must be set (sync from loadsheet or enter manually)";
                return false;
            }

            var runway = st.Runways.FirstOrDefault(r => string.Equals(r.RunwayId, st.RunwayId, StringComparison.OrdinalIgnoreCase));
            if (runway == null)
            {
                st.LastError = "Selected runway is not in the loaded runway list";
                return false;
            }

            int selectedToraFt = runway.LengthFt;
            if (!string.IsNullOrEmpty(st.IntersectionName))
            {
                var inter = runway.Intersections.FirstOrDefault(i => string.Equals(i.Name, st.IntersectionName, StringComparison.OrdinalIgnoreCase));
                if (inter != null) selectedToraFt = inter.ToraFt;
            }

            int rwyLenM = Math.Max(0, (int)Math.Floor(selectedToraFt / 3.28084));
            if (rwyLenM > 3600) rwyLenM = 3600;

            int rwyQdm = PerfProjection.ParseRunwayQdm(runway.RunwayId);
            int towTensKg = (int)Math.Round(st.TowKg / 100.0);     // wire scale: 70.5 t → 705
            int mactowX10 = (int)Math.Round(st.MactowPercent * 10);

            var req = new CalcVSpeedRequest
            {
                AircraftType    = "A320",
                EngineVariant   = st.EngineVariant,
                Tow             = towTensKg,
                TowUnit         = "KG",
                Mactow          = mactowX10,
                RwyLen          = rwyLenM,
                RwyQDM          = rwyQdm,
                FlapVal         = (st.Flap ?? "opt").ToLowerInvariant(),
                Surf            = (st.Surface ?? "DRY").ToUpperInvariant(),
                IceVal          = (st.AntiIce ?? "OFF").ToUpperInvariant(),
                PacksVal        = (st.Packs ?? "ON").ToUpperInvariant(),
                TogaVal         = st.ForceToga ? "YES" : "NO",
                Elev            = ParseElevationFt(runway),
                Temp            = st.OatC,
                QnhVal          = st.QnhHpa.ToString(CultureInfo.InvariantCulture),
                WindDir         = string.IsNullOrWhiteSpace(st.WindDir) ? "0" : st.WindDir,
                WindMag         = st.WindKt,
                SelectedFailures = new List<FailuresResponse>(),
            };

            try
            {
                st.IsBusy = true;
                st.LastError = "";
                st.IsUplinked = false;

                var efb = _app?.GsxService?.AircraftInterface?.ProsimInterface?.Efb;
                if (efb == null)
                {
                    st.LastError = "ProSim EFB unavailable";
                    return false;
                }

                var result = await efb.CalcVSpeeds(req);
                if (result == null)
                {
                    st.LastError = "V-speed calculation failed (no response)";
                    st.HasResult = false;
                    return false;
                }
                if (!string.IsNullOrEmpty(result.CalculationError))
                {
                    st.CalculationError = result.CalculationError;
                    st.HasResult = false;
                    return false;
                }

                ApplyResult(st, result, runway.LengthFt, selectedToraFt);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                st.LastError = ex.Message;
                return false;
            }
            finally
            {
                st.IsBusy = false;
            }
        }

        private void ApplyResult(TakeoffPerfState st, CalcVSpeedResult r, int runwayLengthFt, int selectedToraFt)
        {
            st.CalculationError = "";
            st.V1 = (int)Math.Round(r.VSpeed?.V1 ?? 0);
            st.VR = (int)Math.Round(r.VSpeed?.VR ?? 0);
            st.V2 = (int)Math.Round(r.VSpeed?.V2 ?? 0);
            st.FlapSettings = r.FlapSettings;

            // FlexOutput is the clamped value the EFB displays — authoritative
            // over Flex for both display and FMS uplink. 0 ⇒ TOGA.
            st.FlexOutputC = r.FlexOutput ?? 0;
            st.ForceTogaResult = r.ForceToga || st.FlexOutputC == 0;

            // Reconstruct signed THS from (magnitude, direction).
            var trimMag = r.TrimOutput ?? 0;
            var trimDir = r.TrimDir ?? "";
            int sign = string.Equals(trimDir, "DN", StringComparison.OrdinalIgnoreCase) ? -1 : +1;
            st.ThsValue = sign * trimMag;
            st.TrimDir = trimDir;

            st.ToplKg = r.Topl;
            st.ToplLimited = r.ToplLimited;
            st.HwCompKt = r.HwComp;
            st.GreenDot = r.GreenDot;

            // Runway shift — matches the EFB's calcToShift: feet delta, then
            // unit conversion + round to nearest 100. Sign convention: 0 for
            // full-length, positive for an intersection departure.
            st.ShiftM = ComputeRunwayShift(runwayLengthFt, selectedToraFt);

            st.HasResult = true;
        }

        // Match the EFB's calcToShift exactly:
        //   c = round(lengthFt - selectedToraFt)
        //   if user unit is "Meters": c /= 3.28084
        //   return 100 * round(c / 100)
        // We always read the unit dataref so the FMS displays the expected
        // shift value irrespective of the user's selected unit. Unknown /
        // unset unit defaults to "Feet" (the Western A320 norm).
        private int ComputeRunwayShift(int runwayLengthFt, int selectedToraFt)
        {
            double c = runwayLengthFt - selectedToraFt;
            var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
            var unit = sdk?.GetString(ProsimConstants.RefUnitTakeoffShift, "Feet") ?? "Feet";
            if (string.Equals(unit, "Meters", StringComparison.OrdinalIgnoreCase))
                c /= 3.28084;
            return (int)(100 * Math.Round(c / 100.0));
        }

        private static int ParseElevationFt(RunwayOption runway)
        {
            // The wire-shape's RunwayResponse.ElevationFt is a string. We
            // already projected lengthFt as int, but kept elevation as part
            // of the inbound projection — the perf calc only needs it for
            // the QNH influence. Today RunwayOption doesn't carry elevation;
            // gate against future field additions by returning 0 here.
            // (The /calculate/vspeeds gateway accepts 0 elevation and
            // computes accordingly; a sea-level fallback is operationally
            // safe and matches the EFB's behaviour when elev is missing.)
            return 0;
        }

        // -----------------------------------------------------------------
        //  FMS uplink — writes the seven datarefs the MCDU PERF page reads.
        // -----------------------------------------------------------------

        public virtual bool SendUplinkToFms()
        {
            var st = _app?.TakeoffPerf;
            if (st == null) return false;
            if (!st.HasResult)
            {
                st.LastError = "Run Calculate before sending uplink";
                return false;
            }

            var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
            if (sdk == null || !sdk.IsConnected)
            {
                st.LastError = "SDK not connected — cannot write FMS perf datarefs";
                return false;
            }

            var written = new List<string>();
            var failed = new List<string>();

            void TryInt(string dr, int v, string tag)
            {
                if (sdk.SetInt(dr, v)) written.Add(tag); else failed.Add(tag);
            }
            void TryDouble(string dr, double v, string tag)
            {
                if (sdk.SetDouble(dr, v)) written.Add(tag); else failed.Add(tag);
            }

            TryInt   (ProsimConstants.RefFmsPerfTakeoffV1,       st.V1,            "v1");
            TryInt   (ProsimConstants.RefFmsPerfTakeoffVr,       st.VR,            "vr");
            TryInt   (ProsimConstants.RefFmsPerfTakeoffV2,       st.V2,            "v2");
            TryInt   (ProsimConstants.RefFmsPerfTakeoffFlaps,    st.FlapSettings,  "flaps");
            // FlexTemp: 0 ⇒ TOGA on the FMS (the dataref's documented sentinel).
            TryDouble(ProsimConstants.RefFmsPerfTakeoffFlexTemp, st.FlexOutputC,   "flex");
            TryDouble(ProsimConstants.RefFmsPerfTakeoffThs,      st.ThsValue,      "ths");
            TryInt   (ProsimConstants.RefFmsPerfTakeoffShift,    st.ShiftM,        "shift");

            bool ok = failed.Count == 0 && written.Count > 0;
            if (ok)
            {
                st.IsUplinked = true;
                st.UplinkedAt = DateTime.UtcNow;
                st.LastError = "";
                Logger.Information(
                    $"FMS takeoff uplink: v1={st.V1} vr={st.VR} v2={st.V2} flaps={st.FlapSettings} " +
                    $"flex={st.FlexOutputC} ths={st.ThsValue:F1} shift={st.ShiftM}");
            }
            else
            {
                st.LastError = $"Failed to write: {string.Join(", ", failed)}";
                Logger.Warning($"FMS takeoff uplink partial — written=[{string.Join(",", written)}] failed=[{string.Join(",", failed)}]");
            }
            return ok;
        }

        public virtual void Reset()
        {
            _app?.TakeoffPerf?.Reset();
            _autoLoadedRunwaysForIcao = "";
        }
    }
}
