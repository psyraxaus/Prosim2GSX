using System;
using System.Collections.Generic;

namespace Prosim2GSX.Web.Contracts
{
    // Result of a POST /api/fms/sync attempt. Returned to the caller and
    // broadcast on the WS "fmsSync" channel so every client (WPF + every
    // open browser) gets the same outcome at the same time.
    //
    // Per-field accounting:
    //   WrittenFields — datarefs the SDK accepted the write for.
    //   FailedFields  — datarefs the SDK rejected (returned false from
    //                   WriteDataRef, or threw).
    //   SkippedFields — datarefs intentionally not attempted, e.g. THS.
    //                   The Prosim EFB does not derive a THS value from
    //                   MACTOW (confirmed by inspecting main.<hash>.js —
    //                   the takeOffThs binding has setValue:()=>{} and no
    //                   calculation), so we do not guess.
    public class FmsSyncResultDto
    {
        public bool Success { get; set; }
        public string[] WrittenFields { get; set; } = Array.Empty<string>();
        public string[] FailedFields { get; set; } = Array.Empty<string>();
        public string[] SkippedFields { get; set; } = Array.Empty<string>();
        public string ErrorMessage { get; set; } = "";

        // Broadcast extras — sent on the "fmsSync" WS channel so listeners
        // can update without a follow-up REST call. MaczfwResolvedPercent
        // is the headline value that was just synced (final → prelim →
        // live), and MaczfwResolvedError is its envelope check.
        public double MaczfwResolvedPercent { get; set; }
        public bool MaczfwResolvedError { get; set; }
        public double ZfwKg { get; set; }
        public double MaczfwPercent { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
