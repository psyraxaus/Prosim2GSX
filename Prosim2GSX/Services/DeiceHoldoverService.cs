using CFIT.AppLogger;
using Prosim2GSX.GSX;
using Prosim2GSX.GSX.Services;
using System;

namespace Prosim2GSX.Services
{
    // Drives DeiceHoldoverState. On the rising edge of GSX deicing
    // completion it captures the applied fluid type (GSX LVAR), the
    // concentration (Config.AutoDeiceFluid — GSX doesn't expose it), the
    // crew-supplied OAT + precipitation, looks up the holdover window, and
    // starts a wall-clock countdown ticked from StateUpdateWorker.
    //
    // Readout only — by design it never writes environment.icing or any
    // ProSim dataref (dataref-first: read, don't drive). It is a HOT card,
    // not an icing-protection model.
    public class DeiceHoldoverService
    {
        private readonly AppService _app;

        // Rising-edge tracker for GSX deice completion.
        private bool _wasDeiceCompleted;
        private bool _deiceSeeded;

        // Shutdown-edge tracker (mirrors LoadsheetTimingService): clear the
        // card when the aircraft shuts down on the ground so the next
        // flight starts fresh.
        private readonly FlightCycleEdgeDetector _flightCycle = new();

        public DeiceHoldoverService(AppService app)
        {
            _app = app;
        }

