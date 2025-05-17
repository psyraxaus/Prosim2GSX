using CefSharp;
using CefSharp.OffScreen;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.Services.Audio;
using Prosim2GSX.Services.Logging;
using Prosim2GSX.Services.Logging.Implementation;
using Prosim2GSX.Services.Logging.Interfaces;
using Prosim2GSX.Services.Logging.Options;
using Prosim2GSX.Services.Logging.Provider;
using Prosim2GSX.Themes;
using Serilog;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;

namespace Prosim2GSX
{
    public partial class App : Application
    {
        private ServiceModel Model;
        private ServiceController Controller;
        private TaskbarIcon notifyIcon;
        private ServiceProvider _serviceProvider;
        private ILoggerFactory _loggerFactory;
        private ILogger<App> _logger;

        public static string ConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Prosim2GSX\Prosim2GSX.config";
        public static string AppDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Prosim2GSX\bin";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Configure services with DI container
                var services = new ServiceCollection();

                // Add basic services but WITHOUT UI logging yet
                services.AddLogging(builder =>
                {
                    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                    builder.AddConsole();
                    builder.AddDebug();

                    // Do NOT add UI logger provider here to avoid circular dependency
                });

                // Build service provider to get initial LoggerFactory
                var initialServiceProvider = services.BuildServiceProvider();

                // Get logger factory for global access - this factory does not have UI logging yet
                _loggerFactory = initialServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();

                // Create a default logger for App class
                _logger = _loggerFactory.CreateLogger<App>();

                _logger.LogInformation("Prosim2GSX application starting");

