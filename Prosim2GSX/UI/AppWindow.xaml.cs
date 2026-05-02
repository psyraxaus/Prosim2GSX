using CFIT.AppLogger;
using CFIT.AppTools;
using Prosim2GSX.UI.Views.Audio;
using Prosim2GSX.UI.Views.Checklists;
using Prosim2GSX.UI.Views.Debug;
using Prosim2GSX.UI.Views.Monitor;
using Prosim2GSX.UI.Views.Profiles;
using Prosim2GSX.UI.Views.Settings;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Prosim2GSX.UI
{
    public interface IView
    {
        public void Start();
        public void Stop();
    }

    public partial class AppWindow : Window
    {
        public static UiIconLoader IconLoader { get; } = new(Assembly.GetExecutingAssembly(), IconLoadSource.Embedded, "Prosim2GSX.UI.Icons.");

        private int _previousTabIndex = -1;
        private DispatcherTimer _logTrimTimer;
        private DispatcherTimer _resourceHeartbeatTimer;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
        private const uint GR_GDIOBJECTS = 0;
        private const uint GR_USEROBJECTS = 1;

        public AppWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.WorkArea.Height * 0.95;
            this.Loaded += OnWindowLoaded;
            this.IsVisibleChanged += OnVisibleChanged;
            this.SizeChanged += OnWindowSizeChanged;
            this.Closed += OnWindowClosed;

            // Show SDK warning banner if running in degraded mode
            if (!Prosim2GSX.Instance.IsSdkAvailable)
            {
                LabelSdkWarning.Text = "ProSim SDK is not configured. ProSim integration is disabled. Please set the SDK path in App Settings and restart the application.";
                PanelSdkWarning.Visibility = Visibility.Visible;
            }

            if (Prosim2GSX.Instance.UpdateDetected)
            {
                if (Prosim2GSX.Instance.UpdateIsDev)
                    LabelVersionCheck.Inlines.Add("New Develop Version ");
                else
                    LabelVersionCheck.Inlines.Add("New Stable Version ");
                var run = new Run($"{Prosim2GSX.Instance.UpdateVersion}");

                Hyperlink hyperlink;
                if (Prosim2GSX.Instance.UpdateIsDev)
                    hyperlink = new Hyperlink(run)
                    {
                        NavigateUri = new Uri("https://github.com/psyraxaus/Prosim2GSX/blob/master/Prosim2GSX-Installer-latest.exe")
                    };
                else
                    hyperlink = new Hyperlink(run)
                    {
                        NavigateUri = new Uri("https://github.com/psyraxaus/Prosim2GSX/releases/latest")
                    };
                LabelVersionCheck.Inlines.Add(hyperlink);
                LabelVersionCheck.Inlines.Add(" available!");
                this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Nav.RequestNavigateHandler));
                PanelVersion.Visibility = Visibility.Visible;
            }
        }

        protected virtual void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            bool sdkAvailable = Prosim2GSX.Instance.IsSdkAvailable;

            if (sdkAvailable)
            {
                MainTabControl.Items[0] = CreateTabItem("FLIGHT STATUS", new ViewMonitor());
                MainTabControl.Items[1] = CreateTabItem("OFP", new Views.Ofp.ViewOfp());
                MainTabControl.Items[2] = CreateTabItem("CHECKLISTS", new ViewChecklist());
                MainTabControl.Items[3] = CreateTabItem("GSX SETTINGS", new Views.Automation.ViewAutomation());
                MainTabControl.Items[4] = CreateTabItem("AIRCRAFT PROFILES", new ViewProfiles());
                MainTabControl.Items[5] = CreateTabItem("AUDIO SETTINGS", new ViewAudio());
            }
            else
            {
                // In degraded mode, replace SDK-dependent tabs with placeholder content
                var placeholder = CreateDegradedPlaceholder();
                MainTabControl.Items[0] = CreateTabItem("FLIGHT STATUS", placeholder);
                MainTabControl.Items[1] = CreateTabItem("OFP", CreateDegradedPlaceholder());
                MainTabControl.Items[2] = CreateTabItem("CHECKLISTS", CreateDegradedPlaceholder());
                MainTabControl.Items[3] = CreateTabItem("GSX SETTINGS", CreateDegradedPlaceholder());
                MainTabControl.Items[4] = CreateTabItem("AIRCRAFT PROFILES", CreateDegradedPlaceholder());
                MainTabControl.Items[5] = CreateTabItem("AUDIO SETTINGS", CreateDegradedPlaceholder());
            }

            // Settings tab is always available (needed to configure SDK path)
            MainTabControl.Items[6] = CreateTabItem("APP SETTINGS", new ViewSettings());

            // Optional Debug tab — appended at index 7 only when AppConfig
            // ShowDebugTab is true. Kept out of the XAML so the visual tree
            // never even constructs the view in normal end-user installs.
            if (Prosim2GSX.Instance.AppService?.Config?.ShowDebugTab == true)
            {
                MainTabControl.Items.Add(CreateTabItem("DEBUG", new ViewDebug()));
            }

            // Set index and previousTabIndex before subscribing so SelectionChanged
            // does not fire Start() while the window is still in its layout pass.
            int defaultTab = sdkAvailable ? 0 : 6; // Go straight to Settings in degraded mode
            MainTabControl.SelectedIndex = defaultTab;
            _previousTabIndex = defaultTab;
            MainTabControl.SelectionChanged += OnTabSelectionChanged;

            if (sdkAvailable)
            {
                // Defer the initial Start() until after the window is fully rendered
                // so backend services have a chance to initialise before polling begins.
                Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => ((MainTabControl.Items[0] as TabItem)?.Content as IView)?.Start()));
            }

            StartLogTrimTimer();
            StartResourceHeartbeat();
        }

        // Diagnostic: log USER / GDI / Handle counts for our own process every 60s.
        // USER objects: windows, menus, hooks, popups, tooltips. GDI objects: pens,
        // brushes, fonts, bitmaps, regions. A steady climb here points to a leak in
        // this process; flat counts during a session-wide ERROR_NOT_ENOUGH_QUOTA
        // crash points to another process (often MSFS) exhausting the desktop pool.
        private void StartResourceHeartbeat()
        {
            _resourceHeartbeatTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(60),
            };
            _resourceHeartbeatTimer.Tick += OnResourceHeartbeatTick;
            _resourceHeartbeatTimer.Start();
            // Fire one immediately so we have a baseline at startup.
            OnResourceHeartbeatTick(this, EventArgs.Empty);
        }

        private bool _resourceHeartbeatErrorReported = false;

        private void OnResourceHeartbeatTick(object sender, EventArgs e)
        {
            try
            {
                using var proc = Process.GetCurrentProcess();
                uint user = GetGuiResources(proc.Handle, GR_USEROBJECTS);
                uint gdi = GetGuiResources(proc.Handle, GR_GDIOBJECTS);
                int handles = proc.HandleCount;
                Logger.Information($"Resource heartbeat: USER={user}, GDI={gdi}, Handles={handles}");
            }
            catch (Exception ex) when (!_resourceHeartbeatErrorReported)
            {
                _resourceHeartbeatErrorReported = true;
                Logger.LogException(ex);
            }
        }

        // Periodic safety net: trims Logger.Messages while the Monitor tab is not
        // actively draining it (e.g. user spent hours on Settings). Without this, the
        // CFIT logger's in-memory queue is unbounded and grows for every Information+
        // log message until the app is closed or returns to the Monitor tab.
        private void StartLogTrimTimer()
        {
            _logTrimTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(10),
            };
            _logTrimTimer.Tick += OnLogTrimTick;
            _logTrimTimer.Start();
        }

        private bool _logTrimErrorReported = false;

        private void OnLogTrimTick(object sender, EventArgs e)
        {
            try
            {
                int cap = Math.Max(1, Prosim2GSX.Instance?.AppService?.Config?.UiLogMaxMessages ?? 200);
                while (Logger.Messages.Count > cap)
                    Logger.Messages.TryDequeue(out _);
            }
            catch (Exception ex) when (!_logTrimErrorReported)
            {
                _logTrimErrorReported = true;
                Logger.LogException(ex);
            }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            _logTrimTimer?.Stop();
            _logTrimTimer = null;
            _resourceHeartbeatTimer?.Stop();
            _resourceHeartbeatTimer = null;
        }

        private static UIElement CreateDegradedPlaceholder()
        {
            return new TextBlock
            {
                Text = "ProSim SDK is not configured. Please set the SDK path in App Settings and restart the application.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap,
                Foreground = System.Windows.Media.Brushes.Gray
            };
        }

        private static TabItem CreateTabItem(string header, UIElement content)
        {
            return new TabItem
            {
                Header = header,
                Content = content
            };
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!e.HeightChanged) return;

            var workArea = SystemParameters.WorkArea;
            double windowBottom = this.Top + this.ActualHeight;
            double workAreaBottom = workArea.Top + workArea.Height;

            if (windowBottom > workAreaBottom)
            {
                this.Top = Math.Max(workArea.Top, workAreaBottom - this.ActualHeight);
            }
        }

        protected virtual void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var selectedContent = (MainTabControl?.SelectedItem as TabItem)?.Content as IView;
            if (this.Visibility != Visibility.Visible)
                selectedContent?.Stop();
            else
                selectedContent?.Start();
        }

        protected virtual void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_previousTabIndex >= 0 && _previousTabIndex < MainTabControl.Items.Count)
            {
                var prevContent = (MainTabControl.Items[_previousTabIndex] as TabItem)?.Content as IView;
                prevContent?.Stop();
            }

            var newContent = (MainTabControl?.SelectedItem as TabItem)?.Content as IView;
            newContent?.Start();

            _previousTabIndex = MainTabControl?.SelectedIndex ?? -1;
        }
    }
}
