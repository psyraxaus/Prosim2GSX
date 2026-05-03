using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Product;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Installer
{
    public class Config : ConfigBase
    {
        public override string ProductName { get { return "Prosim2GSX"; } }
        public override string ProductExePath { get { return Path.Combine(ProductPath, "bin", ProductExe); } }
        public virtual string InstallerExtractDir { get { return Path.Combine(ProductPath, "bin"); } }

        public static readonly string OptionResetConfiguration = "ResetConfiguration";
        public static readonly string OptionRemoveMobiflight = "RemoveMobiflight";
        public static readonly string StateRemoveMobiAllowed = "RemoveMobiAllowed";
        public static readonly string OptionProSimSdkPath = "ProSimSdkPath";
        public static readonly string StateProSimSdkAutoDetected = "ProSimSdkAutoDetected";
        public static readonly string OptionEnableVoiceMeeter = "EnableVoiceMeeter";
        public static readonly string OptionVoiceMeeterDllPath = "VoiceMeeterDllPath";
        public static readonly string StateVoiceMeeterAutoDetected = "VoiceMeeterAutoDetected";
        // True when the existing AppConfig.json already shows VoiceMeeter on
        // AND a valid DLL path. UPDATE installs in this state skip the
        // VoiceMeeter section entirely so we don't re-prompt on every update.
        public static readonly string StateVoiceMeeterAlreadyConfigured = "VoiceMeeterAlreadyConfigured";
        public static readonly string OptionOverwriteGSXProfiles = "OverwriteGSXProfiles";
        public static readonly string StateGSXProfilesExist = "GSXProfilesExist";

        private static readonly string ProSimSdkFileName = "ProSimSDK.dll";

        //Worker: .NET
        public virtual bool NetRuntimeDesktop { get; set; } = true;
        public virtual string NetVersion { get; set; } = "10.0.1";
        public virtual bool CheckMajorEqual { get; set; } = true;
        public virtual string NetUrl { get; set; } = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.1/windowsdesktop-runtime-10.0.1-win-x64.exe";
        public virtual string NetInstaller { get; set; } = "windowsdesktop-runtime-10.0.1-win-x64.exe";

        public override void CheckInstallerOptions()
        {
            base.CheckInstallerOptions();

            //ResetConfig
            SetOption(OptionResetConfiguration, false);

            //Removal of Mobi Flight
            SetOption(OptionRemoveMobiflight, false);
            if (MobiInstalled() || PilotsdeckInstalled())
                SetOption(StateRemoveMobiAllowed, false);
            else
                SetOption(StateRemoveMobiAllowed, true);

            //GSX Profiles
            SetOption(OptionOverwriteGSXProfiles, false);
            SetOption(StateGSXProfilesExist, GSXProfilesExist());

            // ProSim SDK Path - try auto-detect, then check existing config
            string detectedPath = DetectProSimSdkPath();
            bool autoDetected = !string.IsNullOrEmpty(detectedPath);
            SetOption(StateProSimSdkAutoDetected, autoDetected);
            SetOption(OptionProSimSdkPath, detectedPath ?? "");

            // VoiceMeeter integration (optional). On UPDATE installs, prefer
            // values from the existing AppConfig.json so the user isn't
            // re-prompted; on fresh INSTALLs, auto-detect VoicemeeterRemote64.dll
            // under the standard VB-Audio install location and prefill.
            var (existingEnabled, existingPath) = GetVoiceMeeterFromExistingConfig();
            bool alreadyConfigured = existingEnabled && !string.IsNullOrEmpty(existingPath);
            SetOption(StateVoiceMeeterAlreadyConfigured, alreadyConfigured);

            string voiceMeeterPath = !string.IsNullOrEmpty(existingPath)
                ? existingPath
                : DetectVoiceMeeterDllPath();
            SetOption(OptionEnableVoiceMeeter, existingEnabled);
            SetOption(StateVoiceMeeterAutoDetected, !string.IsNullOrEmpty(voiceMeeterPath));
            SetOption(OptionVoiceMeeterDllPath, voiceMeeterPath ?? "");
        }

        // Returns (UseVoiceMeeter, VoiceMeeterDllPath) from the existing app
        // config, or (false, null) if missing / unparseable. Same lightweight
        // string-extraction approach as GetSdkPathFromExistingConfig — avoids
        // pulling in the full Prosim2GSX config type from the installer.
        private static (bool enabled, string path) GetVoiceMeeterFromExistingConfig()
        {
            try
            {
                string configPath = Path.Combine(Sys.FolderAppDataRoaming(), "Prosim2GSX", "AppConfig.json");
                if (!File.Exists(configPath)) return (false, null);
                string json = File.ReadAllText(configPath);

                bool enabled = ExtractBool(json, "UseVoiceMeeter") ?? false;
                string path = ExtractString(json, "VoiceMeeterDllPath");
                if (!string.IsNullOrEmpty(path) && !File.Exists(path))
                    path = null;
                return (enabled, path);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return (false, null);
            }
        }

        private static string ExtractString(string json, string key)
        {
            string marker = $"\"{key}\"";
            int idx = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            int colon = json.IndexOf(':', idx + marker.Length);
            if (colon < 0) return null;
            int q1 = json.IndexOf('"', colon + 1);
            int q2 = q1 < 0 ? -1 : json.IndexOf('"', q1 + 1);
            if (q1 < 0 || q2 < 0) return null;
            return json.Substring(q1 + 1, q2 - q1 - 1).Replace("\\\\", "\\");
        }

        private static bool? ExtractBool(string json, string key)
        {
            string marker = $"\"{key}\"";
            int idx = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            int colon = json.IndexOf(':', idx + marker.Length);
            if (colon < 0) return null;
            int after = colon + 1;
            while (after < json.Length && char.IsWhiteSpace(json[after])) after++;
            if (after + 4 <= json.Length && json.Substring(after, 4).Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (after + 5 <= json.Length && json.Substring(after, 5).Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            return null;
        }

        /// <summary>
        /// Best-effort detection of VoicemeeterRemote64.dll. Returns null if
        /// VoiceMeeter doesn't appear to be installed; user can browse to the
        /// file manually on the config page if so.
        /// </summary>
        public static string DetectVoiceMeeterDllPath()
        {
            const string DllName = "VoicemeeterRemote64.dll";
            string[] candidates = new[]
            {
                @"C:\Program Files (x86)\VB\Voicemeeter",
                @"C:\Program Files\VB\Voicemeeter",
            };
            foreach (var dir in candidates)
            {
                try
                {
                    string p = Path.Combine(dir, DllName);
                    if (File.Exists(p)) return p;
                }
                catch (Exception ex) { Logger.LogException(ex); }
            }
            return null;
        }

        /// <summary>
        /// Attempts to auto-detect the ProSim SDK path by searching common locations,
        /// the existing app config, the registry, and running ProSim processes.
        /// </summary>
        public static string DetectProSimSdkPath()
        {
            // 1. Check existing app config file for a previously configured path
            string existingPath = GetSdkPathFromExistingConfig();
            if (!string.IsNullOrEmpty(existingPath))
            {
                Logger.Information($"ProSim SDK found in existing config: {existingPath}");
                return existingPath;
            }

            // 2. Check running ProSim process to find install directory
            string processPath = GetSdkPathFromRunningProcess();
            if (!string.IsNullOrEmpty(processPath))
            {
                Logger.Information($"ProSim SDK found via running process: {processPath}");
                return processPath;
            }

            // 3. Search common installation directories
            string commonPath = GetSdkPathFromCommonLocations();
            if (!string.IsNullOrEmpty(commonPath))
            {
                Logger.Information($"ProSim SDK found in common location: {commonPath}");
                return commonPath;
            }

            // 4. Check registry for ProSim install path
            string registryPath = GetSdkPathFromRegistry();
            if (!string.IsNullOrEmpty(registryPath))
            {
                Logger.Information($"ProSim SDK found via registry: {registryPath}");
                return registryPath;
            }

            Logger.Warning("ProSim SDK could not be auto-detected");
            return null;
        }

        private static string GetSdkPathFromExistingConfig()
        {
            try
            {
                string configPath = Path.Combine(Sys.FolderAppDataRoaming(), "Prosim2GSX", "AppConfig.json");
                if (!File.Exists(configPath))
                    return null;

                string json = File.ReadAllText(configPath);
                // Simple extraction — avoid pulling in the full app config type
                string marker = "\"ProSimSdkPath\"";
                int idx = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    return null;

                int colonIdx = json.IndexOf(':', idx + marker.Length);
                if (colonIdx < 0)
                    return null;

                int quoteStart = json.IndexOf('"', colonIdx + 1);
                int quoteEnd = json.IndexOf('"', quoteStart + 1);
                if (quoteStart < 0 || quoteEnd < 0)
                    return null;

                string path = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1)
                    .Replace("\\\\", "\\");

                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    return path;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return null;
        }

        private static string GetSdkPathFromRunningProcess()
        {
            try
            {
                var process = Process.GetProcessesByName("ProSimA322-System").FirstOrDefault();
                if (process != null)
                {
                    string processDir = Path.GetDirectoryName(process.MainModule?.FileName);
                    if (!string.IsNullOrEmpty(processDir))
                    {
                        string sdkPath = Path.Combine(processDir, ProSimSdkFileName);
                        if (File.Exists(sdkPath))
                            return sdkPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return null;
        }

        private static string GetSdkPathFromCommonLocations()
        {
            var searchPaths = new List<string>
            {
                @"C:\ProSim-AR",
                @"C:\ProSim",
                @"C:\Program Files\ProSim",
                @"C:\Program Files (x86)\ProSim",
                @"C:\Program Files\ProSim-AR",
                @"C:\Program Files (x86)\ProSim-AR",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ProSim-AR"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ProSim"),
            };

            // Also check all drive roots
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
            {
                searchPaths.Add(Path.Combine(drive.RootDirectory.FullName, "ProSim-AR"));
                searchPaths.Add(Path.Combine(drive.RootDirectory.FullName, "ProSim"));
            }

            // Deduplicate
            var searched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string dir in searchPaths)
            {
                if (!searched.Add(dir))
                    continue;

                try
                {
                    string sdkPath = Path.Combine(dir, ProSimSdkFileName);
                    if (File.Exists(sdkPath))
                        return sdkPath;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }

            return null;
        }

        private static string GetSdkPathFromRegistry()
        {
            try
            {
                // Try common registry locations where ProSim might register
                string[] registryPaths = new[]
                {
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\ProSim-AR",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\ProSim-AR",
                    @"HKEY_CURRENT_USER\SOFTWARE\ProSim-AR",
                };

                foreach (string regPath in registryPaths)
                {
                    string installDir = Sys.GetRegistryValue<string>(regPath, "InstallDir");
                    if (!string.IsNullOrEmpty(installDir))
                    {
                        string sdkPath = Path.Combine(installDir, ProSimSdkFileName);
                        if (File.Exists(sdkPath))
                            return sdkPath;
                    }

                    // Also try "InstallPath" key name
                    installDir = Sys.GetRegistryValue<string>(regPath, "InstallPath");
                    if (!string.IsNullOrEmpty(installDir))
                    {
                        string sdkPath = Path.Combine(installDir, ProSimSdkFileName);
                        if (File.Exists(sdkPath))
                            return sdkPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return null;
        }

        public static bool GSXProfilesExist()
        {
            try
            {
                string airplanesDir = Path.Combine(Sys.FolderAppDataRoaming(), "Virtuali", "Airplanes");
                string[] profileNames = { "prosim-a322-cfm", "prosim-a322-iae", "Prosim-a322-neo" };

                foreach (string profile in profileNames)
                {
                    if (Directory.Exists(Path.Combine(airplanesDir, profile)))
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return false;
        }

        public static bool MobiInstalled()
        {
            bool result;
            try
            {
                result = !string.IsNullOrWhiteSpace(Sys.GetRegistryValue<string>(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MobiFlight Connector", "UninstallString"));
            }
            catch (Exception ex)
            {
                result = false;
                Logger.LogException(ex);                
            }
            return result;
        }

        public static bool PilotsdeckInstalled()
        {
            bool result;
            try
            {
                result = File.Exists(Path.Combine(Sys.FolderAppDataRoaming(), @"Elgato\StreamDeck\Plugins\com.extension.pilotsdeck.sdPlugin\PilotsDeck.exe"));
            }
            catch (Exception ex)
            {
                result = false;
                Logger.LogException(ex);
            }
            return result;
        }
    }
}
