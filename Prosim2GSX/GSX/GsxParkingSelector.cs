using CFIT.AppLogger;
using Prosim2GSX.GSX.Menu;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX
{
    public class GsxParkingSelector
    {
        protected static readonly Regex AlphaNumeric = new("[^A-Za-z0-9]", RegexOptions.Compiled);
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

            try
            {
                if (!await Menu.Open(waitReady: true))
                {
                    Logger.Warning("GsxParkingSelector: menu did not become ready");
                    return false;
                }

                await Menu.Select(10);
                if (!await WaitForParkingMenu(Controller.Config.MenuOpenTimeout))
                {
                    Logger.Warning($"GsxParkingSelector: parking submenu did not appear (current title: '{Menu.MenuTitle}')");
                    Menu.Hide();
                    return false;
                }

                var index = FindMatchingGateIndex(target);
                if (index < 0)
                {
                    Logger.Warning($"GsxParkingSelector: gate '{gate}' not found in current parking list ({Menu.MenuLineCount} lines)");
                    Menu.Hide();
                    return false;
                }

                Logger.Information($"GsxParkingSelector: selecting gate '{gate}' at row {index + 1}: '{Menu.MenuLines[index]}'");
                await Menu.Select(index + 1);
                Menu.Hide();
                return true;
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
                return false;
            }
        }

        protected virtual async Task<bool> WaitForParkingMenu(int timeoutMs)
        {
            int waited = 0;
            int interval = Math.Max(50, Controller.Config.MenuCheckInterval);
            while (waited < timeoutMs)
            {
                if (Menu.MatchTitle(GsxConstants.MenuParkingSelect) || Menu.MatchTitle(GsxConstants.MenuParkingChange))
                    return true;
                await Task.Delay(interval, Controller.RequestToken);
                waited += interval;
            }
            return false;
        }

        protected virtual int FindMatchingGateIndex(string normalisedTarget)
        {
            for (int i = 0; i < Menu.MenuLines.Count; i++)
            {
                var line = Normalise(Menu.MenuLines[i]);
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.StartsWith(normalisedTarget, StringComparison.Ordinal)
                    || line.Contains("GATE" + normalisedTarget, StringComparison.Ordinal)
                    || line.Contains("STAND" + normalisedTarget, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        protected static string Normalise(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return AlphaNumeric.Replace(s, "").ToUpperInvariant();
        }
    }
}