        public virtual void Tick()
        {
            try
            {
                ProcessShutdownReset();
                ProcessDeiceCompletion();
                ProcessOatPrefill();
                ProcessCountdown();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // ── Web/UI inputs ────────────────────────────────────────────────

        public virtual void SetPrecip(HotPrecip precip)
        {
            _app.DeiceHoldover.Precip = precip;
            Recompute();
        }

        public virtual void SetOat(double oatC)
        {
            var s = _app.DeiceHoldover;
            s.OatC = oatC;
            s.OatUserSet = true;
            Recompute();
        }

        // ── Pipeline ─────────────────────────────────────────────────────

        // Prefill OAT from the takeoff-perf OAT (same departure value the
        // crew already entered there) until the user sets it on the HOT
        // card. Avoids a second data-entry and avoids a cross-repo ProSim
        // dataref add — ProSim has no clean at-aircraft OAT for this.
        private void ProcessOatPrefill()
        {
            var s = _app.DeiceHoldover;
            if (s.OatUserSet) return;
            double perfOat = _app?.TakeoffPerf?.OatC ?? 0;
            if (Math.Abs(perfOat - s.OatC) > 0.01)
                s.OatC = perfOat;
        }

        private void ProcessDeiceCompletion()
        {
            var gsx = _app?.GsxService;
            if (gsx == null) return;
            if (!gsx.GsxServices.TryGetValue(GsxServiceType.Deice, out var deice) || deice == null)
                return;

            bool completed = deice.IsCompleted;

            if (!_deiceSeeded)
            {
                // Seed from the current value so a stale "completed" left
                // over from a previous session can't auto-start a window.
                _wasDeiceCompleted = completed;
                _deiceSeeded = true;
                return;
            }

            if (completed && !_wasDeiceCompleted)
                StartHoldover();

            _wasDeiceCompleted = completed;
        }

        private void StartHoldover()
        {
            var s = _app.DeiceHoldover;

            int gsxType = _app?.GsxService?.CurrentDeiceTypeRaw ?? 0;
            var (cfgType, conc) = MapConfigFluid(_app?.Config?.AutoDeiceFluid ?? AutoDeiceFluid.TypeIV100);
            int fluidType = gsxType is >= 1 and <= 4 ? gsxType : cfgType;

            s.FluidType = fluidType;
            s.Concentration = conc;
            s.FluidLabel = $"Type {Roman(fluidType)} {conc}%";
            s.StartedUtc = DateTime.UtcNow;

            Recompute();

            Logger.Information(
                $"Deice holdover started: {s.FluidLabel}, OAT {s.OatC:0.#}°C, "
                + $"precip={s.Precip}, window={s.LowMinutes:0}-{s.HighMinutes:0} min");
        }

        // Recompute the window from current inputs. Called on start and
        // whenever the crew changes precip/OAT while a window is running.
        private void Recompute()
        {
            var s = _app.DeiceHoldover;
            if (s.StartedUtc == null) return;

            var win = HotMatrix.Lookup(s.FluidType, s.Concentration, s.OatC, s.Precip);
            if (win == null)
            {
                s.Active = false;
                s.Expired = false;
                s.LowMinutes = 0;
                s.HighMinutes = 0;
                s.RemainingLowSeconds = 0;
                s.RemainingHighSeconds = 0;
                s.Status = s.Precip == HotPrecip.None
                    ? $"{s.FluidLabel} applied · no active precipitation — no holdover"
                    : $"{s.FluidLabel} · {Describe(s.Precip)} · below fluid LOUT — no holdover";
                return;
            }

            s.LowMinutes = win.Value.LowMinutes;
            s.HighMinutes = win.Value.HighMinutes;
            s.Active = true;
            s.Expired = false;
            ProcessCountdown();
        }

        private void ProcessCountdown()
        {
            var s = _app.DeiceHoldover;
            if (!s.Active || s.StartedUtc == null) return;

            double elapsed = (DateTime.UtcNow - s.StartedUtc.Value).TotalSeconds;
            int lo = (int)Math.Max(0, Math.Round(s.LowMinutes * 60 - elapsed));
            int hi = (int)Math.Max(0, Math.Round(s.HighMinutes * 60 - elapsed));
            s.RemainingLowSeconds = lo;
            s.RemainingHighSeconds = hi;

            if (hi <= 0)
            {
                s.Active = false;
                s.Expired = true;
                s.Status = $"{s.FluidLabel} · {Describe(s.Precip)} · HOLDOVER EXPIRED — re-treatment required";
                Logger.Information("Deice holdover expired");
                return;
            }

            s.Status = $"{s.FluidLabel} · {Describe(s.Precip)} · "
                + $"{FmtMin(lo)}–{FmtMin(hi)} remaining";
        }

        private void ProcessShutdownReset()
        {
            bool onGround = _app?.FlightStatus?.AppOnGround ?? true;
            bool engines = _app?.FlightStatus?.AppEnginesRunning ?? false;
            _flightCycle.Update(onGround, engines);

            // Engines were running on the ground, now off → flight done. Uses the
            // stricter "stable" edge (also requires on-ground the previous tick).
            if (_flightCycle.EngineShutdownOnGroundStable)
                Clear();
        }

        private void Clear()
        {
            var s = _app.DeiceHoldover;
            if (s.StartedUtc == null && !s.Active && !s.Expired) return;
            s.Active = false;
            s.Expired = false;
            s.StartedUtc = null;
            s.FluidType = 0;
            s.FluidLabel = "";
            s.Concentration = 0;
            s.LowMinutes = 0;
            s.HighMinutes = 0;
            s.RemainingLowSeconds = 0;
            s.RemainingHighSeconds = 0;
            s.Status = "";
            // Precip / OatUserSet / OatC are crew settings — left intact
            // across flights on purpose (re-prefill resumes if not user-set).
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static (int type, int conc) MapConfigFluid(AutoDeiceFluid f) => f switch
        {
            AutoDeiceFluid.TypeI100 => (1, 100),
            AutoDeiceFluid.TypeI75 => (1, 75),
            AutoDeiceFluid.TypeII100 => (2, 100),
            AutoDeiceFluid.TypeII75 => (2, 75),
            AutoDeiceFluid.TypeIV100 => (4, 100),
            AutoDeiceFluid.TypeIV75 => (4, 75),
            _ => (4, 100),
        };

        private static string Roman(int t) => t switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV", _ => "?",
        };

        private static string FmtMin(int seconds)
        {
            int m = seconds / 60;
            int sec = seconds % 60;
            return $"{m}:{sec:00}";
        }

        private static string Describe(HotPrecip p) => p switch
        {
            HotPrecip.None => "no precip",
            HotPrecip.ActiveFrost => "active frost",
            HotPrecip.FreezingFog => "freezing fog",
            HotPrecip.Snow => "snow",
            HotPrecip.FreezingDrizzleLight => "light freezing drizzle",
            HotPrecip.FreezingDrizzleModerate => "moderate freezing drizzle",
            HotPrecip.LightFreezingRain => "light freezing rain",
            HotPrecip.RainOnColdSoakedWing => "rain on cold-soaked wing",
            _ => p.ToString(),
        };
    }
}
