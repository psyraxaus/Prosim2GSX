using CFIT.AppLogger;
using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
using CFIT.Installer.Product;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace Installer
{
    public class WorkerInstallUpdate : WorkerAppInstall<Config>
    {
        public bool ResetConfiguration { get; set; } = false;

        public WorkerInstallUpdate(Config config) : base(config)
        {
            SetPropertyFromOption<bool>(Config.OptionResetConfiguration);
        }

        protected override void CreateFileExclusions()
        {

        }

        protected override bool DeleteOldFiles()
        {
            FuncIO.DeleteDirectory(Path.Combine(Config.ProductPath, "log"), true, true);
            FuncIO.DeleteDirectory(InstallerExtractDir, true, true);

            if (File.Exists(Config.ProductConfigPath) && ResetConfiguration)
            {
                Logger.Debug($"Deleting Config File '{Config.ProductConfigPath}'");
                FuncIO.DeleteFile(Config.ProductConfigPath);
            }

            return Directory.Exists(InstallerExtractDir);
        }

        protected override bool CreateDefaultConfig()
        {
            string configDir = Path.GetDirectoryName(Config.ProductConfigPath);
            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);

            using (var stream = GetAppConfig())
            {
                var confStream = File.Create(Config.ProductConfigPath);
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(confStream);
                confStream.Flush(true);
                confStream.Close();
            }
            Thread.Sleep(250);
            return Config.HasConfigFile;
        }

        protected override bool FinalizeSetup()
        {
            string logDir = Path.Combine(Config.ProductPath, "log");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            // Write user-selected paths into the app config file.
            WriteUserPathsToConfig();

            return Directory.Exists(logDir);
        }

        /// <summary>
        /// Reads the deployed AppConfig.json, injects user-selected paths
        /// from the install-time configuration, and rewrites the file.
        /// Currently handles ProSimSdkPath, plus the VoiceMeeter integration
        /// toggle and DLL path.
        /// </summary>
        private void WriteUserPathsToConfig()
        {
            try
            {
                string configPath = Config.ProductConfigPath;
                if (!File.Exists(configPath))
                {
                    Logger.Error($"Config file not found at '{configPath}', cannot write user paths");
                    return;
                }

                string json = File.ReadAllText(configPath);
                var jsonNode = JsonNode.Parse(json);
                if (jsonNode is not JsonObject jsonObj) return;

                bool changed = false;

                string sdkPath = Config.GetOption<string>(Config.OptionProSimSdkPath);
                if (!string.IsNullOrEmpty(sdkPath))
                {
                    jsonObj["ProSimSdkPath"] = sdkPath;
                    Logger.Information($"ProSim SDK path written to config: {sdkPath}");
                    changed = true;
                }
                else
                {
                    Logger.Warning("No ProSim SDK path was configured during installation");
                }

                // Only write VoiceMeeter keys when the section was actually
                // shown to the user this run. UPDATE installs that detected
                // an existing valid configuration suppress the prompt — and
                // must therefore leave the existing keys untouched.
                bool vmAlreadyConfigured = Config.GetOption<bool>(Config.StateVoiceMeeterAlreadyConfigured);
                bool vmPromptShown = Config.Mode == SetupMode.INSTALL || !vmAlreadyConfigured;
                if (vmPromptShown)
                {
                    bool enableVm = Config.GetOption<bool>(Config.OptionEnableVoiceMeeter);
                    string vmPath = Config.GetOption<string>(Config.OptionVoiceMeeterDllPath) ?? "";
                    jsonObj["UseVoiceMeeter"] = enableVm;
                    jsonObj["VoiceMeeterDllPath"] = vmPath;
                    Logger.Information($"VoiceMeeter integration: enabled={enableVm}, dll='{vmPath}'");
                    changed = true;
                }
                else
                {
                    Logger.Information("VoiceMeeter integration: keeping existing AppConfig.json values (already configured).");
                }

                if (changed)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(configPath, jsonObj.ToJsonString(options));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to write user paths to config file");
                Logger.LogException(ex);
            }
        }
    }
}
