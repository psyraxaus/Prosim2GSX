using CFIT.AppLogger;
using Prosim2GSX.State;
using Prosim2GSX.Web.Contracts;
using ProsimInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Prosim2GSX.Services
{
    // Resolves the "current" MACTOW value, validates it against the A320
    // envelope, and writes the FMS init datarefs on demand.
    //
    // Resolution chain (final → prelim → computed) mirrors how the EFB
    // surfaces MACTOW through the flight workflow: the final loadsheet is
    // authoritative once issued, the prelim is the planned value before
    // boarding completes, and the live W&B mirror (MACGW until a separate
    // take-off computation lands) is the last-resort fallback so the panel
    // always shows something usable.
    //
    // Validation bounds are sourced from LoadsheetState.MinMacTow /
    // MaxMacTow (10.5 / 45.0 — the same constants the loadsheet parser
    // already applies to PrelimMacTow / FinalMacTow). Keeping a single
    // source of truth means a future variant change touches one place.
    //
    // The FMS sync writes three FMS init datarefs:
    //   aircraft.fms.init.zfw    ← WeightBalance.ZfwKg
    //   aircraft.fms.init.zfwcg  ← WeightBalance.MaczfwPercent
    //   aircraft.fms.init.block  ← ceil(WeightBalance.FuelPlannedKg / 100) * 100
    // The block-fuel field is rounded UP to the nearest 100 kg per
    // operational convention (FMS block entries are typically whole hectos).
    //
    // THS / stab-trim is NOT written. The only writable trim dataref in
    // ProsimDataref.csv is aircraft.fms.perf.takeOff.ths, and the EFB's
    // own main.<hash>.js binding for that path has setValue:()=>{} with no
    // derivation logic (confirmed by user inspection). Guessing a THS value
    // would violate the dataref-first principle, so the field is reported
    // as Skipped in the result rather than written or silently ignored.
    public class MactowValidationService
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
        private double _lastSyncedMacTow;
        private double _lastSyncedZfwKg;
        private double _lastSyncedMaczfwPercent;
        private double _lastSyncedBlockKg;
        private string _lastSyncedSource = "";

        // Auto-sync edge tracking. Re-fires when FinalReceivedAt advances
        // (covers the resend case which writes a fresh timestamp). Null
        // sentinel = no auto-sync fired yet for the current final slot.
        private DateTime? _autoSyncFiredForFinalAt;
        private LoadsheetState _attachedLoadsheet;

        public MactowValidationService(AppService app)
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
            _lastSyncedMacTow = 0;
            _lastSyncedZfwKg = 0;
            _lastSyncedMaczfwPercent = 0;
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

        // Called from WeightBalanceService.Tick AFTER MactowPercent /
        // MacTowSource have been written. Compares the current resolved
        // values against the last-sync snapshot and sets FmsSyncStale.
        // No-op when never synced.
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
                bool sourceUpgraded =
                    string.Equals(_lastSyncedSource, "prelim", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(wb.MacTowSource, "final", StringComparison.OrdinalIgnoreCase);

                bool drift =
                    Math.Abs(wb.MactowPercent - _lastSyncedMacTow) > StaleThresholdMacPercent
                    || Math.Abs(wb.ZfwKg - _lastSyncedZfwKg) > StaleThresholdZfwKg
                    || Math.Abs(wb.MaczfwPercent - _lastSyncedMaczfwPercent) > StaleThresholdMacPercent
                    || Math.Abs(currentBlock - _lastSyncedBlockKg) > StaleThresholdBlockKg;

                wb.FmsSyncStale = sourceUpgraded || drift;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Resolve the current MACTOW. Source order: FinalStatus="received"
        // → PrelimStatus="received" → live computed value on
        // WeightBalanceState.MactowPercent (currently mirrors MACGW). The
        // out param is informational — useful for diagnostics and the WS
        // payload — but not part of the validation logic.
        public virtual (double MacTow, string Source) ResolveCurrentMacTow()
        {
            var ls = _app?.Loadsheet;
            if (ls != null)
            {
                if (string.Equals(ls.FinalStatus, "received", StringComparison.OrdinalIgnoreCase) && ls.FinalMacTow > 0)
                    return (ls.FinalMacTow, "final");
                if (string.Equals(ls.PrelimStatus, "received", StringComparison.OrdinalIgnoreCase) && ls.PrelimMacTow > 0)
                    return (ls.PrelimMacTow, "prelim");
            }

            var wb = _app?.WeightBalance;
            return (wb?.MactowPercent ?? 0.0, "computed");
        }

        // Envelope check using the same bounds the loadsheet parser uses.
        // Zero/negative MACTOW counts as in-range (no data to flag) so the
        // SYNC TO FMS button isn't disabled before any source is available.
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

                if (sdk.SetDouble(ProsimConstants.RefFmsInitZfw, wb.ZfwKg))
                    written.Add("zfw");
                else
                    failed.Add("zfw");

                if (sdk.SetDouble(ProsimConstants.RefFmsInitZfwcg, wb.MaczfwPercent))
                    written.Add("zfwcg");
                else
                    failed.Add("zfwcg");

                var block = RoundBlockFuelKg(wb.FuelPlannedKg);
                if (sdk.SetDouble(ProsimConstants.RefFmsInitBlock, block))
                    written.Add("blockFuel");
                else
                    failed.Add("blockFuel");

                result.WrittenFields = written.ToArray();
                result.FailedFields = failed.ToArray();
                result.Success = failed.Count == 0 && written.Count > 0;
                if (!result.Success && string.IsNullOrEmpty(result.ErrorMessage))
                    result.ErrorMessage = failed.Count > 0 ? $"Failed to write: {string.Join(", ", failed)}" : "No fields written";

                Logger.Information(
                    $"FMS sync: zfw={wb.ZfwKg:F0} zfwcg={wb.MaczfwPercent:F2} block={block:F0} " +
                    $"source={wb.MacTowSource} " +
                    $"written=[{string.Join(",", written)}] failed=[{string.Join(",", failed)}]");

                if (result.Success)
                {
                    _lastSyncedAt = result.Timestamp;
                    _lastSyncedMacTow = wb.MactowPercent;
                    _lastSyncedZfwKg = wb.ZfwKg;
                    _lastSyncedMaczfwPercent = wb.MaczfwPercent;
                    _lastSyncedBlockKg = block;
                    _lastSyncedSource = wb.MacTowSource ?? "";
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
            var wb = _app?.WeightBalance;
            var (mac, _) = ResolveCurrentMacTow();
            result.MacTow = mac;
            result.MacTowError = IsOutOfRange(mac);
            result.ZfwKg = wb?.ZfwKg ?? 0;
            result.MaczfwPercent = wb?.MaczfwPercent ?? 0;
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
