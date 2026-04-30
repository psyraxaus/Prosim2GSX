using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Tasks;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Installer
{
    // Installs gsx_handler.py into each Virtuali/Airplanes/{profile} directory.
    //
    // Lives separately from WorkerGSXProfiles because the handler script is
    // a Prosim2GSX-owned file: users who picked "don't overwrite GSX profiles"
    // (to preserve their own gsx.cfg edits) still need the handler to be
    // refreshed whenever Prosim2GSX is updated. This worker therefore always
    // copies, regardless of OptionOverwriteGSXProfiles.
    //
    // The script source is the gsx_handler.py shipped inside each profile
    // directory of the installer payload (GSXProfiles/{profile}/gsx_handler.py),
    // so a future per-variant divergence remains possible without restructuring.
    public class WorkerGSXHandler : TaskWorker<Config>
    {
        private const string HandlerFileName = "gsx_handler.py";

        public WorkerGSXHandler(Config config) : base(config, "Install GSX Handler Script", "Installing GSX handler script ...")
        {
            Model.DisplayInSummary = true;
            Model.DisplayCompleted = true;
        }

        protected override async Task<bool> DoRun()
        {
            string sourceRoot = Path.Combine(Config.InstallerExtractDir, "GSXProfiles");
            if (!Directory.Exists(sourceRoot))
            {
                // GSX profiles missing entirely is a packaging error, not
                // something this worker can recover from — surface a warning
                // and succeed so it doesn't block the rest of the install.
                Logger.Warning("GSX Profiles source dir missing — skipping handler install");
                Model.SetSuccess("GSX handler skipped (source missing)");
                return true;
            }

            string targetRoot = Path.Combine(Sys.FolderAppDataRoaming(), "Virtuali", "Airplanes");

            int written = 0;
            int missingTarget = 0;

            foreach (string sourceProfileDir in Directory.GetDirectories(sourceRoot))
            {
                string profileName = Path.GetFileName(sourceProfileDir);
                string sourceHandler = Path.Combine(sourceProfileDir, HandlerFileName);
                if (!File.Exists(sourceHandler))
                    continue;

                string targetProfileDir = Path.Combine(targetRoot, profileName);
                if (!Directory.Exists(targetProfileDir))
                {
                    // Profile not installed (e.g. user kept old version, or
                    // chose not to overwrite a missing dir). Nothing to do —
                    // when they install the profile later, the handler ships
                    // alongside it via WorkerGSXProfiles.
                    missingTarget++;
                    continue;
                }

                Model.SetState($"Writing handler: {profileName}");
                string targetHandler = Path.Combine(targetProfileDir, HandlerFileName);
                try
                {
                    File.Copy(sourceHandler, targetHandler, true);
                    written++;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to write GSX handler for profile '{profileName}'");
                    Logger.LogException(ex);
                    Model.SetError($"Failed to write GSX handler for profile '{profileName}'");
                    return false;
                }

                await Task.Delay(50);
            }

            string message = $"GSX handler installed: {written} profiles updated";
            if (missingTarget > 0)
                message += $", {missingTarget} skipped (profile not installed)";
            Model.SetSuccess(message);
            return true;
        }
    }
}
