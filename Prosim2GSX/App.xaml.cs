using CefSharp;
using CefSharp.OffScreen;
using H.NotifyIcon;
using Prosim2GSX.Models;
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

            Model = new();
            InitLog();
            InitSystray();
            InitCef();

            Controller = new(Model);
            Task.Run(Controller.Run);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTick;
            timer.Start();

            MainWindow = new MainWindow(notifyIcon.DataContext as NotifyIconViewModel, Model);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Model.CancellationRequested = true;
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
            if (!Cef.IsInitialized)
                Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
        }

        public Icon GetIcon(string filename)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Prosim2GSX.{filename}");
            return new Icon(stream);
        }
    }
}
