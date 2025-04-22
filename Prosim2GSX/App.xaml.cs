using CefSharp;
using CefSharp.OffScreen;
using H.NotifyIcon;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.Services.Audio;
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

        public static string ConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Prosim2GSX\Prosim2GSX.config";
        public static string AppDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Prosim2GSX\bin";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Process.GetProcessesByName("Prosim2GSX").Length > 1)
            {
                MessageBox.Show("Prosim2GSX is already running!", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            Directory.SetCurrentDirectory(AppDir);

            if (!File.Exists(ConfigFile))
            {
                ConfigFile = Directory.GetCurrentDirectory() + @"\Prosim2GSX.config";
                if (!File.Exists(ConfigFile))
                {
                    XmlDocument xmlDoc = new XmlDocument();

                    XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                    xmlDoc.AppendChild(xmlDeclaration);

                    XmlElement appSettings = xmlDoc.CreateElement("appSettings");
                    xmlDoc.AppendChild(appSettings);

                    xmlDoc.Save(ConfigFile);
                }
            }

            try
            {
                // Create the service model
                var serviceModel = new ServiceModel();
                Model = serviceModel;

                // Check for default SimBrief ID (0) before proceeding
                if (serviceModel.SimBriefID == "0")
                {
                    // Show the first-time setup dialog
                    var setupDialog = new FirstTimeSetupDialog(serviceModel);
                    bool? result = setupDialog.ShowDialog();
                    
                    // If the user cancels, exit the application
                    if (result != true)
                    {
                        Logger.Log(LogLevel.Information, "App:OnStartup", 
                            "User cancelled first-time setup. Exiting application.");
                        Current.Shutdown();
                        return;
                    }
                    
                    // At this point, the user has entered a valid SimBrief ID
                    Logger.Log(LogLevel.Information, "App:OnStartup", 
                        $"User entered SimBrief ID: {serviceModel.SimBriefID}");
                }
                
                // Only proceed with normal initialization if we have a valid SimBrief ID
                if (serviceModel.IsValidSimbriefId())
                {
                    InitLog();
                    InitSystray();
                    InitCef();
                    
                    // Initialize theme manager
                    ThemeManager.Instance.SetServiceModel(serviceModel);
                    ThemeManager.Instance.Initialize();

                    try
                    {
                        Logger.Log(LogLevel.Information, "App:OnStartup", "Initializing ServiceLocator");
                        ServiceLocator.Initialize(serviceModel);
                        Logger.Log(LogLevel.Information, "App:OnStartup", "ServiceLocator initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Critical, "App:OnStartup", $"Failed to initialize ServiceLocator: {ex.Message}\n{ex.StackTrace}");
                        MessageBox.Show(
                            $"Failed to initialize core services:\n\n{ex.Message}\n\nApplication will now exit.",
                            "Critical Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Current.Shutdown();
                        return;
                    }

                    // Then create the controller that uses ServiceLocator
                    Controller = new ServiceController(serviceModel);

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
                            Logger.Log(LogLevel.Critical, "App:OnStartup", 
                                $"Critical exception in Controller.Run: {ex.GetType()} - {ex.Message}\n{ex.StackTrace}");
                        }
                    });

                    var timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };
                    timer.Tick += OnTick;
                    timer.Start();

                    MainWindow = new MainWindow(notifyIcon.DataContext as NotifyIconViewModel, serviceModel);
                }
                else
                {
                    // This should never happen, but just in case
                    Logger.Log(LogLevel.Critical, "App:OnStartup", 
                        "Invalid SimBrief ID after setup. Exiting application.");
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
                // Log the exception
                Logger.Log(LogLevel.Critical, "App:OnStartup", 
                    $"Critical exception during application startup: {ex.GetType()} - {ex.Message}\n{ex.StackTrace}");
                
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
            base.OnExit(e);

            Logger.Log(LogLevel.Information, "App:OnExit", "Prosim2GSX exiting ...");
        }

        protected void OnTick(object sender, EventArgs e)
        {
            if (Model.ServiceExited)
            {
                Current.Shutdown();
            }
        }

        protected void InitLog()
        {
            string logFilePath = @"..\log\" + Model.GetSetting("logFilePath");
            string logLevel = Model.GetSetting("logLevel");
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration().WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3,
                                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message} {NewLine}{Exception}");
            if (logLevel == "Warning")
                loggerConfiguration.MinimumLevel.Warning();
            else if (logLevel == "Debug")
                loggerConfiguration.MinimumLevel.Debug();
            else
                loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();
            Log.Information($"-----------------------------------------------------------------------");
            Logger.Log(LogLevel.Information, "App:InitLog", $"Prosim2GSX started! Log Level: {logLevel} Log File: {logFilePath}");
        }

        protected void InitSystray()
        {
            Logger.Log(LogLevel.Information, "App:InitSystray", $"Creating SysTray Icon ...");
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            notifyIcon.Icon = GetIcon("logo.ico");
            notifyIcon.ForceCreate(false);
        }

        protected void InitCef()
        {
            Logger.Log(LogLevel.Information, "App:InitCef", $"Initializing Cef Browser ...");
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
