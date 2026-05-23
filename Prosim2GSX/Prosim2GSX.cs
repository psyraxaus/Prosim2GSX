using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using CFIT.AppFramework;
using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Diagnostics;
using Prosim2GSX.Themes;
using Prosim2GSX.UI;
using Prosim2GSX.UI.NotifyIcon;

namespace Prosim2GSX
{
    public class Prosim2GSX(Type windowType) : SimApp<Prosim2GSX, AppService, Config, Definition>(windowType, typeof(NotifyIconModelExt))
    {
        /// <summary>
        /// Indicates whether the ProSim SDK was successfully loaded at startup.
        /// When false, the app runs in degraded mode — UI is accessible (especially Settings)
        /// but ProSim integration is disabled.
        /// </summary>
        public bool IsSdkAvailable { get; internal set; } = false;

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
        protected override void OnStartup(StartupEventArgs e)
        {
            // Check if ProSimSdkPath is configured and valid
            bool sdkPathValid = !string.IsNullOrEmpty(Config.ProSimSdkPath) && File.Exists(Config.ProSimSdkPath);

            if (sdkPathValid)
            {
                // SDK path is valid, set up assembly resolution and load
                Logger.Information($"ProSimSdkPath is configured: {Config.ProSimSdkPath}");
                SetupProSimSdkAssemblyResolver(Config.ProSimSdkPath);
                LoadProSimSdkAssembly(Config.ProSimSdkPath);
                IsSdkAvailable = true;

                Logger.Information("ProSim SDK loaded successfully");
            }
            else
            {
                // SDK path missing or invalid — launch in degraded mode
                Logger.Warning($"ProSimSdkPath not configured or invalid: '{Config.ProSimSdkPath}'");
                Logger.Information("Launching in degraded mode — ProSim integration disabled. Configure the SDK path in App Settings.");
                IsSdkAvailable = false;
            }

            // Initialise theme system before the window is created
            ThemeManager.Instance.SetConfig(Config);
            ThemeManager.Instance.Initialize();

            // Continue with normal startup sequence (window will show SDK banner if degraded)
            base.OnStartup(e);
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

        /// <summary>
        /// Phase 6.6 resilience override. Intercepts the specific recurring
        /// WPF dispatcher quota exception (<see cref="Win32Exception"/> with
        /// <c>NativeErrorCode == 1816</c> / <c>ERROR_NOT_ENOUGH_QUOTA</c>) from
        /// the render pipeline (<c>HwndTarget.UpdateWindowSettings → PostMessage</c>),
        /// hands it to <see cref="DispatcherQuotaHandler"/> for forensic
        /// logging, marks the dispatcher exception args as <c>Handled</c>,
        /// and returns WITHOUT calling base so CFIT's
        /// <see cref="SimApp{TApp,TAppService,TConfig,TDefinition}.UnhandledExceptionHandler"/>
        /// does NOT force-shut-down the app.
        ///
        /// <para>
        /// Per the Phase 6.5.B diagnostic findings the in-process resource
        /// state is healthy at the moment of crash (USER ~30, dispatcher
        /// pending ~3). The exception is almost certainly an external/
        /// transient OS or WPF render-subsystem failure, so the pragmatic
        /// response is to swallow the specific code, log it richly, and
        /// keep running. Every other exception flows through to
        /// <c>base.UnhandledExceptionHandler</c> unchanged — its
        /// shutdown / log / exit-code path is preserved.
        /// </para>
        ///
        /// <para>
        /// The handler MUST NEVER throw. The body is wrapped in try/catch
        /// and on any failure we fall through to <c>base</c> so the existing
        /// crash pipeline still runs.
        /// </para>
        /// </summary>
        public override void UnhandledExceptionHandler(object sender, EventArgs args)
        {
            try
            {
                if (args is DispatcherUnhandledExceptionEventArgs dispatchArgs
                    && dispatchArgs.Exception is Win32Exception win32Ex
                    && win32Ex.NativeErrorCode == DispatcherQuotaHandler.ErrorNotEnoughQuota)
                {
                    var handler = AppService.Instance?.DispatcherQuotaHandler;
                    if (handler != null)
                    {
                        try { handler.HandleQuotaException(win32Ex); }
                        catch { /* the handler is bulletproof internally; this catch is belt-and-braces */ }
                        dispatchArgs.Handled = true;
                        return;
                    }
                    // No handler yet — very early startup before AppService
                    // has constructed it. Better to crash with full
                    // diagnostics than swallow silently with no record.
                }
            }
            catch
            {
                // Recognition path itself failed (cast / property access).
                // Fall through to the existing crash handler so we don't
                // hide an unrelated failure.
            }

            base.UnhandledExceptionHandler(sender, args);
        }
    }
}
