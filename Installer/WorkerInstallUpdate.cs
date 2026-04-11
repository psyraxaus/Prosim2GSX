using CFIT.AppLogger;
using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
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

            // Write the ProSim SDK path into the app config file
            WriteProSimSdkPathToConfig();

            return Directory.Exists(logDir);
        }

        /// <summary>
        /// Reads the deployed AppConfig.json, injects the ProSimSdkPath value
        /// selected by the user during installation, and rewrites the file.
        /// </summary>
        private void WriteProSimSdkPathToConfig()
        {
            string sdkPath = Config.GetOption<string>(Config.OptionProSimSdkPath);
            if (string.IsNullOrEmpty(sdkPath))
            {
                Logger.Warning("No ProSim SDK path was configured during installation");
                return;
            }

            try
            {
                string configPath = Config.ProductConfigPath;
                if (!File.Exists(configPath))
                {
                    Logger.Error($"Config file not found at '{configPath}', cannot write SDK path");
                    return;
                }

                string json = File.ReadAllText(configPath);
                var jsonNode = JsonNode.Parse(json);
                if (jsonNode is JsonObject jsonObj)
                {
                    jsonObj["ProSimSdkPath"] = sdkPath;

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string updatedJson = jsonObj.ToJsonString(options);
                    File.WriteAllText(configPath, updatedJson);

                    Logger.Information($"ProSim SDK path written to config: {sdkPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to write ProSim SDK path to config file");
                Logger.LogException(ex);
            }
        }
    }
}
