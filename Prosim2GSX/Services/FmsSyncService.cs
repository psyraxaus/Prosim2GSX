using CFIT.AppLogger;
using Prosim2GSX.State;
using Prosim2GSX.Web.Contracts;
using ProsimInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Prosim2GSX.Services
{
    // Resolves the "current" MACZFW value (the headline figure displayed
    // next to the SYNC TO FMS button) and writes the FMS init datarefs on
    // demand.
    //
    // Resolution chain (final → prelim → computed) mirrors how the EFB
    // surfaces MACZFW through the flight workflow: the final loadsheet is
    // authoritative once issued, the prelim is the planned value before
    // boarding completes, and the live aircraft.zfwcg dataref is the
    // last-resort fallback so the panel always shows something usable.
    //
    // Validation bounds are sourced from LoadsheetState.MinMacTow /
    // MaxMacTow (10.5 / 45.0). The bounds are operationally a MACTOW
    // envelope but apply to MACZFW too — any out-of-envelope MACTOW
    // implies a suspect ZFW CG, so we use the same gate to disable the
    // SYNC TO FMS button.
    //
    // The FMS sync writes three FMS init datarefs:
    //   aircraft.fms.init.zfw    ← loadsheet zfw (final → prelim → live fallback)
    //   aircraft.fms.init.zfwcg  ← loadsheet macZfw (final → prelim → live fallback)
    //   aircraft.fms.init.block  ← ceil(WeightBalance.FuelPlannedKg / 100) * 100
    // The block-fuel field is rounded UP to the nearest 100 kg per
    // operational convention (FMS block entries are typically whole hectos).
    //
    // ZFW + ZFWCG come from the loadsheet (the dispatcher-signed values)
    // rather than live aircraft.weight.zfw / aircraft.zfwcg so the FMS
    // matches what the loadsheet says — even if pax/cargo continue to
    // settle after the loadsheet was generated. When no loadsheet has
    // been received yet the resolver falls back to the live datarefs so
    // the button is still usable for headless / dispatch-less flying.
    //
    // THS / stab-trim is NOT written. The only writable trim dataref in
    // ProsimDataref.csv is aircraft.fms.perf.takeOff.ths, and the EFB's
    // own main.<hash>.js binding for that path has setValue:()=>{} with no
    // derivation logic (confirmed by user inspection). Guessing a THS value
    // would violate the dataref-first principle, so the field is reported
    // as Skipped in the result rather than written or silently ignored.
    public class FmsSyncService
    {
        // Operational tolerances for "FMS out of date" detection. Match
        // A320 dispatch practice: ZFWCG is entered to 0.1 %MAC, ZFW to
        // the next 100 kg, block fuel to the next 100 kg (we round UP on
        // write, so the smallest *real* operational change is one full
        // step beyond the rounding step → 200 kg).
        public const double StaleThresholdMacPercent = 0.1;
        public const double StaleThresholdZfwKg = 100.0;
        public const double StaleThresholdBlockKg = 200.0;

        private readonly AppService _app;

        // Last-sync snapshot — set on every successful SyncToFms(). Cleared
        // on flight-cycle reset (LoadsheetService calls ResetSyncTracking).
        // Null _lastSyncedAt = "never synced this flight" → not "stale", just
        // "untouched", so FmsSyncStale stays false until the user actually
        // pushes once.
        private DateTime? _lastSyncedAt;
        private double _lastSyncedMaczfw;
        private double _lastSyncedZfwKg;
        private double _lastSyncedBlockKg;
        private string _lastSyncedSource = "";

        // Auto-sync edge tracking. Re-fires when FinalReceivedAt advances
        // (covers the resend case which writes a fresh timestamp). Null
        // sentinel = no auto-sync fired yet for the current final slot.
        private DateTime? _autoSyncFiredForFinalAt;
        private LoadsheetState _attachedLoadsheet;

        public FmsSyncService(AppService app)
        {
            _app = app;
        }

        // Wire the auto-sync subscription. Called from
        // AppService.CreateServiceControllers after construction so the
        // service has a valid AppService reference and Loadsheet is set.
        // Idempotent — repeated calls re-attach to the same store without
        // double-subscribing.
        public virtual void Attach()
        {
            var ls = _app?.Loadsheet;
            if (ls == null || ReferenceEquals(_attachedLoadsheet, ls)) return;
            if (_attachedLoadsheet != null)
                _attachedLoadsheet.PropertyChanged -= OnLoadsheetChanged;
            _attachedLoadsheet = ls;
            _attachedLoadsheet.PropertyChanged += OnLoadsheetChanged;
        }

        private void OnLoadsheetChanged(object sender, PropertyChangedEventArgs e)
        {
            // Re-evaluate on Status or ReceivedAt changes — both flag the
            // "final has arrived (or been resent)" edge. Other prelim
            // updates are irrelevant to the auto-sync trigger.
            if (e?.PropertyName != nameof(LoadsheetState.FinalStatus)
                && e?.PropertyName != nameof(LoadsheetState.FinalReceivedAt))
                return;
            TryAutoSyncOnFinal();
        }

        protected virtual void TryAutoSyncOnFinal()
        {
            try
            {
                var cfg = _app?.Config;
                var ls = _app?.Loadsheet;
                if (cfg == null || ls == null) return;
                if (!cfg.AutoSyncFmsOnFinal) return;
                if (ls.FinalStatus != "received") return;
                var receivedAt = ls.FinalReceivedAt;
                if (receivedAt == null) return;

                // Same final timestamp we already auto-fired for? skip.
                if (_autoSyncFiredForFinalAt.HasValue && _autoSyncFiredForFinalAt.Value == receivedAt.Value)
                    return;

                // Final loadsheet's MACTOW envelope check still gates the
                // auto-sync — a loadsheet whose MACTOW is out-of-envelope
                // is suspect overall, so we don't push its ZFW/MACZFW into
                // the FMS automatically.
                if (ls.FinalMacTowError)
                {
                    Logger.Warning("FMS auto-sync skipped — final loadsheet MACTOW out of envelope");
                    _autoSyncFiredForFinalAt = receivedAt;
                    return;
                }

                Logger.Information("FMS auto-sync triggered by final loadsheet arrival");
                _autoSyncFiredForFinalAt = receivedAt;
                SyncToFms();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Cleared by LoadsheetService.ProcessShutdownReset so a new flight
        // starts with no "stale" baseline. Also clears the auto-sync edge
        // marker so the first final loadsheet of the next flight fires.
        public virtual void ResetSyncTracking()
        {
            _lastSyncedAt = null;
            _lastSyncedMaczfw = 0;
            _lastSyncedZfwKg = 0;
            _lastSyncedBlockKg = 0;
            _lastSyncedSource = "";
            _autoSyncFiredForFinalAt = null;

            var wb = _app?.WeightBalance;
            if (wb != null)
            {
                wb.FmsSyncStale = false;
                wb.FmsLastSyncedAt = null;
                wb.FmsLastSyncedSource = "";
            }
        }

        // Called from WeightBalanceService.Tick AFTER MaczfwResolvedPercent /
        // MaczfwResolvedSource have been written. Compares the current
        // resolved values against the last-sync snapshot and sets
        // FmsSyncStale. No-op when never synced.
        //
        // Drift is checked against the *resolver outputs* (loadsheet → live
        // fallback) — the same shape Sync would push if pressed now.
        // Comparing live wb.ZfwKg / wb.MaczfwPercent against a last-synced
        // snapshot that came from the loadsheet would always look stale
        // (live values keep moving while the signed loadsheet stays put),
        // so we re-resolve on each tick instead.
        public virtual void UpdateStaleness()
        {
            try
            {
                var wb = _app?.WeightBalance;
                if (wb == null) return;
                if (_lastSyncedAt == null)
                {
                    if (wb.FmsSyncStale) wb.FmsSyncStale = false;
                    return;
                }

                double currentBlock = RoundBlockFuelKg(wb.FuelPlannedKg);
                var currentTrio = ResolveCurrentZfwTrio();

                bool sourceUpgraded =
                    string.Equals(_lastSyncedSource, "prelim", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(currentTrio.Source, "final", StringComparison.OrdinalIgnoreCase);

                bool drift =
                    Math.Abs(currentTrio.MaczfwPercent - _lastSyncedMaczfw) > StaleThresholdMacPercent
                    || Math.Abs(currentTrio.ZfwKg - _lastSyncedZfwKg) > StaleThresholdZfwKg
                    || Math.Abs(currentBlock - _lastSyncedBlockKg) > StaleThresholdBlockKg;

                wb.FmsSyncStale = sourceUpgraded || drift;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Resolve the current MACZFW. Source order: FinalStatus="received"
        // → PrelimStatus="received" → live aircraft.zfwcg mirrored on
        // WeightBalanceState.MaczfwPercent. The Source out param is
        // informational — drives the panel's "FINAL LS / PRELIM LS /
        // COMPUTED" chip and the SYNC TO FMS button label.
        public virtual (double Maczfw, string Source) ResolveCurrentMaczfw()
        {
            var ls = _app?.Loadsheet;
            if (ls != null)
            {
                if (string.Equals(ls.FinalStatus, "received", StringComparison.OrdinalIgnoreCase) && ls.FinalMacZfw > 0)
                    return (ls.FinalMacZfw, "final");
                if (string.Equals(ls.PrelimStatus, "received", StringComparison.OrdinalIgnoreCase) && ls.PrelimMacZfw > 0)
                    return (ls.PrelimMacZfw, "prelim");
            }

            var wb = _app?.WeightBalance;
            return (wb?.MaczfwPercent ?? 0.0, "computed");
        }

        // Resolve the (ZFW kg, MACZFW %, source) trio that should be
        // written to the FMS. The pair is always sourced together so the
        // FMS gets a consistent ZFW/CG snapshot — mixing live ZFW with
        // loadsheet MACZFW would be operationally incoherent. Source order
        // matches ResolveCurrentMaczfw: final → prelim → computed (live).
        // The live fallback uses aircraft.weight.zfw + aircraft.zfwcg
        // mirrored on WeightBalanceState so the button is still usable
        // before any loadsheet has been received.
        public virtual (double ZfwKg, double MaczfwPercent, string Source) ResolveCurrentZfwTrio()
        {
            var ls = _app?.Loadsheet;
            if (ls != null)
            {
                if (string.Equals(ls.FinalStatus, "received", StringComparison.OrdinalIgnoreCase)
                    && ls.FinalZfwKg > 0 && ls.FinalMacZfw > 0)
                    return (ls.FinalZfwKg, ls.FinalMacZfw, "final");
                if (string.Equals(ls.PrelimStatus, "received", StringComparison.OrdinalIgnoreCase)
                    && ls.PrelimZfwKg > 0 && ls.PrelimMacZfw > 0)
                    return (ls.PrelimZfwKg, ls.PrelimMacZfw, "prelim");
            }

            var wb = _app?.WeightBalance;
            return (wb?.ZfwKg ?? 0.0, wb?.MaczfwPercent ?? 0.0, "computed");
        }

        // Envelope check using the same bounds the loadsheet parser uses.
        // Zero/negative MAC counts as in-range (no data to flag) so the
        // SYNC TO FMS button isn't disabled before any source is available.
        // Bounds are LoadsheetState.MinMacTow / MaxMacTow — operationally
        // a MACTOW envelope but it applies to MACZFW too because the two
        // values are bounded together by the airframe's CG limits.
        public virtual bool IsOutOfRange(double mac)
        {
            if (mac <= 0) return false;
            var ls = _app?.Loadsheet;
            if (ls == null) return false;
            return mac < ls.MinMacTow || mac > ls.MaxMacTow;
        }

        // Round block fuel UP to the nearest 100 kg. Negative or zero input
        // returns 0 (no point writing junk to the FMS).
        public static double RoundBlockFuelKg(double rawKg)
        {
            if (rawKg <= 0) return 0;
            return Math.Ceiling(rawKg / 100.0) * 100.0;
        }

        // Perform the FMS sync. Returns a populated DTO regardless of
        // success — the caller (controller + WS broadcast) wants the
        // structure either way. SDK-degraded mode short-circuits with a
        // clear error message rather than letting per-field writes fail.
        public virtual FmsSyncResultDto SyncToFms()
        {
            var result = new FmsSyncResultDto
            {
                Timestamp = DateTime.UtcNow,
                SkippedFields = new[] { "stabTrim" },
            };

            try
            {
                var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
                if (sdk == null || !sdk.IsConnected)
                {
                    result.Success = false;
                    result.ErrorMessage = "ProSim SDK not connected";
                    PopulateBroadcastFields(result);
                    BroadcastResult(result);
                    return result;
                }

                var wb = _app?.WeightBalance;
                if (wb == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Weight & Balance state not available";
                    PopulateBroadcastFields(result);
                    BroadcastResult(result);
                    return result;
                }

                var written = new List<string>();
                var failed = new List<string>();

                // ZFW + ZFWCG are sourced from the loadsheet (final → prelim
                // → live fallback) so the FMS receives the dispatcher-signed
                // pair, not the live aircraft state. The pair is resolved
                // together so we never mix sources (loadsheet ZFW with live
                // MACZFW would be operationally incoherent).
                var zfwTrio = ResolveCurrentZfwTrio();

                // The MCDU INIT B page expects ZFW and BLOCK in TONS (xx.x),
                // not kg. Without the /1000 the FMS displays the raw kg value
                // (e.g. 56778) instead of the expected 56.8. ZFWCG is %MAC
                // and stays as-is. The log line keeps kg for ops familiarity.
                var zfwTons = zfwTrio.ZfwKg / 1000.0;
                var blockKg = RoundBlockFuelKg(wb.FuelPlannedKg);
                var blockTons = blockKg / 1000.0;

                if (sdk.SetDouble(ProsimConstants.RefFmsInitZfw, zfwTons))
                    written.Add("zfw");
                else
                    failed.Add("zfw");

                if (sdk.SetDouble(ProsimConstants.RefFmsInitZfwcg, zfwTrio.MaczfwPercent))
                    written.Add("zfwcg");
                else
                    failed.Add("zfwcg");

                if (sdk.SetDouble(ProsimConstants.RefFmsInitBlock, blockTons))
                    written.Add("blockFuel");
                else
                    failed.Add("blockFuel");

                result.WrittenFields = written.ToArray();
                result.FailedFields = failed.ToArray();
                result.Success = failed.Count == 0 && written.Count > 0;
                if (!result.Success && string.IsNullOrEmpty(result.ErrorMessage))
                    result.ErrorMessage = failed.Count > 0 ? $"Failed to write: {string.Join(", ", failed)}" : "No fields written";

                Logger.Information(
                    $"FMS sync: zfw={zfwTrio.ZfwKg:F0}kg ({zfwTons:F1}t) zfwcg={zfwTrio.MaczfwPercent:F2} ({zfwTrio.Source}) " +
                    $"block={blockKg:F0}kg ({blockTons:F1}t) " +
                    $"written=[{string.Join(",", written)}] failed=[{string.Join(",", failed)}]");

                if (result.Success)
                {
                    // Track the *resolved* values that were actually written so
                    // UpdateStaleness compares apples-to-apples on the next
                    // tick (live ZFW/MACZFW will keep drifting against the
                    // signed loadsheet — that drift is expected, not stale).
                    _lastSyncedAt = result.Timestamp;
                    _lastSyncedZfwKg = zfwTrio.ZfwKg;
                    _lastSyncedMaczfw = zfwTrio.MaczfwPercent;
                    _lastSyncedBlockKg = blockKg;
                    _lastSyncedSource = zfwTrio.Source ?? "";
                    wb.FmsLastSyncedAt = _lastSyncedAt;
                    wb.FmsLastSyncedSource = _lastSyncedSource;
                    wb.FmsSyncStale = false;
                }

                PopulateBroadcastFields(result);
                BroadcastResult(result);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                result.Success = false;
                result.ErrorMessage = ex.Message ?? "Unexpected error";
                PopulateBroadcastFields(result);
                BroadcastResult(result);
                return result;
            }
        }

        protected virtual void PopulateBroadcastFields(FmsSyncResultDto result)
        {
            var trio = ResolveCurrentZfwTrio();
            // Report the resolved values that would be / have been written
            // to the FMS, not the live datarefs — otherwise the toast
            // shows different numbers than what actually got synced.
            result.MaczfwResolvedPercent = trio.MaczfwPercent;
            result.MaczfwResolvedError = IsOutOfRange(trio.MaczfwPercent);
            result.ZfwKg = trio.ZfwKg;
            result.MaczfwPercent = trio.MaczfwPercent;
        }

        protected virtual void BroadcastResult(FmsSyncResultDto result)
        {
            try
            {
                _app?.WebSocketHandler?.BroadcastFmsSync(result);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