                if (Process.GetProcessesByName("Prosim2GSX").Length > 1)
                {
                    _logger.LogCritical("Prosim2GSX is already running!");
                    MessageBox.Show("Prosim2GSX is already running!", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }

                Directory.SetCurrentDirectory(AppDir);

                if (!File.Exists(ConfigFile))
                {
                    _logger.LogInformation("Config file not found, creating new one at {ConfigFile}", ConfigFile);

                    ConfigFile = Directory.GetCurrentDirectory() + @"\Prosim2GSX.config";
                    if (!File.Exists(ConfigFile))
                    {
                        XmlDocument xmlDoc = new XmlDocument();

                        XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                        xmlDoc.AppendChild(xmlDeclaration);

                        XmlElement appSettings = xmlDoc.CreateElement("appSettings");
                        xmlDoc.AppendChild(appSettings);

                        xmlDoc.Save(ConfigFile);
                        _logger.LogInformation("Created new config file");
                    }
                }

                // Create the service model
                var serviceModel = new ServiceModel();
                Model = serviceModel;

                // Initialize Serilog with settings from the model
                InitLog();

                // ============== MANUAL SETUP OF UI LOGGING TO AVOID CIRCULAR DEPENDENCY ==============

                // Create UI logging options with model settings
                var uiLoggerOptions = new Services.Logging.Options.UiLoggerOptions
                {
                    MaxLogEntries = 1000,
                    ShowTimestamps = true,
                    ShowLogLevels = true,
                    ShowDebugMessages = serviceModel.GetSettingBool("showDebugInfo", false)
                };

                // Create UiLogListener manually, not through DI
                var uiLogListener = new Services.Logging.Implementation.UiLogListener(
                    Microsoft.Extensions.Options.Options.Create(uiLoggerOptions),
                    _loggerFactory.CreateLogger<Services.Logging.Implementation.UiLogListener>());

                // Register it as a singleton in the ServiceLocator
                ServiceLocator.RegisterService<Services.Logging.Interfaces.IUiLogListener>(uiLogListener);

                // Now create UILoggerProvider manually using our already created UiLogListener
                var uiLoggerProvider = new Services.Logging.Provider.UiLoggerProvider(
                    uiLogListener,
                    Microsoft.Extensions.Options.Options.Create(uiLoggerOptions));

                // Add the provider to the logger factory manually AFTER UiLogListener is created
                _loggerFactory.AddProvider(uiLoggerProvider);

                // ============== END OF MANUAL LOGGING SETUP ==============

                // First check for SimBrief ID
                if (serviceModel.SimBriefID == "0")
                {
                    _logger.LogInformation("SimBrief ID not set, showing first-time setup dialog");

                    // Show the first-time setup dialog
                    var setupDialog = new FirstTimeSetupDialog(serviceModel);
                    bool? result = setupDialog.ShowDialog();

                    // If the user cancels, exit the application
                    if (result != true)
                    {
                        _logger.LogInformation("User cancelled first-time setup. Exiting application.");
                        Current.Shutdown();
                        return;
                    }

                    // At this point, the user has entered a valid SimBrief ID
                    _logger.LogInformation("User entered SimBrief ID: {SimBriefID}", serviceModel.SimBriefID);
                }

                // Now check for external dependencies configuration
                bool needsExternalDependenciesConfig = false;

                // Check if we need custom ProsimSDK.dll (if it's not in the expected location)
                if (!File.Exists(Path.Combine(AppDir, "lib", "ProSimSDK.dll")) &&
                    string.IsNullOrEmpty(serviceModel.ProsimSDKPath))
                {
                    needsExternalDependenciesConfig = true;
                    _logger.LogWarning("ProSimSDK.dll not found in default location and custom path not set");
                }

                // Check if we need custom VoicemeeterRemote64.dll (if it's not in the expected location)
                if (!File.Exists(Path.Combine(AppDir, "VoicemeeterRemote64.dll")) &&
                    string.IsNullOrEmpty(serviceModel.VoicemeeterDllPath))
                {
                    needsExternalDependenciesConfig = true;
                    _logger.LogWarning("VoicemeeterRemote64.dll not found in default location and custom path not set");
                }

                if (needsExternalDependenciesConfig)
                {
                    _logger.LogInformation("External dependencies need configuration, showing dialog");

                    // Show the external dependencies dialog
                    var dependenciesDialog = new ExternalDependenciesDialog(serviceModel);
                    bool? result = dependenciesDialog.ShowDialog();

                    // If the user cancels, show a warning but continue
                    if (result != true)
                    {
                        _logger.LogWarning("User cancelled external dependencies setup. Some features may not work correctly.");
                        MessageBox.Show(
                            "External dependencies have not been configured. Some features related to Prosim and Voicemeeter may not work correctly.",
                            "Warning",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                    else
                    {
                        _logger.LogInformation("User configured external dependencies.");
                    }
                }

                // Only proceed with normal initialization if we have a valid SimBrief ID
                if (serviceModel.IsValidSimbriefId())
                {
                    InitSystray();
                    InitCef();

                    // Set up DLL search paths if they have been configured
                    if (!string.IsNullOrEmpty(serviceModel.ProsimSDKPath) && File.Exists(serviceModel.ProsimSDKPath))
                    {
                        string prosimDir = Path.GetDirectoryName(serviceModel.ProsimSDKPath);
                        if (!string.IsNullOrEmpty(prosimDir))
                        {
                            // Add ProsimSDK directory to DLL search path
                            Services.DllLoader.AddDllDirectory(prosimDir);
                            _logger.LogInformation("Added ProsimSDK directory to DLL search path: {ProsimDir}", prosimDir);
                        }
                    }

                    try
                    {
                        _logger.LogInformation("Initializing ServiceLocator");

                        // Note: We're passing our configured _loggerFactory which already has UI logging set up
                        ServiceLocator.Initialize(_loggerFactory, serviceModel);

                        _logger.LogInformation("ServiceLocator initialized successfully");

                        // Initialize theme manager AFTER ServiceLocator is initialized
                        ThemeManager.Instance.SetServiceModel(serviceModel);
                        ThemeManager.Instance.Initialize();
                    }

                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Failed to initialize ServiceLocator");
                        MessageBox.Show(
                            $"Failed to initialize core services:\n\n{ex.Message}\n\nApplication will now exit.",
                            "Critical Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Current.Shutdown();
                        return;
                    }

                    // Then create the controller that uses ServiceLocator
                    Controller = new ServiceController(
                        serviceModel,
                        _loggerFactory.CreateLogger<ServiceController>());

                    // Store the ServiceController in IPCManager for access from other components
                    IPCManager.ServiceController = Controller;

                    // Start the controller in a background task with exception handling
                    Task.Run(() => {
                        try
                        {
                            Controller.Run();
                        }
                        catch (Exception ex)
                        {
                            // Log the exception
                            _logger.LogCritical(ex, "Critical exception in Controller.Run");
                        }
                    });

                    var timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };
                    timer.Tick += OnTick;
                    timer.Start();

                    MainWindow = new MainWindow(
                        notifyIcon.DataContext as NotifyIconViewModel,
                        serviceModel,
                        _loggerFactory);
                }
                else
                {
                    // This should never happen, but just in case
                    _logger.LogCritical("Invalid SimBrief ID after setup. Exiting application.");
                    MessageBox.Show(
                        "Invalid SimBrief ID. Please restart the application and enter a valid ID.",
                        "Critical Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                // Log the exception (falling back to Console if logger isn't available)
                if (_logger != null)
                {
                    _logger.LogCritical(ex, "Critical exception during application startup");
                }
                else
                {
                    Console.Error.WriteLine($"Critical exception during application startup: {ex}");
                }

                // Show a message box with the error
                MessageBox.Show(
                    $"A critical error occurred during application startup:\n\n{ex.Message}\n\nPlease check the log file for more details.",
                    "Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Shutdown the application
                Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _logger?.LogInformation("Prosim2GSX exiting...");

            Model.CancellationRequested = true;

            // Ensure VoiceMeeter connection is closed
            if (Model.AudioApiType == AudioApiType.VoiceMeeter &&
                Controller != null &&
                Controller is ServiceController serviceController)
            {
                // Try to access the AudioService through the ServiceController
                var audioService = serviceController.GetAudioService();
                if (audioService != null)
                {
                    // Call Dispose to ensure VoiceMeeter connection is closed
                    audioService.Dispose();
                }
            }

            notifyIcon?.Dispose();
            Cef.Shutdown();

            // Dispose of services
            _serviceProvider?.Dispose();

            base.OnExit(e);
        }

        protected void OnTick(object sender, EventArgs e)
        {
            if (Model.ServiceExited)
            {
                Current.Shutdown();
            }
        }

        // In App.xaml.cs, update InitLog method
        protected void InitLog()
        {
            string logFilePath = @"..\log\" + Model.GetSetting("logFilePath");
            string logLevel = Model.GetSetting("logLevel");

            _logger.LogInformation("Initializing logging system with path: {LogPath}, level: {LogLevel}",
                logFilePath, logLevel);

            // First, clear any existing Serilog logger
            Serilog.Log.CloseAndFlush();

            // Create a configuration that only writes to the file (not to console)
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration().WriteTo.File(logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 3,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message} {NewLine}{Exception}");

            // Set minimum level based on configuration
            switch (logLevel.ToUpper())
            {
                case "VERBOSE":
                case "TRACE":
                    loggerConfiguration = loggerConfiguration.MinimumLevel.Verbose();
                    break;
                case "DEBUG":
                    loggerConfiguration = loggerConfiguration.MinimumLevel.Debug();
                    break;
                case "INFO":
                case "INFORMATION":
                    loggerConfiguration = loggerConfiguration.MinimumLevel.Information();
                    break;
                case "WARN":
                case "WARNING":
                    loggerConfiguration = loggerConfiguration.MinimumLevel.Warning();
                    break;
                case "ERROR":
                    loggerConfiguration = loggerConfiguration.MinimumLevel.Error();
                    break;
                case "FATAL":
                    loggerConfiguration = loggerConfiguration.MinimumLevel.Fatal();
                    break;
                default:
                    loggerConfiguration = loggerConfiguration.MinimumLevel.Information();
                    break;
            }

            // Create the logger and set it as the default
            var logger = loggerConfiguration.CreateLogger();
            Serilog.Log.Logger = logger;

            // Now attach this to our logger factory, but REMOVE any existing Serilog providers first
            if (_loggerFactory is Microsoft.Extensions.Logging.LoggerFactory factory)
            {
                // We can't directly remove providers, so we'll create a new factory with the same providers
                // but replace any existing Serilog provider with our new one

                // First, add Serilog to our existing factory
                _loggerFactory.AddSerilog(logger, dispose: false);
            }
        }


        protected void InitSystray()
        {
            _logger.LogInformation("Creating SysTray Icon");
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            notifyIcon.Icon = GetIcon("logo.ico");
            notifyIcon.ForceCreate(false);
        }

        protected void InitCef()
        {
            _logger.LogInformation("Initializing Cef Browser");
            var settings = new CefSettings();
            if (Cef.IsInitialized != true)  // Handle nullable boolean from newer CEF version
                Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
        }

        public Icon GetIcon(string filename)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Prosim2GSX.{filename}");
            return new Icon(stream);
        }
    }
}
