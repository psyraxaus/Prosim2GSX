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
    // Orchestrates the LANDING perf tab. Proxies /efb/airport/* +
    // /efb/calculate/ldr onto LandingPerfState. Unlike takeoff there's no
    // FMS uplink target — Airbus FMGCs don't accept a landing-distance
    // write. After each successful gateway call this service runs the
    // client-side derivations (LDA, LDR+15, head/cross wind, colour
    // classes) so both the WPF and React UIs render identically off the
    // same store fields.
    //
    // Tick() handles ICAO prefill from aircraft.fms.destination and the
    // shutdown-reset edge. The heavy work — runway loads, METARs, calcs —
    // is REST-driven and runs off-tick.
    public class LandingPerfService
    {
        private readonly AppService _app;

        // Wind crosswind limits (kt) indexed by runway condition code 1–6
        // (6=Dry … 1=Poor). Sourced from the EFB JS landing calculator —
        // do NOT collapse the duplicate 38s, they're real (codes 5 and 6
        // both pin to 38).
        private static readonly int[] CrosswindLimitKt = { 15, 20, 25, 29, 38, 38 };

        private bool _wasOnGround = true;
        private bool _wasEnginesRunning;
        private bool _primed;

        private static readonly string[] PolledRefs = new[]
        {
            ProsimConstants.RefFmsDestination,
        };

        public LandingPerfService(AppService app)
        {
            _app = app;
        }

        // -----------------------------------------------------------------
        //  Tick
        // -----------------------------------------------------------------

        public virtual void Tick()
        {
            try
            {
                var st = _app?.LandingPerf;
                if (st == null) return;

                var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
                if (sdk == null || !sdk.IsConnected)
                {
                    _primed = false;
                    return;
                }

                Prime(sdk);
                ProcessShutdownReset(st);

                if (string.IsNullOrWhiteSpace(st.Icao))
                {
                    var dest = sdk.GetString(ProsimConstants.RefFmsDestination, "") ?? "";
                    if (dest.Length == 4 && !string.Equals(dest, "Null", StringComparison.OrdinalIgnoreCase))
                        st.Icao = dest.ToUpperInvariant();
                }
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
            Logger.Debug($"LandingPerfService primed {PolledRefs.Length} datarefs");
        }

        private static void NoOpHandler(string name, dynamic newValue, dynamic oldValue) { }

        protected virtual void ProcessShutdownReset(LandingPerfState st)
        {
            var fs = _app?.FlightStatus;
            if (fs == null) return;

            bool nowOnGround = fs.AppOnGround;
            bool nowEnginesRunning = fs.AppEnginesRunning;

            if (nowOnGround && _wasEnginesRunning && !nowEnginesRunning)
            {
                st.Reset();
                Logger.Information("LandingPerfState reset on flight-cycle shutdown");
            }

            _wasOnGround = nowOnGround;
            _wasEnginesRunning = nowEnginesRunning;
        }

        // -----------------------------------------------------------------
        //  REST-driven actions
        // -----------------------------------------------------------------

        public virtual async Task<bool> LoadRunwaysAsync(string icao, CancellationToken ct = default)
        {
            var st = _app?.LandingPerf;
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

                // No intersections — the EFB doesn't offer intersection
                // landings.
                var runways = await efb.GetRunways(st.Icao, includeIntersections: false);
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
                }

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
            var st = _app?.LandingPerf;
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

                if (!string.IsNullOrWhiteSpace(metar.WindDir))
                    st.WindDir = metar.WindDir;
                if (!string.IsNullOrWhiteSpace(metar.WindSpeed)
                    && float.TryParse(metar.WindSpeed, NumberStyles.Float, CultureInfo.InvariantCulture, out var ws))
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

        public virtual async Task<bool> CalculateAsync(CancellationToken ct = default)
        {
            var st = _app?.LandingPerf;
            if (st == null) return false;

            if (string.IsNullOrWhiteSpace(st.RunwayId))
            {
                st.LastError = "Select a runway before calculating";
                return false;
            }
            if (st.LdgWeightTons <= 0)
            {
                st.LastError = "Landing weight must be set";
                return false;
            }

            var runway = st.Runways.FirstOrDefault(r => string.Equals(r.RunwayId, st.RunwayId, StringComparison.OrdinalIgnoreCase));
            if (runway == null)
            {
                st.LastError = "Selected runway is not in the loaded runway list";
                return false;
            }

            int rwyQdm = PerfProjection.ParseRunwayQdm(runway.RunwayId);

            // VRB resolves to QDM ± 180 on our side so the gateway sees a
            // numeric heading — it parses WindDir == "VRB" too, but going
            // through the same path simplifies the derivation math below.
            string wireWindDir;
            float effectiveWindDeg;
            if (string.Equals(st.WindDir, "VRB", StringComparison.OrdinalIgnoreCase))
            {
                int wd = rwyQdm > 180 ? rwyQdm - 180 : rwyQdm + 180;
                effectiveWindDeg = wd;
                wireWindDir = wd.ToString(CultureInfo.InvariantCulture);
            }
            else if (float.TryParse(st.WindDir, NumberStyles.Float, CultureInfo.InvariantCulture, out var wDeg))
            {
                effectiveWindDeg = wDeg;
                wireWindDir = ((int)Math.Round(wDeg)).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                effectiveWindDeg = 0f;
                wireWindDir = "0";
            }

            var req = new CalcLdrRequest
            {
                Qdm           = rwyQdm,
                Elev          = 0f,                                          // sea-level fallback (see TakeoffPerfService note)
                WindDir       = wireWindDir,
                WindSpeed     = st.WindKt,
                Oat           = st.OatC,
                Slp           = st.QnhHpa,
                BreakAction   = st.RwySurfaceCode,                           // intentional misspelling, gateway expects "Break*"
                AircraftSpeed = st.AircraftSpeedKt,
                LdgW          = (float)st.LdgWeightTons,                     // tons, not kg — gateway reference is 66 t
                BreakMode     = (st.BrakeMode ?? "MED").ToUpperInvariant(),  // intentional misspelling
                Rev           = (st.RevMode ?? "max").ToLowerInvariant(),    // gateway special-cases "idle"
                Autoland      = string.Equals(st.AutolandMode, "auto", StringComparison.OrdinalIgnoreCase) ? "1" : "0",
                FlapConfig    = (st.FlapConfig ?? "FULL").ToUpperInvariant(),
                Athr          = string.IsNullOrEmpty(st.Athr) ? "1" : st.Athr,
                SelectedFailures = new List<FailuresResponse>(),
            };

            try
            {
                st.IsBusy = true;
                st.LastError = "";

                var efb = _app?.GsxService?.AircraftInterface?.ProsimInterface?.Efb;
                if (efb == null)
                {
                    st.LastError = "ProSim EFB unavailable";
                    return false;
                }

                var result = await efb.CalcLdr(req);
                if (result == null)
                {
                    st.LastError = "Landing-distance calculation failed (no response)";
                    st.HasResult = false;
                    return false;
                }

                ApplyResult(st, result, runway, effectiveWindDeg, rwyQdm);
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

        private static void ApplyResult(LandingPerfState st, CalcLdrResponse r, RunwayOption runway, float windDeg, int qdm)
        {
            st.HasResult = true;
            st.IsNoData = r.Ldr == 0;
            st.RetreatFlap = r.Ldr == -2;          // A320 path doesn't emit it — defensive

            if (st.IsNoData || st.RetreatFlap)
            {
                st.LdrM = 0;
                st.Ldr15M = 0;
            }
            else
            {
                st.LdrM = r.Ldr;
                st.Ldr15M = (int)Math.Round(1.15 * r.Ldr);
            }

            // LDA: usable landing distance after subtracting the displaced
            // threshold. Computed in metres regardless of unit setting (the
            // visual-distance class compares against LdrM which is metres).
            double ldaM = (runway.LengthFt - runway.DtFt) / 3.28084;
            st.LdaM = ldaM;

            // Head/cross wind. Signed: positive HW = headwind, negative HW
            // = tailwind. XW is signed but the class check uses |XW|.
            double diffDeg = Math.Abs(qdm - windDeg);
            double rad = diffDeg * Math.PI / 180.0;
            double hw = Math.Cos(rad) * st.WindKt;
            double xw = Math.Sin(rad) * st.WindKt;
            st.HwKt = hw;
            st.XwKt = xw;

            // Class derivations — match the EFB's logic exactly.
            bool autoland = string.Equals(st.AutolandMode, "auto", StringComparison.OrdinalIgnoreCase);
            // HW: red if tailwind > 10 kt, or (autoland AND headwind > 30 kt).
            st.HwClass = (hw < -10 || (hw > 30 && autoland)) ? "red" : "normal";

            int absXw = (int)Math.Round(Math.Abs(xw));
            if (autoland)
            {
                int code = Math.Max(1, Math.Min(6, st.RwySurfaceCode));
                int limit = CrosswindLimitKt[code - 1];
                st.XwClass = absXw > limit ? "red" : "normal";
            }
            else
            {
                st.XwClass = absXw > 20 ? "red" : "normal";
            }

            if (st.IsNoData || st.RetreatFlap)
            {
                st.VisualDistClass = "normal";
            }
            else if (ldaM < r.Ldr)
            {
                st.VisualDistClass = "red";
            }
            else if (ldaM < 1.15 * r.Ldr)
            {
                st.VisualDistClass = "red-margin";
            }
            else
            {
                st.VisualDistClass = "normal";
            }
        }

        public virtual void Reset()
        {
            _app?.LandingPerf?.Reset();
        }
    }
}
