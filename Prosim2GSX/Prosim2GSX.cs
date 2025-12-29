using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CFIT.AppFramework;
using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Prosim;
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
                
                // Attempt to load the SDK assembly (non-blocking, will be verified during service init)
                LoadProSimSdkAssembly(Config.ProSimSdkPath);

                // Initialize the ProSim SDK Service early
                if (!InitializeProSimSdkService())
                {
                    Logger.Warning("ProSim SDK service initialization had issues, but continuing with startup");
                    // We continue anyway as the SDK might connect later when ProSim starts
                }
            }

            // Continue with normal startup sequence
            base.OnStartup(e);
        }

        /// <summary>
        /// Initialize the ProSim SDK Service before other services
        /// </summary>
        private bool InitializeProSimSdkService()
        {
            try
            {
                Logger.Information("Initializing ProSim SDK Service...");
                
                // Create the service (it's created in AppService.CreateServiceControllers but we need it earlier)
                // We'll create a temporary instance for early initialization
                var sdkService = new ProsimSdkService(Config);
                
                // Initialize the service (this verifies SDK types are accessible)
                var initTask = sdkService.Initialize();
                initTask.Wait(); // Blocking wait is acceptable during startup
                
                if (!initTask.Result)
                {
                    Logger.Error("ProSim SDK Service initialization failed");
                    return false;
                }

                Logger.Information("ProSim SDK Service initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during ProSim SDK Service initialization");
                Logger.LogException(ex);
                return false;
            }
        }

        private void LoadProSimSdkAssembly(string sdkPath)
        {
            try
            {
                Logger.Information("Preloading ProSim SDK assembly...");

                // Add SDK directory to PATH environment variable for native dependencies
                string sdkDirectory = Path.GetDirectoryName(sdkPath);
                if (!string.IsNullOrEmpty(sdkDirectory))
                {
                    string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                    if (!currentPath.Contains(sdkDirectory))
                    {
                        Environment.SetEnvironmentVariable("PATH", currentPath + ";" + sdkDirectory);
                        Logger.Debug($"Added SDK directory to PATH: {sdkDirectory}");
                    }
                }

                // Try to preload the assembly - this helps ensure it's available
                // The actual verification will happen in ProsimSdkService.Initialize()
                try
                {
                    var loadedAssembly = Assembly.LoadFrom(sdkPath);
                    Logger.Information($"ProSim SDK assembly preloaded: {loadedAssembly.FullName}");
                }
                catch (FileLoadException)
                {
                    // Assembly might already be loaded, this is fine
                    Logger.Debug("SDK assembly may already be loaded");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error preloading ProSim SDK assembly - will attempt to load on demand");
                Logger.LogException(ex);
                // Don't fail here - the assembly resolver will handle it when needed
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
