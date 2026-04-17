using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Tasks;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Installer
{
    public class WorkerGSXProfiles : TaskWorker<Config>
    {
        public bool OverwriteGSXProfiles { get; set; } = false;

        private static readonly string[] ProfileDirectories = { "prosim-a322-cfm", "prosim-a322-iae", "Prosim-a322-neo" };

        public WorkerGSXProfiles(Config config) : base(config, "Install GSX Profiles", "Checking GSX Profiles ...")
        {
            SetPropertyFromOption<bool>(Config.OptionOverwriteGSXProfiles);
            Model.DisplayInSummary = true;
            Model.DisplayCompleted = true;
        }

        protected override async Task<bool> DoRun()
        {
            string sourceDir = Path.Combine(Config.InstallerExtractDir, "GSXProfiles");
            if (!Directory.Exists(sourceDir))
            {
                Model.SetError("GSX Profiles not found in installation package!");
                return false;
            }

            string targetDir = Path.Combine(Sys.FolderAppDataRoaming(), "Virtuali", "Airplanes");
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            int copied = 0;
            int skipped = 0;

            foreach (string profileDir in Directory.GetDirectories(sourceDir))
            {
                string profileName = Path.GetFileName(profileDir);
                string targetProfileDir = Path.Combine(targetDir, profileName);

                if (Directory.Exists(targetProfileDir) && !OverwriteGSXProfiles)
                {
                    Logger.Information($"GSX Profile '{profileName}' already exists, skipping");
                    skipped++;
                    continue;
                }

                Model.SetState($"Installing GSX Profile: {profileName}");

                try
                {
                    if (Directory.Exists(targetProfileDir))
                        Directory.Delete(targetProfileDir, true);

                    Directory.CreateDirectory(targetProfileDir);

                    foreach (string file in Directory.GetFiles(profileDir))
                    {
                        string destFile = Path.Combine(targetProfileDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                    }

                    copied++;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to install GSX Profile '{profileName}'");
                    Logger.LogException(ex);
                    Model.SetError($"Failed to install GSX Profile '{profileName}'");
                    return false;
                }

                await Task.Delay(150);
            }

            string message = $"GSX Profiles installed: {copied} copied";
            if (skipped > 0)
                message += $", {skipped} skipped (already exist)";
            Model.SetSuccess(message);

            return true;
        }
    }
}
