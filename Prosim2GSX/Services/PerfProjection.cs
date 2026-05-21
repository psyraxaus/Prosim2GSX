using Prosim2GSX.State;
using ProsimInterface.Performance;
using System.Collections.Generic;
using System.Globalization;

namespace Prosim2GSX.Services
{
    // Shared projection helpers between the takeoff + landing perf services.
    // SDK DTOs carry the full gateway wire-shape (string-typed Dt, optional
    // intersection arrays, fields neither panel uses). We project them once
    // into the lean state-side option types.
    internal static class PerfProjection
    {
        public static List<RunwayOption> ProjectRunways(IEnumerable<RunwayResponse> source)
        {
            var result = new List<RunwayOption>();
            if (source == null) return result;

            foreach (var r in source)
            {
                if (r == null || string.IsNullOrEmpty(r.RunwayId)) continue;
                var opt = new RunwayOption
                {
                    RunwayId = r.RunwayId,
                    LengthFt = r.LengthFt ?? 0,
                    DtFt     = ParseIntLoose(r.Dt),
                    Qdm      = ParseRunwayQdm(r.RunwayId),
                };
                if (r.Intersections != null)
                {
                    foreach (var i in r.Intersections)
                    {
                        if (i == null || string.IsNullOrEmpty(i.Name)) continue;
                        opt.Intersections.Add(new RunwayIntersectionOption
                        {
                            Name    = i.Name,
                            ToraFt  = i.ToraFt ?? 0,
                        });
                    }
                }
                result.Add(opt);
            }
            return result;
        }

        // RunwayId "DDR" — heading × 10 (e.g. "27L" → 270). Suffix ignored.
        public static int ParseRunwayQdm(string runwayId)
        {
            if (string.IsNullOrEmpty(runwayId) || runwayId.Length < 2) return 0;
            return int.TryParse(runwayId.Substring(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                ? n * 10
                : 0;
        }

        // Gateway returns Dt (displaced threshold) as a string. Loose parse —
        // empty / "Null" / unparseable → 0.
        public static int ParseIntLoose(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
        }
    }
}
