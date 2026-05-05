using CFIT.AppLogger;
using Prosim2GSX.Web.Contracts;
using ProsimInterface;
using System;
using System.Collections.Generic;

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
        private readonly AppService _app;

        public MactowValidationService(AppService app)
        {
            _app = app;
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
                    $"written=[{string.Join(",", written)}] failed=[{string.Join(",", failed)}]");

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
