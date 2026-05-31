using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Prosim2GSX.GSX.Menu
{
    /// <summary>
    /// Phase 5 diagnostic-only helper. Parses GSX Pro v4.0.0 pushback-direction
    /// menu entries (e.g. "Facing SW on Taxi AV", "Facing 225° on Taxi AV")
    /// into a compass bearing in degrees. Used by
    /// <see cref="GsxMenu.OnPushbackDirection"/>'s instrumentation to record
    /// what bearing the live menu offered, without altering the matching
    /// algorithm. A future refactor (deferred — needs captures from 2-3 more
    /// airports per the Phase 1 archaeology) can lean on the same parser.
    /// </summary>
    internal static class PushbackCompassParser
    {
        /// <summary>16-point compass: token → bearing in degrees (0 = N, 90 = E, 180 = S, 270 = W).</summary>
        private static readonly Dictionary<string, double> CompassTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            ["N"] = 0,     ["NNE"] = 22.5,  ["NE"] = 45,    ["ENE"] = 67.5,
            ["E"] = 90,    ["ESE"] = 112.5, ["SE"] = 135,   ["SSE"] = 157.5,
            ["S"] = 180,   ["SSW"] = 202.5, ["SW"] = 225,   ["WSW"] = 247.5,
            ["W"] = 270,   ["WNW"] = 292.5, ["NW"] = 315,   ["NNW"] = 337.5,
        };

        // "Facing <TOKEN>" — extracts the all-letter compass token after "Facing".
        private static readonly Regex CompassRegex = new(
            @"\bFacing\s+([A-Z]+)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // "Facing 225°" / "Facing 225" — numeric heading variant for forward-compat.
        private static readonly Regex NumericRegex = new(
            @"\bFacing\s+(\d+(?:\.\d+)?)\s*°?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public readonly struct Result
        {
            public string Token { get; }
            public double Degrees { get; }
            public bool IsNumeric { get; }

            public Result(string token, double degrees, bool isNumeric)
            {
                Token = token;
                Degrees = degrees;
                IsNumeric = isNumeric;
            }
        }

        /// <summary>
        /// Attempts to parse a menu entry into a compass bearing. Returns
        /// <c>null</c> when no recognised compass / numeric token is present
        /// — non-directional entries ("Straight pushback (manual stop, max
        /// 100 m)", "QuickEdit Pushback", "Reposition Aircraft", etc.) all
        /// resolve to <c>null</c>.
        /// </summary>
        public static Result? TryParse(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // Try the named-compass form first (the common v4.0.0 case captured at EFHK).
            var compassMatch = CompassRegex.Match(line);
            if (compassMatch.Success)
            {
                var token = compassMatch.Groups[1].Value.ToUpperInvariant();
                if (CompassTokens.TryGetValue(token, out var deg))
                    return new Result(token, deg, isNumeric: false);
            }

            // Numeric heading — flagged separately so the diagnostic log shows
            // GSX migrated its labels (currently unobserved but cheap to handle).
            var numericMatch = NumericRegex.Match(line);
            if (numericMatch.Success
                && double.TryParse(numericMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numDeg))
            {
                // Normalise into [0, 360).
                while (numDeg < 0) numDeg += 360;
                while (numDeg >= 360) numDeg -= 360;
                return new Result(numDeg.ToString("F0", CultureInfo.InvariantCulture) + "°", numDeg, isNumeric: true);
            }

            return null;
        }
    }
}
