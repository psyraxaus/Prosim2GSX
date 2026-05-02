using CFIT.AppLogger;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Prosim2GSX.Web
{
    // Keeps gsx_handler.py's PROSIM2GSX_PORT line in sync with
    // Config.WebServerPort. The Installer drops the script with the default
    // port baked in (5001); when a user picks a different port via App
    // Settings, the in-sim Stackless Python handler would otherwise still
    // hit the old endpoint. This rewrites the line in every installed
    // profile's handler whenever the port changes (or on app startup).
    internal static class GsxHandlerSync
    {
        private const string HandlerFileName = "gsx_handler.py";
        private const string PortLinePrefix = "PROSIM2GSX_PORT = ";
        private static readonly Regex PortLineRegex = new(@"^PROSIM2GSX_PORT\s*=\s*\d+", RegexOptions.Multiline);

        public static void EnsurePort(int port)
        {
            string root;
            try
            {
                root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Virtuali", "Airplanes");
            }
            catch (Exception ex)
            {
                Logger.Warning("GsxHandlerSync: cannot resolve %appdata% path");
                Logger.LogException(ex);
                return;
            }

            if (!Directory.Exists(root))
                return;

            int rewritten = 0;
            foreach (var profileDir in Directory.GetDirectories(root))
            {
                string path = Path.Combine(profileDir, HandlerFileName);
                if (!File.Exists(path)) continue;
                if (RewriteIfNeeded(path, port)) rewritten++;
            }

            if (rewritten > 0)
                Logger.Information($"GsxHandlerSync: PROSIM2GSX_PORT set to {port} in {rewritten} profile(s)");
        }

        private static bool RewriteIfNeeded(string path, int port)
        {
            try
            {
                string text = File.ReadAllText(path);
                var match = PortLineRegex.Match(text);
                if (!match.Success) return false;

                string desired = PortLinePrefix + port.ToString(CultureInfo.InvariantCulture);
                if (match.Value == desired) return false;

                string updated = text.Substring(0, match.Index) + desired + text.Substring(match.Index + match.Length);
                File.WriteAllText(path, updated);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"GsxHandlerSync: failed to rewrite '{path}'");
                Logger.LogException(ex);
                return false;
            }
        }
    }
}
