using System;
using System.IO;
using System.Reflection;
using System.Windows;
using CFIT.AppFramework;
using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using Prosim2GSX.UI;
using Prosim2GSX.UI.Dialogs;
using Prosim2GSX.UI.NotifyIcon;

namespace Prosim2GSX
{
    public class Prosim2GSX(Type windowType) : SimApp<Prosim2GSX, AppService, Config, Definition>(windowType, typeof(NotifyIconModelExt))
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                var app = new Prosim2GSX(typeof(AppWindow));
                return app.Start(args);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return -1;
            }
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Check if ProSimSdkPath is configured and valid
            bool sdkPathValid = !string.IsNullOrEmpty(Config.ProSimSdkPath) && File.Exists(Config.ProSimSdkPath);

            if (!sdkPathValid)
            {
                Logger.Warning($"ProSimSdkPath not configured or invalid: '{Config.ProSimSdkPath}'");
                Logger.Information("Showing ProSim SDK configuration dialog...");

                // Show dialog to configure the SDK path
                var dialog = new ProSimSdkDialog(Config);
                bool? result = dialog.ShowDialog();

                if (result == true && !string.IsNullOrEmpty(Config.ProSimSdkPath) && File.Exists(Config.ProSimSdkPath))
                {
                    Logger.Information($"ProSimSdkPath configured by user: {Config.ProSimSdkPath}");

                    // SDK path has been configured - app needs to restart to properly load the assembly
                    Logger.Information("ProSim SDK path configured. Application restart required.");
                    MessageBox.Show(
                        "ProSim SDK path has been configured successfully.\n\nPlease restart the application to load the SDK.",
                        "Restart Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    RequestShutdown(0);
                    return;
                }
                else
                {
                    Logger.Error("ProSimSdkPath configuration cancelled or invalid. Application cannot continue.");
                    MessageBox.Show(
                        "ProSim SDK is required for this application. The application will now close.",
                        "Configuration Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    RequestShutdown(1);
                    return;
                }
            }
            else
            {
                // SDK path is valid, ensure assembly resolution is set up
                Logger.Information($"ProSimSdkPath is configured: {Config.ProSimSdkPath}");
                SetupProSimSdkAssemblyResolver(Config.ProSimSdkPath);
            }

            // Continue with normal startup sequence
            base.OnStartup(e);
        }

        private bool LoadProSimSdkAssembly(string sdkPath)
        {
            try
            {
                Logger.Information("Loading ProSimSDK assembly...");

                // Register assembly resolver first
                SetupProSimSdkAssemblyResolver(sdkPath);

                // Add SDK directory to PATH environment variable for native dependencies
                string sdkDirectory = Path.GetDirectoryName(sdkPath);
                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                if (!currentPath.Contains(sdkDirectory))
                {
                    Environment.SetEnvironmentVariable("PATH", currentPath + ";" + sdkDirectory);
                    Logger.Debug($"Added SDK directory to PATH: {sdkDirectory}");
                }

                // Load the assembly
                var loadedAssembly = Assembly.LoadFrom(sdkPath);
                Logger.Information($"ProSimSDK assembly loaded: {loadedAssembly.FullName}");

                // Verify the assembly contains expected types
                var prosimInterfaceType = loadedAssembly.GetType("ProsimInterface.ProsimAircraftInterface");
                if (prosimInterfaceType == null)
                {
                    Logger.Error("ProSimSDK assembly loaded but ProsimAircraftInterface type not found");
                    return false;
                }

                Logger.Information("ProSimSDK assembly verified successfully");
                return true;
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error($"ProSimSDK file not found: {ex.FileName}");
                Logger.LogException(ex);
                return false;
            }
            catch (BadImageFormatException ex)
            {
                Logger.Error("ProSimSDK assembly has incorrect format (possibly wrong architecture)");
                Logger.LogException(ex);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error loading ProSimSDK assembly");
                Logger.LogException(ex);
                return false;
            }
        }

        private void SetupProSimSdkAssemblyResolver(string sdkPath)
        {
            string sdkDirectory = Path.GetDirectoryName(sdkPath);
            
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                // Only handle ProSimSDK-related assemblies
                if (args.Name.Contains("ProSimSDK") || args.Name.Contains("ProsimInterface"))
                {
                    Logger.Debug($"Resolving assembly: {args.Name} from {sdkPath}");
                    
                    try
                    {
                        // Try the configured SDK path first
                        if (File.Exists(sdkPath))
                        {
                            return Assembly.LoadFrom(sdkPath);
                        }

                        // Try looking in the SDK directory for the assembly
                        string assemblyName = args.Name.Split(',')[0];
                        string assemblyPath = Path.Combine(sdkDirectory, assemblyName + ".dll");
                        
                        if (File.Exists(assemblyPath))
                        {
                            Logger.Debug($"Found assembly at: {assemblyPath}");
                            return Assembly.LoadFrom(assemblyPath);
                        }

                        Logger.Warning($"Could not resolve assembly: {args.Name}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error resolving assembly {args.Name}: {ex.Message}");
                    }
                }
                
                return null;
            };

            Logger.Debug("ProSimSDK assembly resolver registered");
        }

        protected override void InitAppWindow()
        {
            base.InitAppWindow();
            AppContext.SetSwitch("Switch.System.Windows.Controls.Grid.StarDefinitionsCanExceedAvailableSpace", true);
        }
    }
}
