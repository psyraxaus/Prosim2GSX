using CFIT.AppLogger;
using Prosim2GSX.GSX.Menu;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX
{
    public class GsxParkingSelector
    {
        protected static readonly Regex AlphaNumeric = new("[^A-Za-z0-9]", RegexOptions.Compiled);
        // Gate identifier: optional letter prefix + number + optional letter suffix (e.g. W34, W34A, B12, 117L)
        protected static readonly Regex GateIdRegex = new(@"^([A-Z]*)(\d+)([A-Z]*)$", RegexOptions.Compiled);
        // Range pattern in a menu line, e.g. "W34-W48" or "W34 - W48" or "W34—W48"
        protected static readonly Regex RangeRegex = new(@"\b([A-Z]*)(\d+)\s*[-‐-―]\s*([A-Z]*)(\d+)\b", RegexOptions.Compiled);
        // Bare gate identifier on a menu line. Letter prefix is optional so we
        // can match lines like "Gate 46" once we're inside an apron context
        // (where the apron implies the prefix). Requires the GATE keyword to
        // anchor the match so we don't pick up unrelated numbers in the line.
        protected static readonly Regex GateTokenRegex = new(@"\bGATE\s+([A-Z]*)(\d+)([A-Z]*)\b", RegexOptions.Compiled);
        // Extract implied apron prefix from titles like "All Apron 1W (Gates W34-W48) positions".
        protected static readonly Regex ApronPrefixRegex = new(@"APRON\s+\d+([A-Z]+)", RegexOptions.Compiled);

        protected const int MaxNavigationDepth = 6;
        protected const int MaxPageClicksPerLevel = 8;
        protected const string AirportSelectTitle = "Select airport";
        protected const string GateListTitlePrefix = "All Apron";
        protected const string NextPageToken = "NEXT PAGE";
        protected const string PreviousPageToken = "PREVIOUS PAGE";
        protected const string BackToken = "BACK";

        protected virtual GsxController Controller { get; }
        protected virtual GsxMenu Menu => Controller.Menu;

        public GsxParkingSelector(GsxController controller)
        {
            Controller = controller;
        }

        public virtual async Task<bool> ApplyAsync(string gate)
        {
            var target = Normalise(gate);
            if (string.IsNullOrEmpty(target))
            {
                Logger.Warning($"GsxParkingSelector: empty/invalid gate '{gate}' — skipping");
                return false;
            }

            if (!Controller.IsGsxRunning)
            {
                Logger.Warning("GsxParkingSelector: GSX not running — skipping");
                return false;
            }

            var arrivalIcao = Normalise(Controller.AircraftInterface?.FmsDestination ?? "");

            try
            {
                if (!await Menu.Open(waitReady: true))
                {
                    Logger.Warning("GsxParkingSelector: menu did not become ready");
                    return false;
                }

                // Pre-detect: if GSX is already showing a page in the gate-selection
                // tree (airport list, parking list, or apron's gate list), skip the
                // root-level Select(10) — that index doesn't exist on these pages
                // and the menu would silently refuse to advance.
                if (!IsAlreadyInGateTree())
                {
                    // Open the GSX gate/parking menu (item 10 = "Activate Services at" or "Select airport"
                    // depending on whether GSX considers us parked).
                    var rootTitle = Menu.MenuTitle;
                    await Menu.Select(10);
                    if (!await WaitForMenuChange(rootTitle, Menu.MenuLineCount, Controller.Config.MenuOpenTimeout))
                    {
                        Logger.Warning($"GsxParkingSelector: GSX menu did not advance from '{rootTitle}'");
                        Menu.Hide();
                        return false;
                    }
                }
                else
                {
                    Logger.Information($"GsxParkingSelector: menu already in gate tree at '{Menu.MenuTitle}' — skipping root Select(10)");
                }

                int pageClicksAtLevel = 0;

                for (int step = 0; step < MaxNavigationDepth; step++)
                {
                    var lines = Menu.MenuLines;
                    if (lines.Count == 0)
                    {
                        Logger.Warning("GsxParkingSelector: menu has no lines — aborting");
                        Menu.Hide();
                        return false;
                    }

                    var impliedPrefix = ImpliedApronPrefix(Menu.MenuTitle);

                    // Always try exact gate match first — covers the case where we've drilled
                    // down to the stand list and the target stand is now visible.
                    int chosen = FindExactGateIndex(lines, target, impliedPrefix);
                    if (chosen >= 0)
                    {
                        Logger.Information($"GsxParkingSelector: exact gate '{gate}' matched at row {chosen + 1}: '{lines[chosen]}'");
                        await Menu.Select(chosen + 1);
                        Menu.Hide();
                        return true;
                    }

                    // Airport selection submenu — pick the row containing the arrival ICAO,
                    // fall back to row 2 (GSX convention: arrival is the second entry).
                    if (Menu.MatchTitle(AirportSelectTitle))
                    {
                        chosen = !string.IsNullOrEmpty(arrivalIcao) ? FindIcaoIndex(lines, arrivalIcao) : -1;
                        if (chosen < 0 && lines.Count >= 2)
                        {
                            Logger.Debug($"GsxParkingSelector: arrival ICAO '{arrivalIcao}' not found in airport list — falling back to row 2");
                            chosen = 1;
                        }
                    }

                    // Apron group / range row, e.g. "Apron 1W (Gates W34-W48)" containing target W34.
                    if (chosen < 0)
                        chosen = FindRangeMatchIndex(lines, target);

                    if (chosen < 0)
                    {
                        // No drill-down match. If the page has a "Next Page" entry we're on a
                        // paginated apron list — click it and stay at the same logical depth.
                        int nextPageIdx = FindNextPageIndex(lines);
                        if (nextPageIdx >= 0 && pageClicksAtLevel < MaxPageClicksPerLevel)
                        {
                            pageClicksAtLevel++;
                            Logger.Information($"GsxParkingSelector: gate '{gate}' not on this page — paginating (click {pageClicksAtLevel}) at row {nextPageIdx + 1}");
                            var prevTitle = Menu.MenuTitle;
                            var prevCount = lines.Count;
                            await Menu.Select(nextPageIdx + 1);
                            if (!await WaitForMenuChange(prevTitle, prevCount, Controller.Config.MenuOpenTimeout))
                            {
                                Logger.Warning($"GsxParkingSelector: menu did not refresh after Next Page at row {nextPageIdx + 1}");
                                Menu.Hide();
                                return false;
                            }
                            // Stay at same depth — pagination doesn't drill deeper.
                            step--;
                            continue;
                        }

                        Logger.Warning($"GsxParkingSelector: gate '{gate}' not found at depth {step} — title: '{Menu.MenuTitle}', {lines.Count} lines:");
                        for (int i = 0; i < lines.Count; i++)
                            Logger.Warning($"  [{i + 1}] '{lines[i]}'");
                        Menu.Hide();
                        return false;
                    }

                    // Drill-down — reset the per-level page counter.
                    pageClicksAtLevel = 0;

                    Logger.Information($"GsxParkingSelector: drilling into row {chosen + 1} (depth {step}): '{lines[chosen]}'");
                    var prevTitleDrill = Menu.MenuTitle;
                    var prevCountDrill = lines.Count;
                    await Menu.Select(chosen + 1);
                    if (!await WaitForMenuChange(prevTitleDrill, prevCountDrill, Controller.Config.MenuOpenTimeout))
                    {
                        Logger.Warning($"GsxParkingSelector: menu did not refresh after selecting row {chosen + 1} (still '{Menu.MenuTitle}')");
                        Menu.Hide();
                        return false;
                    }
                }

                Logger.Warning($"GsxParkingSelector: exceeded max navigation depth ({MaxNavigationDepth}) without finding gate '{gate}'");
                Menu.Hide();
                return false;
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
                return false;
            }
        }

        protected virtual bool IsAlreadyInGateTree()
        {
            return Menu.MatchTitle(AirportSelectTitle)
                || Menu.MatchTitle(GsxConstants.MenuParkingSelect)
                || Menu.MatchTitle(GateListTitlePrefix);
        }

        protected virtual async Task<bool> WaitForMenuChange(string previousTitle, int previousLineCount, int timeoutMs)
        {
            int waited = 0;
            int interval = Math.Max(50, Controller.Config.MenuCheckInterval);
            while (waited < timeoutMs)
            {
                await Task.Delay(interval, Controller.RequestToken);
                waited += interval;
                if (Menu.MenuTitle != previousTitle || Menu.MenuLineCount != previousLineCount)
                    return true;
            }
            return false;
        }

        protected virtual int FindIcaoIndex(IReadOnlyList<string> lines, string normalisedIcao)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var line = Normalise(lines[i]);
                if (line.Contains(normalisedIcao, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        protected virtual int FindNextPageIndex(IReadOnlyList<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var upper = (lines[i] ?? "").ToUpperInvariant();
                // Match "Next Page" but not "Previous Page" — both contain "PAGE".
                if (upper.Contains(NextPageToken) && !upper.Contains(PreviousPageToken))
                    return i;
            }
            return -1;
        }

        protected virtual int FindExactGateIndex(IReadOnlyList<string> lines, string normalisedTarget, string impliedPrefix)
        {
            var (targetPrefix, targetNumber, targetSuffix) = ParseGateId(normalisedTarget);
            if (targetNumber < 0)
                return -1;

            for (int i = 0; i < lines.Count; i++)
            {
                var upper = lines[i].ToUpperInvariant();
                // Skip page-nav rows so they aren't accidentally treated as gates.
                if (upper.Contains(NextPageToken) || upper.Contains(PreviousPageToken) || upper.StartsWith(BackToken))
                    continue;

                foreach (Match m in GateTokenRegex.Matches(upper))
                {
                    var prefix = m.Groups[1].Value;
                    var number = int.Parse(m.Groups[2].Value);
                    var suffix = m.Groups[3].Value;

                    // If the menu line shows a bare-number gate (no letter prefix on the token),
                    // treat the apron-implied prefix as the line's effective prefix. This matches
                    // GSX's convention of dropping the apron letter inside an apron's gate list
                    // (e.g. "Gate 46" inside Apron 1W means W46).
                    if (string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(impliedPrefix))
                        prefix = impliedPrefix;

                    if (prefix == targetPrefix && number == targetNumber && suffix == targetSuffix)
                        return i;
                }
            }
            return -1;
        }

        protected virtual int FindRangeMatchIndex(IReadOnlyList<string> lines, string normalisedTarget)
        {
            var (targetPrefix, targetNumber, _) = ParseGateId(normalisedTarget);
            if (targetNumber < 0)
                return -1;

            int bestIndex = -1;
            int bestSpan = int.MaxValue;
            for (int i = 0; i < lines.Count; i++)
            {
                foreach (Match m in RangeRegex.Matches(lines[i].ToUpperInvariant()))
                {
                    var startPrefix = m.Groups[1].Value;
                    var startNum = int.Parse(m.Groups[2].Value);
                    var endPrefix = m.Groups[3].Value;
                    var endNum = int.Parse(m.Groups[4].Value);
                    if (string.IsNullOrEmpty(endPrefix)) endPrefix = startPrefix;
                    // Skip ranges whose prefix doesn't match
                    if (startPrefix != targetPrefix || endPrefix != targetPrefix) continue;
                    var lo = Math.Min(startNum, endNum);
                    var hi = Math.Max(startNum, endNum);
                    if (targetNumber < lo || targetNumber > hi) continue;
                    var span = hi - lo;
                    if (span < bestSpan)
                    {
                        bestSpan = span;
                        bestIndex = i;
                    }
                }
            }
            return bestIndex;
        }

        protected static string ImpliedApronPrefix(string menuTitle)
        {
            if (string.IsNullOrEmpty(menuTitle))
                return "";
            var m = ApronPrefixRegex.Match(menuTitle.ToUpperInvariant());
            return m.Success ? m.Groups[1].Value : "";
        }

        protected static (string prefix, int number, string suffix) ParseGateId(string normalised)
        {
            if (string.IsNullOrEmpty(normalised)) return ("", -1, "");
            var m = GateIdRegex.Match(normalised);
            if (!m.Success) return ("", -1, "");
            return (m.Groups[1].Value, int.Parse(m.Groups[2].Value), m.Groups[3].Value);
        }

        protected static string Normalise(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return AlphaNumeric.Replace(s, "").ToUpperInvariant();
        }
    }
}
