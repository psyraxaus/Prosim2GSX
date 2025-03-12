﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using CefSharp;
using CefSharp.OffScreen;
using H.NotifyIcon;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB;
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
        private EnhancedServiceController Controller;
        private EFBApplication EfbApp;

        private TaskbarIcon notifyIcon;
        // UseEfbUi is now controlled by the ServiceModel

        public static string ConfigFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Prosim2GSX\Prosim2GSX.config";
        public static string AppDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Prosim2GSX\bin";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Check if application is already running
                if (Process.GetProcessesByName("Prosim2GSX").Length > 1)
                {
                    MessageBox.Show("Prosim2GSX is already running!", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }

                // Ensure application directory exists
                if (!Directory.Exists(AppDir))
                {
                    Directory.CreateDirectory(AppDir);
                }
                
                Directory.SetCurrentDirectory(AppDir);

                // Create configuration file if it doesn't exist
                if (!File.Exists(ConfigFile))
                {
                    // Try local config file if AppData config doesn't exist
                    ConfigFile = Directory.GetCurrentDirectory() + @"\Prosim2GSX.config";
                    if (!File.Exists(ConfigFile))
                    {
                        // Create new config file with XML writer for better control in .NET 8.0
                        var settings = new XmlWriterSettings
                        {
                            Indent = true,
                            IndentChars = "  ",
                            NewLineChars = "\n",
                            NewLineHandling = NewLineHandling.Replace
                        };
                        
                        using (var writer = XmlWriter.Create(ConfigFile, settings))
                        {
                            writer.WriteStartDocument();
                            writer.WriteStartElement("appSettings");
                            writer.WriteEndElement();
                            writer.WriteEndDocument();
                        }
                    }
                }

                // Initialize application components
                Model = new();
                InitLog();
                InitSystray();
                InitCef();

                // Create EventAggregator
                var eventAggregator = new EventAggregator(Logger.Instance);
                
                // Register it with the ServiceModel
                Model.SetService<IEventAggregator>(eventAggregator);
                
                // Start service controller
                Controller = new EnhancedServiceController(Model, Logger.Instance, eventAggregator);
                Task.Run(Controller.Run);

                // Set up timer for service monitoring
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                timer.Tick += OnTick;
                timer.Start();

            // Initialize EFB UI if enabled in settings
            if (Model.UseEfbUi)
            {
                InitEfbUi();
            }
            else
            {
                // Create legacy main window
                MainWindow = new MainWindow(notifyIcon?.DataContext as NotifyIconViewModel, Model);
            }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during application startup: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Model.CancellationRequested = true;
            notifyIcon?.Dispose();
            
            // Stop EFB UI if it was initialized
            EfbApp?.Stop();
            
            Cef.Shutdown();
            base.OnExit(e);

            Logger.Log(LogLevel.Information, "App:OnExit", "Prosim2GSX exiting ...");
        }
        
        private async void InitEfbUi()
        {
            try
            {
                Logger.Log(LogLevel.Information, "App:InitEfbUi", "Initializing EFB UI...");
                
                // Log diagnostic information before initialization
                LogEfbDiagnosticInfo("Pre-initialization");
                
                // Wait for services to be initialized
                Logger.Log(LogLevel.Debug, "App:InitEfbUi", "Waiting for services to be initialized...");
                await Task.Run(async () => {
                    int attempts = 0;
                    while (!Controller.AreServicesInitialized() && attempts < 30)
                    {
                        await Task.Delay(500);
                        attempts++;
                    }
                    
                    if (attempts >= 30)
                    {
                        Logger.Log(LogLevel.Warning, "App:InitEfbUi", 
                            "Timed out waiting for services to initialize. EFB UI may not function correctly.");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Debug, "App:InitEfbUi", 
                            $"Services initialized after {attempts * 0.5} seconds.");
                    }
                });
                
                // Create and initialize the EFB application with logger
                Logger.Log(LogLevel.Debug, "App:InitEfbUi", "Creating EFBApplication instance");
                EfbApp = new EFBApplication(Model, Logger.Instance);
                
                // Initialize the EFB application
                Logger.Log(LogLevel.Debug, "App:InitEfbUi", "Calling EFBApplication.InitializeAsync()");
                bool initialized = await EfbApp.InitializeAsync();
                
                if (initialized)
                {
                    Logger.Log(LogLevel.Debug, "App:InitEfbUi", "EFB UI initialization successful, starting application");
                    
                    // Start the EFB application
                    bool started = EfbApp.Start();
                    
                    if (started)
                    {
                        // Set the main window to the EFB main window
                        MainWindow = EfbApp.MainWindow as Window;
                        Logger.Log(LogLevel.Information, "App:InitEfbUi", "EFB UI initialized and started successfully");
                    }
                    else
                    {
                        // Log detailed information about the failure
                        Logger.Log(LogLevel.Error, "App:InitEfbUi", 
                            $"Failed to start EFB UI. Initialization state: {EfbApp.InitializationState}");
                        
                        // Log diagnostic information after failure
                        LogEfbDiagnosticInfo("Post-start-failure");
                        
                        // Fall back to legacy UI
                        Logger.Log(LogLevel.Information, "App:InitEfbUi", "Falling back to legacy UI");
                        MainWindow = new MainWindow(notifyIcon?.DataContext as NotifyIconViewModel, Model);
                    }
                }
                else
                {
                    // Log detailed information about the failure
                    Logger.Log(LogLevel.Error, "App:InitEfbUi", 
                        $"Failed to initialize EFB UI. Initialization state: {EfbApp.InitializationState}");
                    
                    // Log diagnostic information after failure
                    LogEfbDiagnosticInfo("Post-initialization-failure");
                    
                    // Fall back to legacy UI
                    Logger.Log(LogLevel.Information, "App:InitEfbUi", "Falling back to legacy UI");
                    MainWindow = new MainWindow(notifyIcon?.DataContext as NotifyIconViewModel, Model);
                }
            }
            catch (Exception ex)
            {
                // Log the full exception details
                Logger.Log(LogLevel.Error, "App:InitEfbUi", ex, "Error initializing EFB UI");
                
                // Log additional diagnostic information
                LogEfbDiagnosticInfo("Post-exception");
                
                // Fall back to legacy UI
                Logger.Log(LogLevel.Information, "App:InitEfbUi", "Falling back to legacy UI due to exception");
                MainWindow = new MainWindow(notifyIcon?.DataContext as NotifyIconViewModel, Model);
            }
        }
        
        /// <summary>
        /// Logs diagnostic information about the EFB UI environment
        /// </summary>
        /// <param name="stage">The stage at which diagnostics are being logged</param>
        private void LogEfbDiagnosticInfo(string stage)
        {
            try
            {
                Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", $"--- EFB Diagnostics ({stage}) ---");
                
                // Application directory information
                Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", $"AppDir: {AppDir}");
                Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", $"Current directory: {Directory.GetCurrentDirectory()}");
                
                // EFB UI configuration
                Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", $"UseEfbUi setting: {Model.UseEfbUi}");
                
                // EFB UI directories
                var efbAssetsDir = Path.Combine(AppDir, "UI", "EFB", "Assets");
                var efbThemesDir = Path.Combine(efbAssetsDir, "Themes");
                
                Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", 
                    $"EFB assets directory exists: {Directory.Exists(efbAssetsDir)}, Path: {efbAssetsDir}");
                Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", 
                    $"EFB themes directory exists: {Directory.Exists(efbThemesDir)}, Path: {efbThemesDir}");
                
                // Check for theme files if the directory exists
                if (Directory.Exists(efbThemesDir))
                {
                    try
                    {
                        var themeFiles = Directory.GetFiles(efbThemesDir, "*.json");
                        Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", $"Theme files found: {themeFiles.Length}");
                        
                        foreach (var file in themeFiles)
                        {
                            Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", 
                                $"Theme file: {Path.GetFileName(file)}, Size: {new FileInfo(file).Length} bytes");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", 
                            $"Error enumerating theme files: {ex.Message}");
                    }
                }
                
                // EFB application state if available
                if (EfbApp != null)
                {
                    Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", 
                        $"EFB application initialized: {EfbApp.IsInitialized}, " +
                        $"State: {EfbApp.InitializationState}");
                }
                else
                {
                    Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", "EFB application not created yet");
                }
                
                // System information
                Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", 
                    $"Available memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", 
                    $"OS version: {Environment.OSVersion.VersionString}");
                Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", 
                    $".NET version: {Environment.Version}");
                
                Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", "--- End EFB Diagnostics ---");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Debug, "App:EfbDiagnostics", 
                    $"Error collecting diagnostic information: {ex.Message}");
            }
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
            try
            {
                // Ensure log directory exists
                string logDir = Path.Combine(AppDir, @"..\log");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                
                string logFilePath = Path.Combine(logDir, Model.GetSetting("logFilePath"));
                string logLevel = Model.GetSetting("logLevel");
                
                // Configure Serilog with .NET 8.0 compatible settings
                LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                    .WriteTo.File(
                        logFilePath, 
                        rollingInterval: RollingInterval.Day, 
                        retainedFileCountLimit: 3,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message} {NewLine}{Exception}",
                        buffered: false,  // Disable buffering for immediate writes
                        flushToDiskInterval: TimeSpan.FromSeconds(1) // Flush to disk more frequently
                    );
                
                // Set log level
                switch (logLevel.ToLower())
                {
                    case "warning":
                        loggerConfiguration.MinimumLevel.Warning();
                        break;
                    case "debug":
                        loggerConfiguration.MinimumLevel.Debug();
                        break;
                    default:
                        loggerConfiguration.MinimumLevel.Information();
                        break;
                }
                
                // Create and set the logger
                Log.Logger = loggerConfiguration.CreateLogger();
                Log.Information($"-----------------------------------------------------------------------");
                Logger.Log(LogLevel.Information, "App:InitLog", $"Prosim2GSX started! Log Level: {logLevel} Log File: {logFilePath}");
            }
            catch (Exception ex)
            {
                // Show error message to the user
                MessageBox.Show(
                    $"Error initializing file logging: {ex.Message}\n\nLogging will continue in memory only.",
                    "Logging Warning", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                
                // Create a minimal logger that doesn't write to any external sink
                // but still allows the application to function and show logs in the UI
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .CreateLogger();
                
                // Log the initialization error (this will still go to the MessageQueue for UI display)
                Logger.Log(LogLevel.Error, "App:InitLog", $"Failed to initialize file logging: {ex.Message}");
            }
        }

        protected void InitSystray()
        {
            Logger.Log(LogLevel.Information, "App:InitSystray", $"Creating SysTray Icon ...");
            try
            {
                notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
                var icon = GetIcon("logo.ico");
                if (icon != null)
                {
                    notifyIcon.Icon = icon;
                    notifyIcon.ForceCreate(false);
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "App:InitSystray", "Failed to load icon, using default icon");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "App:InitSystray", $"Error creating system tray icon: {ex.Message}");
            }
        }

        protected void InitCef()
        {
            Logger.Log(LogLevel.Information, "App:InitCef", $"Initializing Cef Browser ...");
            var settings = new CefSettings();
            
            // Add any additional settings required for .NET 8.0 compatibility
            settings.CefCommandLineArgs.Add("disable-gpu", "1"); // Disable GPU acceleration
            settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1");
            settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
            
            // Initialize Cef with the updated settings
            if (!Cef.IsInitialized)
            {
                // Use the recommended initialization approach for CefSharp 120+
                Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            }
        }

        public Icon GetIcon(string filename)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Prosim2GSX.{filename}");
            if (stream == null)
            {
                Logger.Log(LogLevel.Warning, "App:GetIcon", $"Resource not found: Prosim2GSX.{filename}");
                return null;
            }
            
            // Use try-catch to handle potential issues with System.Drawing in .NET 8.0
            try
            {
                return new Icon(stream);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "App:GetIcon", $"Error loading icon: {ex.Message}");
                return null;
            }
        }
    }
}
