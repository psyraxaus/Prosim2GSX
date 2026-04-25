using CFIT.Installer.Product;
using CFIT.Installer.UI.Behavior;
using CFIT.Installer.UI.Config;
using System;
using System.IO;

namespace Installer
{
    public class ConfigPage : PageConfig
    {
        public Config Config { get { return BaseConfig as Config; } }

        public override void CreateConfigItems()
        {
            // ProSim SDK path — show on fresh install, or on update if the path is missing/invalid
            bool showSdkPath = Config.Mode == SetupMode.INSTALL || !HasValidSdkPathInConfig();
            if (showSdkPath)
            {
                bool autoDetected = Config.GetOption<bool>(Config.StateProSimSdkAutoDetected);
                string description = autoDetected
                    ? "ProSim SDK was automatically detected. Verify the path below is correct, or browse to select a different location."
                    : "ProSim SDK was not detected automatically. Please browse to locate your ProSimSDK.dll file.\n\nThis file is typically found in your ProSim installation folder (e.g. C:\\ProSim-AR\\ProSimSDK.dll).";

                var sdkItem = new ConfigItemFileBrowse(
                    "ProSim SDK Location",
                    description,
                    "ProSim SDK (ProSimSDK.dll)|ProSimSDK.dll|DLL files (*.dll)|*.dll|All files (*.*)|*.*",
                    Config.OptionProSimSdkPath,
                    Config)
                {
                    DefaultFileName = "ProSimSDK.dll",
                    ValidationFunc = ValidateSdkPath
                };

                Items.Add(sdkItem);
            }

            ConfigItemHelper.CreateCheckboxDesktopLink(Config, ConfigBase.OptionDesktopLink, Items);
            ConfigItemHelper.CreateRadioAutoStart(Config, Items);
            if (Config.Mode == SetupMode.UPDATE)
                Items.Add(new ConfigItemCheckbox("Reset Configuration", "Reset App Configuration to Default (only for Troubleshooting)", Config.OptionResetConfiguration, Config));
            if (Config.GetOption<bool>(Config.StateGSXProfilesExist))
                Items.Add(new ConfigItemCheckbox("Overwrite GSX Profiles", "Existing GSX aircraft profiles were detected in %appdata%\\Virtuali\\Airplanes.\n\nCheck this option to overwrite them with the profiles included in this installation.\nIf unchecked, existing profiles will be kept.", Config.OptionOverwriteGSXProfiles, Config));
            if (Config.GetOption<bool>(Config.StateRemoveMobiAllowed))
                Items.Add(new ConfigItemCheckbox("Remove Mobiflight Module", "Remove the Mobiflight Module from MSFS' Community Folder (not required anymore for Prosim2GSX).\n\nATTENTION: Make sure no other Application is using it before removing it!\n(You will only see this Option if MobiFlight Connector and PilotsDeck are not detected as installed)", Config.OptionRemoveMobiflight, Config));
        }

        /// <summary>
        /// Checks whether the existing app config already has a valid SDK path configured.
        /// </summary>
        private bool HasValidSdkPathInConfig()
        {
            string currentPath = Config.GetOption<string>(Config.OptionProSimSdkPath);
            return !string.IsNullOrEmpty(currentPath) && File.Exists(currentPath);
        }

        /// <summary>
        /// Validation function for the SDK path.
        /// Returns null if valid, or a warning message if there's an issue.
        /// </summary>
        private static string ValidateSdkPath(string path)
        {
            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return "Warning: The selected file is not a DLL file.";

            string fileName = Path.GetFileName(path);
            if (!fileName.Equals("ProSimSDK.dll", StringComparison.OrdinalIgnoreCase))
                return $"Warning: Expected 'ProSimSDK.dll' but selected '{fileName}'. Please verify this is the correct file.";

            return null;
        }
    }
}
