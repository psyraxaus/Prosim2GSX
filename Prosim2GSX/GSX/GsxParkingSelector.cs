using CFIT.AppLogger;
using Prosim2GSX.GSX.Menu;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
        // Gate-letter parent entry for two-level airports (e.g. EHAM Schiphol):
        // the "Select Position at <airport>" page lists "Gate A (N suitable parkings)" /
        // "Gate D (36 suitable parkings)" etc., and drilling into one opens
        // "All Gate D positions" with the specific stand entries. The regex
        // matches "Gate <LETTERS>" followed by optional whitespace and an
        // opening paren — the paren is the discriminator that rules out
        // "Gate D 18 - Heavy" (specific stand) and "Gates W34-W48" (apron range).
        protected static readonly Regex GateLetterParentRegex = new(@"\bGATE\s+([A-Z]+)\s*\(", RegexOptions.Compiled);
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
            // Phase 5 diagnostic state — captured into gate-selection-* rows
            // alongside the existing main-log output. The matching logic below
            // is byte-for-byte unchanged; only outcome-string tracking and
            // per-page diagnostic emission were added.
            var stopwatch = Stopwatch.StartNew();
            int pagesTraversed = 0;
            string outcome = "unknown";
            LogStartDiagnostic(gate);

            try
            {
                var target = Normalise(gate);
                if (string.IsNullOrEmpty(target))
                {
                    Logger.Warning($"GsxParkingSelector: empty/invalid gate '{gate}' — skipping");
                    outcome = "aborted: empty or invalid gate input";
                    return false;
                }

                if (!Controller.IsGsxRunning)
                {
                    Logger.Warning("GsxParkingSelector: GSX not running — skipping");
                    outcome = "aborted: GSX not running";
                    return false;
                }

                var arrivalIcao = Normalise(Controller.AircraftInterface?.FmsDestination ?? "");

                try
                {
                    if (!await Menu.Open(waitReady: true))
                    {
                        Logger.Warning("GsxParkingSelector: menu did not become ready");
                        outcome = "aborted: menu did not become ready";
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
                            outcome = $"aborted: GSX menu did not advance from root '{rootTitle}' after Select(10)";
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
                        pagesTraversed++;
                        var lines = Menu.MenuLines;
                        // Defensive snapshot for diagnostic emission — the live
                        // list will mutate when we Select(next) below.
                        var pageTitle = Menu.MenuTitle;
                        var pageLinesSnapshot = new List<string>(lines);

                        if (lines.Count == 0)
                        {
                            Logger.Warning("GsxParkingSelector: menu has no lines — aborting");
                            LogPageDiagnostic(pagesTraversed, step, pageTitle, pageLinesSnapshot, -1, "n/a", "page has no entries — aborting");
                            outcome = $"aborted: empty page at depth {step}";
                            Menu.Hide();
                            return false;
                        }

                        var impliedPrefix = ImpliedApronPrefix(Menu.MenuTitle);

                        // Always try exact gate match first — covers the case where we've drilled
                        // down to the stand list and the target stand is now visible.
                        int chosen = FindExactGateIndex(lines, target, impliedPrefix);
                        string strategy = "exact";
                        if (chosen >= 0)
                        {
                            Logger.Information($"GsxParkingSelector: exact gate '{gate}' matched at row {chosen + 1}: '{lines[chosen]}'");
                            LogPageDiagnostic(pagesTraversed, step, pageTitle, pageLinesSnapshot, chosen, strategy, $"matched gate '{gate}' — final");
                            await Menu.Select(chosen + 1);
                            Menu.Hide();
                            outcome = $"matched: page {pagesTraversed}, row {chosen + 1}, '{lines[chosen]}', strategy 'exact'";
                            return true;
                        }

                        // Airport selection submenu — pick the row containing the arrival ICAO,
                        // fall back to row 2 (GSX convention: arrival is the second entry).
                        if (Menu.MatchTitle(AirportSelectTitle))
                        {
                            chosen = !string.IsNullOrEmpty(arrivalIcao) ? FindIcaoIndex(lines, arrivalIcao) : -1;
                            if (chosen >= 0)
                                strategy = "icao";
                            if (chosen < 0 && lines.Count >= 2)
                            {
                                Logger.Debug($"GsxParkingSelector: arrival ICAO '{arrivalIcao}' not found in airport list — falling back to row 2");
                                chosen = 1;
                                strategy = "airport-fallback-row2";
                            }
                        }

                        // Apron group / range row, e.g. "Apron 1W (Gates W34-W48)" containing target W34.
                        if (chosen < 0)
                        {
                            chosen = FindRangeMatchIndex(lines, target);
                            if (chosen >= 0)
                                strategy = "range";
                        }

                        // Two-level airport layout (Schiphol convention): the
                        // "Select Position at <airport>" page lists "Gate A
                        // (N suitable parkings)" / "Gate D (36 suitable
                        // parkings)" parent entries, and the actual stands
                        // live one level deeper under "All Gate D positions".
                        // When the target's letter prefix matches a parent
                        // letter on this page, drill into it.
                        if (chosen < 0)
                        {
                            var (targetPrefix, _, _) = ParseGateId(target);
                            chosen = FindGateLetterParentIndex(lines, targetPrefix);
                            if (chosen >= 0)
                                strategy = "gate-letter-parent";
                        }

                        if (chosen < 0)
                        {
                            // No drill-down match. If the page has a "Next Page" entry we're on a
                            // paginated apron list — click it and stay at the same logical depth.
                            int nextPageIdx = FindNextPageIndex(lines);
                            if (nextPageIdx >= 0 && pageClicksAtLevel < MaxPageClicksPerLevel)
                            {
                                pageClicksAtLevel++;
                                Logger.Information($"GsxParkingSelector: gate '{gate}' not on this page — paginating (click {pageClicksAtLevel}) at row {nextPageIdx + 1}");
                                LogPageDiagnostic(pagesTraversed, step, pageTitle, pageLinesSnapshot, nextPageIdx, "next-page", $"no match — paginating (click {pageClicksAtLevel}/{MaxPageClicksPerLevel})");
                                var prevTitle = Menu.MenuTitle;
                                var prevCount = lines.Count;
                                await Menu.Select(nextPageIdx + 1);
                                if (!await WaitForMenuChange(prevTitle, prevCount, Controller.Config.MenuOpenTimeout))
                                {
                                    Logger.Warning($"GsxParkingSelector: menu did not refresh after Next Page at row {nextPageIdx + 1}");
                                    outcome = $"aborted: menu did not refresh after Next Page at depth {step}";
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
                            LogPageDiagnostic(pagesTraversed, step, pageTitle, pageLinesSnapshot, -1, "none", $"no match and no next-page (or page-click budget exhausted at {pageClicksAtLevel}/{MaxPageClicksPerLevel})");
                            outcome = $"not-found: traversed {pagesTraversed} pages, no match for '{gate}' at depth {step}";
                            Menu.Hide();
                            return false;
                        }

                        // Drill-down — reset the per-level page counter.
                        pageClicksAtLevel = 0;

                        Logger.Information($"GsxParkingSelector: drilling into row {chosen + 1} (depth {step}): '{lines[chosen]}'");
                        LogPageDiagnostic(pagesTraversed, step, pageTitle, pageLinesSnapshot, chosen, strategy, $"drilling into row {chosen + 1}");
                        var prevTitleDrill = Menu.MenuTitle;
                        var prevCountDrill = lines.Count;
                        await Menu.Select(chosen + 1);
                        if (!await WaitForMenuChange(prevTitleDrill, prevCountDrill, Controller.Config.MenuOpenTimeout))
                        {
                            Logger.Warning($"GsxParkingSelector: menu did not refresh after selecting row {chosen + 1} (still '{Menu.MenuTitle}')");
                            outcome = $"aborted: menu did not refresh after drill into row {chosen + 1} at depth {step}";
                            Menu.Hide();
                            return false;
                        }
                    }

                    Logger.Warning($"GsxParkingSelector: exceeded max navigation depth ({MaxNavigationDepth}) without finding gate '{gate}'");
                    outcome = $"aborted: exceeded max navigation depth ({MaxNavigationDepth}) without finding '{gate}'";
                    Menu.Hide();
                    return false;
                }
                catch (Exception ex)
                {
                    if (ex is not TaskCanceledException)
                        Logger.LogException(ex);
                    outcome = ex is TaskCanceledException
                        ? "aborted: cancelled"
                        : $"error: {ex.GetType().Name}: {ex.Message}";
                    return false;
                }
            }
            finally
            {
                LogResultDiagnostic(outcome, stopwatch.Elapsed, pagesTraversed);
            }
        }

        private void LogStartDiagnostic(string gate)
        {
            try
            {
                var diag = Controller?.GsxMenuDiagnosticLog;
                if (diag == null) return;
                var sb = new StringBuilder();
                sb.AppendLine("Gate selection invocation");
                sb.Append("  OFP gate (input): '").Append(gate ?? "<null>").Append("'").AppendLine();
                sb.Append("  Arrival ICAO: '").Append(Controller?.AircraftInterface?.FmsDestination ?? "<unknown>").Append("'").AppendLine();
                sb.Append("  GSX running: ").AppendLine(Controller?.IsGsxRunning == true ? "yes" : "no");
                diag.LogDiagnostic("gate-selection-start", sb.ToString());
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        private void LogPageDiagnostic(int pageNumber, int depthStep, string title, IReadOnlyList<string> lines, int chosenIndex, string strategy, string outcomeForPage)
        {
            try
            {
                var diag = Controller?.GsxMenuDiagnosticLog;
                if (diag == null) return;
                var sb = new StringBuilder();
                sb.Append("Gate selection page ").Append(pageNumber).Append(" (depth=").Append(depthStep).Append(")").AppendLine();
                sb.Append("  Title: '").Append(title ?? "<null>").Append("'").AppendLine();
                sb.Append("  Entries (").Append(lines?.Count ?? 0).Append("):").AppendLine();
                if (lines != null)
                    for (int i = 0; i < lines.Count; i++)
                        sb.Append("    [").Append(i + 1).Append("] ").AppendLine(lines[i]);
                if (chosenIndex >= 0)
                    sb.Append("  Chosen: row ").Append(chosenIndex + 1)
                      .Append(" via strategy '").Append(strategy ?? "<null>").Append("'").AppendLine();
                else
                    sb.AppendLine("  Chosen: none");
                sb.Append("  Outcome: ").AppendLine(outcomeForPage ?? "<null>");
                diag.LogDiagnostic("gate-selection-page", sb.ToString());
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        private void LogResultDiagnostic(string outcome, TimeSpan elapsed, int pagesTraversed)
        {
            try
            {
                var diag = Controller?.GsxMenuDiagnosticLog;
                if (diag == null) return;
                var sb = new StringBuilder();
                sb.AppendLine("Gate selection result");
                sb.Append("  Pages traversed: ").Append(pagesTraversed).AppendLine();
                sb.Append("  Elapsed: ").Append((int)elapsed.TotalMilliseconds).AppendLine("ms");
                sb.Append("  Outcome: ").AppendLine(outcome ?? "<null>");
                diag.LogDiagnostic("gate-selection-result", sb.ToString());
            }
            catch (Exception ex) { Logger.LogException(ex); }
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

        /// <summary>
        /// Finds a "Gate &lt;LETTER&gt; (N suitable parkings)" parent entry whose
        /// letter prefix matches <paramref name="targetPrefix"/>. Used for
        /// two-level airport navigation (Schiphol-style) where the position
        /// list is split into a per-letter parent menu and a per-stand child
        /// menu. Returns -1 when no parent matches or <paramref name="targetPrefix"/>
        /// is empty.
        /// </summary>
        protected virtual int FindGateLetterParentIndex(IReadOnlyList<string> lines, string targetPrefix)
        {
            if (string.IsNullOrEmpty(targetPrefix)) return -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var upper = (lines[i] ?? "").ToUpperInvariant();
                // Skip page-nav rows.
                if (upper.Contains(NextPageToken) || upper.Contains(PreviousPageToken) || upper.StartsWith(BackToken))
                    continue;
                var m = GateLetterParentRegex.Match(upper);
                if (m.Success && m.Groups[1].Value == targetPrefix)
                    return i;
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
