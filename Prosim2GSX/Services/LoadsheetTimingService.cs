using CFIT.AppLogger;
using Prosim2GSX.State;
using System;

namespace Prosim2GSX.Services
{
    // Auto-trigger timing for the loadsheet workflow. Three operational
    // notifications get raised on the lifecycle of a flight:
    //
    //   1. Prelim due — at T-PrelimOffsetMinutes before STD (default 30).
    //      Info severity. Suppressed if the prelim has already arrived.
    //   2. Prelim overdue — at T-0 if the prelim still hasn't arrived.
    //      Warning severity. Different Type from (1) so the React banner
    //      shows it as a fresh alert rather than mutating the existing
    //      info notification.
    //   3. Final due — on the boarding-complete rising edge (the SDK's
    //      ProsimBoardingService writes "completed" to efb.efb.boardingStatus
    //      when GSX finishes, the native ProSim EFB writes "ended" when
    //      the user manually completes; IsEfbBoardingCompleted accepts
    //      both). Info severity. Fires before the SDK actually transmits
    //      the final loadsheet — the SDK has a randomised FinalDelay
    //      (Profile.FinalDelay{Min,Max}) between boarding-complete and
    //      the final-loadsheet POST, so this is a heads-up.
    //
    // STD source priority:
    //   1. EfbFlightPlanState.CurrentOfp.Std (the EfbFlightPlanService
    //      tick projects this from both the manual fetch and the MCDU-
    //      triggered auto-fetch, so a single read covers both paths).
    //   2. Manual override via SetStd (POST /api/loadsheet/set-std).
    //
    // No ProSim STD dataref exists — confirmed against
    // ProsimDataref.csv. SimBrief direct-read isn't a separate fallback
    // because EfbFlightPlanService already projects it into OFPData.Std.
    //
    // Time source: AircraftInterface.ZuluTimeSeconds when the sim is
    // connected (matches the header bar's UTC clock), with DateTime.UtcNow
    // fallback when sim isn't running. Compared against STD's time-of-day
    // only; date is ignored because ZuluTimeSeconds is seconds-since-
    // midnight with no date component. Day-rollover is handled by wrapping
    // the delta into a [-12h, +12h] window.
    //
    // Reset semantics:
    //   - Shutdown edge (on-ground + engines running → off): clears all
    //     fired flags + manual STD override + re-seeds the boarding-edge
    //     baseline. Mirrors LoadsheetService.ProcessShutdownReset exactly.
    //   - STD change (new OFP loaded with a different Std): clears the
    //     fired flags so prelim/overdue can re-fire against the new
    //     timing.
    public class LoadsheetTimingService
    {
        private readonly AppService _app;

        // Fired flags so each notification only fires once per flight.
        private bool _prelimDueFired;
        private bool _prelimOverdueFired;
        private bool _finalDueFired;

        // Rising-edge tracker for IsEfbBoardingCompleted. Seeded on first
        // tick from the current dataref value so a stale "completed" left
        // over from a previous flight (without a clean engine shutdown)
        // can't trigger a false-positive "Final due" notification.
        private bool _lastBoardingCompleted;
        private bool _boardingSeeded;

        // Manual STD override — used only when no OFP is loaded. Cleared
        // on shutdown reset; OFP-derived STD always wins when present.
        private DateTime? _manualStdOverride;

        // STD change tracker — clears fired flags when the OFP's STD moves
        // (e.g. user reloads a different OFP without shutting down).
        private DateTime? _lastStdSeen;

        // Shutdown-edge tracker. Mirrors LoadsheetService.
        private bool _wasOnGround = true;
        private bool _wasEnginesRunning;

        // 60-second throttle for the STD-based time evaluation. Boarding-
        // edge detection runs every tick (responsiveness > polling cost),
        // but the prelim/overdue checks only need minute resolution.
        private DateTime _lastStdEvalUtc = DateTime.MinValue;

        public LoadsheetTimingService(AppService app)
        {
            _app = app;
        }

        public virtual void Tick()
        {
            try
            {
                if (_app?.Config?.LoadsheetAutoTriggerEnabled != true) return;

                ProcessShutdownReset();
                ProcessStdChange();
                ProcessBoardingComplete();

                var now = DateTime.UtcNow;
                if ((now - _lastStdEvalUtc).TotalSeconds < 60) return;
                _lastStdEvalUtc = now;

                ProcessStdTriggers();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Manual STD override. Only takes effect when no OFP is loaded —
        // OFP-derived STD always wins. Cleared on the shutdown edge so
        // each flight starts clean.
        public virtual void SetStd(DateTime utc)
        {
            _manualStdOverride = utc.Kind == DateTimeKind.Utc
                ? utc
                : utc.ToUniversalTime();
            _lastStdEvalUtc = DateTime.MinValue; // re-evaluate on next tick
            Logger.Information(
                $"Loadsheet timing: manual STD set to {_manualStdOverride:O}");
        }

        public virtual void ClearManualStd()
        {
            if (_manualStdOverride.HasValue)
                Logger.Information("Loadsheet timing: manual STD cleared");
            _manualStdOverride = null;
        }

        public virtual DateTime? ResolveStd()
        {
            var ofpStd = _app?.EfbFlightPlan?.CurrentOfp?.Std;
            if (ofpStd.HasValue) return ofpStd;
            return _manualStdOverride;
        }

        // ── Reset paths ────────────────────────────────────────────────────

        protected virtual void ProcessShutdownReset()
        {
            var fs = _app?.FlightStatus;
            if (fs == null) return;

            bool nowOnGround = fs.AppOnGround;
            bool nowEnginesRunning = fs.AppEnginesRunning;

            if (nowOnGround && _wasEnginesRunning && !nowEnginesRunning)
            {
                ResetFiredFlags();
                _manualStdOverride = null;
                _lastStdSeen = null;
                _boardingSeeded = false; // re-seed on next tick from current dataref
                Logger.Information("Loadsheet timing: state reset on flight-cycle shutdown");
            }

            _wasOnGround = nowOnGround;
            _wasEnginesRunning = nowEnginesRunning;
        }

        protected virtual void ProcessStdChange()
        {
            var current = ResolveStd();
            if (current == _lastStdSeen) return;

            // First observation isn't a "change" — just seed.
            if (_lastStdSeen.HasValue && current != _lastStdSeen)
            {
                ResetFiredFlags();
                _lastStdEvalUtc = DateTime.MinValue; // force re-evaluation immediately
                Logger.Information(
                    $"Loadsheet timing: STD changed ({_lastStdSeen:HH:mm} → {current:HH:mm}), fired flags reset");
            }
            _lastStdSeen = current;
        }

        private void ResetFiredFlags()
        {
            _prelimDueFired = false;
            _prelimOverdueFired = false;
            _finalDueFired = false;
        }

        // ── Trigger paths ──────────────────────────────────────────────────

        protected virtual void ProcessBoardingComplete()
        {
            var ai = _app?.GsxService?.AircraftInterface;
            if (ai == null) return;

            bool nowComplete;
            try { nowComplete = ai.IsEfbBoardingCompleted; }
            catch { return; }

            // First tick: seed the baseline from the current dataref value
            // so a stale "completed" from a previous flight doesn't look
            // like a fresh rising edge.
            if (!_boardingSeeded)
            {
                _lastBoardingCompleted = nowComplete;
                _boardingSeeded = true;
                return;
            }

            // Rising edge — fire once.
            if (nowComplete && !_lastBoardingCompleted && !_finalDueFired)
            {
                EmitNotification(
                    type: "loadsheet_final_due",
                    severity: "info",
                    message: "Boarding complete — final loadsheet due");
                _finalDueFired = true;
            }

            _lastBoardingCompleted = nowComplete;
        }

        protected virtual void ProcessStdTriggers()
        {
            var std = ResolveStd();
            if (!std.HasValue) return;

            var minutesToStd = ComputeMinutesToStd(std.Value);
            int prelimOffset = Math.Max(1, _app?.Config?.LoadsheetPrelimOffsetMinutes ?? 30);

            bool prelimReceived =
                string.Equals(_app?.Loadsheet?.PrelimStatus, "received", StringComparison.OrdinalIgnoreCase);

            // Prelim due at T-PrelimOffset, suppressed if already received.
            if (!_prelimDueFired
                && !prelimReceived
                && minutesToStd <= prelimOffset
                && minutesToStd > -prelimOffset) // avoid firing for ancient pre-arm windows on first eval
            {
                EmitNotification(
                    type: "loadsheet_prelim_due",
                    severity: "info",
                    message: $"Prelim loadsheet due — {prelimOffset} minutes to departure");
                _prelimDueFired = true;
            }

            // Overdue at T-0, suppressed if prelim arrived in the meantime.
            if (!_prelimOverdueFired
                && !prelimReceived
                && minutesToStd <= 0
                && minutesToStd > -prelimOffset) // 12-hour wrap can produce massive negatives, ignore those
            {
                EmitNotification(
                    type: "loadsheet_prelim_overdue",
                    severity: "warning",
                    message: "Prelim loadsheet overdue — STD reached without preliminary loadsheet");
                _prelimOverdueFired = true;
            }
        }

        // Minutes from "now" (sim-zulu when connected, UtcNow otherwise) to
        // STD's time-of-day. Day rollover is handled by wrapping the delta
        // into [-12h, +12h] so e.g. STD 23:30Z when sim is at 00:15Z reads
        // as -45 min, not +1395 min.
        protected virtual double ComputeMinutesToStd(DateTime std)
        {
            int nowTodSec = ResolveSimTodSeconds();
            int stdTodSec = std.Hour * 3600 + std.Minute * 60 + std.Second;
            int delta = stdTodSec - nowTodSec;
            const int day = 24 * 3600;
            const int half = 12 * 3600;
            if (delta > half) delta -= day;
            else if (delta < -half) delta += day;
            return delta / 60.0;
        }

        protected virtual int ResolveSimTodSeconds()
        {
            // Mirror StateUpdateWorker.UpdateApp's UTC-source precedence so
            // the timing service and the header bar agree on "now". Sim-
            // zulu when in a session, wall-clock UTC otherwise.
            var sim = _app?.SimConnect;
            var ai = _app?.GsxService?.AircraftInterface;
            bool simConnected = sim?.IsSimConnected == true && sim?.IsSessionRunning == true;
            if (simConnected && ai != null)
            {
                int z = ai.ZuluTimeSeconds;
                if (z > 0) return z;
            }
            var now = DateTime.UtcNow;
            return now.Hour * 3600 + now.Minute * 60 + now.Second;
        }

        // ── Emission ───────────────────────────────────────────────────────

        protected virtual void EmitNotification(string type, string severity, string message)
        {
            var store = _app?.Notifications;
            if (store == null) return;

            store.Add(new Notification
            {
                Type = type,
                Severity = severity,
                Message = message,
                Timestamp = DateTime.UtcNow,
            });

            // Mirror to the CFIT log so WPF users (whose primary surface is
            // the Monitor tab's message log) see the same alert without
            // needing a separate WPF notification banner.
            Logger.Information($"[{severity.ToUpperInvariant()}] {message}");
        }
    }
}
