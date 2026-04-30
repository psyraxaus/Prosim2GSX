using CFIT.AppFramework.UI.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prosim2GSX.AppConfig;
using Prosim2GSX.GSX;
using Prosim2GSX.GSX.Menu;
using Prosim2GSX.GSX.Services;
using Prosim2GSX.State;
using ProsimInterface;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;

namespace Prosim2GSX.UI.Views.Monitor
{
    // Adapter view-model over the long-lived FlightStatusState / GsxState stores.
    // The Monitor tab's bindings are unchanged; the actual polling and state
    // mutation now lives in StateUpdateWorker / MessageLogDrainWorker on
    // AppService, so the web layer (and any other consumer) sees the same data.
    //
    // Colour brushes are computed projections of the underlying booleans, which
    // is why they live on the Model rather than the store: the WPF UI is the
    // only consumer that wants SolidColorBrush — the web layer will read the
    // booleans and apply its own theme.
    public partial class ModelMonitor(AppService source) : ViewModelBase<AppService>(source)
    {
        protected static SolidColorBrush ColorValid { get; } = new(Colors.Green);
        protected static SolidColorBrush ColorInvalid { get; } = new(Colors.Red);
        protected static SolidColorBrush ColorGray { get; } = new(Color.FromArgb(0xFF, 0xD3, 0xD3, 0xD3));

        protected virtual Config Config => this.Source.Config;
        protected virtual FlightStatusState FlightStatus => this.Source.FlightStatus;
        protected virtual GsxState Gsx => this.Source.Gsx;

        // Solari board blink timer (~600ms per side, full cycle ~1.2s). View-only
        // animation state — does not belong on the long-lived state stores.
        protected virtual DispatcherTimer SolariTimer { get; set; }

        [ObservableProperty]
        protected bool _SolariToggle = false;

        // Visible-log mirror: store keeps up to 500 messages for durability /
        // web replay; the Monitor tab continues to render only the last ~9
        // visual lines, so we maintain a separate trimmed collection here.
        public virtual ObservableCollection<string> MessageLog { get; } = [];

        // Approximate character width of Consolas 10pt at the log panel width (~760px available).
        private const int LogCharsPerLine = 115;
        private const int LogMaxVisualLines = 9;

        protected override void InitializeModel()
        {
            SolariTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(600),
            };
            SolariTimer.Tick += (s, e) => SolariToggle = !SolariToggle;
        }

        private bool _started;

        public virtual void Start()
        {
            // AppWindow can call Start() twice on initial load (Loaded BeginInvoke +
            // OnVisibleChanged), so keep this idempotent — otherwise duplicate
            // CollectionChanged subscriptions cause every log message to appear twice.
            if (_started) return;
            _started = true;

            SyncMessageLogFromStore();
            FlightStatus.PropertyChanged += OnFlightStatusStateChanged;
            Gsx.PropertyChanged += OnGsxStateChanged;
            FlightStatus.MessageLog.CollectionChanged += OnStoreMessageLogChanged;
            SolariTimer.Start();
            // Force a one-shot refresh so all bindings re-read from the stores.
            NotifyPropertyChanged(string.Empty);
        }

        public virtual void Stop()
        {
            if (!_started) return;
            _started = false;

            SolariTimer?.Stop();
            FlightStatus.PropertyChanged -= OnFlightStatusStateChanged;
            Gsx.PropertyChanged -= OnGsxStateChanged;
            FlightStatus.MessageLog.CollectionChanged -= OnStoreMessageLogChanged;
        }

        [RelayCommand]
        public virtual void LogDir()
        {
            try { Process.Start(new ProcessStartInfo(Path.Join(Config.Definition.ProductPath, Config.Definition.ProductLogPath)) { UseShellExecute = true }); } catch { }
        }

        // ── Sim / aircraft / connection passthroughs ─────────────────────────

        public bool SimRunning => FlightStatus.SimRunning;
        public SolidColorBrush SimRunningColor => FlightStatus.SimRunning ? ColorValid : ColorInvalid;

        public bool SimConnected => FlightStatus.SimConnected;
        public SolidColorBrush SimConnectedColor => FlightStatus.SimConnected ? ColorValid : ColorInvalid;

        public bool SimSession => FlightStatus.SimSession;
        public SolidColorBrush SimSessionColor => FlightStatus.SimSession ? ColorValid : ColorInvalid;

        // SimPaused/SimWalkaround use reverse colour mapping: true == bad.
        public bool SimPaused => FlightStatus.SimPaused;
        public SolidColorBrush SimPausedColor => FlightStatus.SimPaused ? ColorInvalid : ColorValid;

        public bool SimWalkaround => FlightStatus.SimWalkaround;
        public SolidColorBrush SimWalkaroundColor => FlightStatus.SimWalkaround ? ColorInvalid : ColorValid;

        public long CameraState => FlightStatus.CameraState;
        public string SimVersion => FlightStatus.SimVersion;
        public string AircraftString => FlightStatus.AircraftString;

        // ── GSX passthroughs ─────────────────────────────────────────────────

        public bool GsxRunning => Gsx.GsxRunning;
        public SolidColorBrush GsxRunningColor => Gsx.GsxRunning ? ColorValid : ColorInvalid;

        public string GsxStarted => Gsx.GsxStarted;
        public SolidColorBrush GsxStartedColor => Gsx.GsxStartedValid ? ColorValid : ColorInvalid;

        public GsxMenuState GsxMenu => Gsx.GsxMenu;
        public int GsxPaxTarget => Gsx.GsxPaxTarget;
        public string GsxPaxTotal => Gsx.GsxPaxTotal;
        public string GsxCargoProgress => Gsx.GsxCargoProgress;

        public GsxServiceState ServiceReposition => Gsx.ServiceReposition;
        public GsxServiceState ServiceRefuel => Gsx.ServiceRefuel;
        public GsxServiceState ServiceCatering => Gsx.ServiceCatering;
        public GsxServiceState ServiceLavatory => Gsx.ServiceLavatory;
        public GsxServiceState ServiceWater => Gsx.ServiceWater;
        public GsxServiceState ServiceCleaning => Gsx.ServiceCleaning;

        // GPU indicator: gray when the current automation phase isn't relevant
        // (Preparation/Departure/Arrival/TurnAround), otherwise red/green.
        public bool ServiceGpuConnected => Gsx.ServiceGpuConnected;
        public SolidColorBrush ServiceGpuConnectedColor =>
            !Gsx.ServiceGpuPhaseRelevant ? ColorGray
            : Gsx.ServiceGpuConnected ? ColorValid : ColorInvalid;

        public GsxServiceState ServiceBoarding => Gsx.ServiceBoarding;
        public GsxServiceState ServiceDeboarding => Gsx.ServiceDeboarding;
        public string ServicePushback => Gsx.ServicePushback;
        public GsxServiceState ServiceJetway => Gsx.ServiceJetway;
        public GsxServiceState ServiceStairs => Gsx.ServiceStairs;

        // ── App-subsystem passthroughs ───────────────────────────────────────

        public bool AppGsxController => FlightStatus.AppGsxController;
        public SolidColorBrush AppGsxControllerColor => FlightStatus.AppGsxController ? ColorValid : ColorInvalid;

        public bool AppAircraftBinary => FlightStatus.AppAircraftBinary;
        public SolidColorBrush AppAircraftBinaryColor => FlightStatus.AppAircraftBinary ? ColorValid : ColorInvalid;

        public bool AppAircraftInterface => FlightStatus.AppAircraftInterface;
        public SolidColorBrush AppAircraftInterfaceColor => FlightStatus.AppAircraftInterface ? ColorValid : ColorInvalid;

        public bool AppProsimSdkConnected => FlightStatus.AppProsimSdkConnected;
        public SolidColorBrush AppProsimSdkConnectedColor => FlightStatus.AppProsimSdkConnected ? ColorValid : ColorInvalid;

        public bool AppAutomationController => FlightStatus.AppAutomationController;
        public SolidColorBrush AppAutomationControllerColor => FlightStatus.AppAutomationController ? ColorValid : ColorInvalid;

        public bool AppAudioController => FlightStatus.AppAudioController;
        public SolidColorBrush AppAudioControllerColor => FlightStatus.AppAudioController ? ColorValid : ColorInvalid;

        public AutomationState AppAutomationState => Gsx.AppAutomationState;
        public string AppAutomationDepartureServices => Gsx.AppAutomationDepartureServices;

        public string AssignedArrivalGate => Gsx.AssignedArrivalGate;

        public bool AppOnGround => FlightStatus.AppOnGround;
        public bool AppEnginesRunning => FlightStatus.AppEnginesRunning;
        public bool AppInMotion => FlightStatus.AppInMotion;
        public string AppProfile => FlightStatus.AppProfile;
        public string AppAircraft => FlightStatus.AppAircraft;

        // ── Store change handlers ────────────────────────────────────────────

        protected virtual void OnFlightStatusStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            var name = e?.PropertyName ?? "";
            // The Model property has the same name as the store property, so the
            // bare re-raise covers the value binding.
            NotifyPropertyChanged(name);

            // For state fields that drive a derived colour brush, raise the
            // colour property too so the indicator updates atomically.
            switch (name)
            {
                case nameof(FlightStatusState.SimRunning):              NotifyPropertyChanged(nameof(SimRunningColor)); break;
                case nameof(FlightStatusState.SimConnected):            NotifyPropertyChanged(nameof(SimConnectedColor)); break;
                case nameof(FlightStatusState.SimSession):              NotifyPropertyChanged(nameof(SimSessionColor)); break;
                case nameof(FlightStatusState.SimPaused):               NotifyPropertyChanged(nameof(SimPausedColor)); break;
                case nameof(FlightStatusState.SimWalkaround):           NotifyPropertyChanged(nameof(SimWalkaroundColor)); break;
                case nameof(FlightStatusState.AppGsxController):        NotifyPropertyChanged(nameof(AppGsxControllerColor)); break;
                case nameof(FlightStatusState.AppAircraftBinary):       NotifyPropertyChanged(nameof(AppAircraftBinaryColor)); break;
                case nameof(FlightStatusState.AppAircraftInterface):    NotifyPropertyChanged(nameof(AppAircraftInterfaceColor)); break;
                case nameof(FlightStatusState.AppProsimSdkConnected):   NotifyPropertyChanged(nameof(AppProsimSdkConnectedColor)); break;
                case nameof(FlightStatusState.AppAutomationController): NotifyPropertyChanged(nameof(AppAutomationControllerColor)); break;
                case nameof(FlightStatusState.AppAudioController):      NotifyPropertyChanged(nameof(AppAudioControllerColor)); break;
            }
        }

        protected virtual void OnGsxStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            var name = e?.PropertyName ?? "";
            NotifyPropertyChanged(name);

            switch (name)
            {
                case nameof(GsxState.GsxRunning):             NotifyPropertyChanged(nameof(GsxRunningColor)); break;
                case nameof(GsxState.GsxStartedValid):        NotifyPropertyChanged(nameof(GsxStartedColor)); break;
                case nameof(GsxState.ServiceGpuConnected):
                case nameof(GsxState.ServiceGpuPhaseRelevant):
                    NotifyPropertyChanged(nameof(ServiceGpuConnectedColor));
                    break;
            }
        }

        // ── Message log mirror ───────────────────────────────────────────────

        protected virtual void OnStoreMessageLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Only react to Adds — the store also emits Remove events when it
            // trims to its 500-cap, but the visible log on the Monitor tab has
            // its own (smaller) visual-line trim and shouldn't follow store evictions.
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null)
                return;

            foreach (var item in e.NewItems)
            {
                if (item is string msg)
                    MessageLog.Add(msg);
            }
            TrimToVisualLines();
            NotifyPropertyChanged(nameof(MessageLog));
        }

        protected virtual void SyncMessageLogFromStore()
        {
            // Populate from the store on Start so reopening the Monitor tab shows
            // recent history (the previous implementation showed only what was in
            // CFIT Logger.Messages at that moment — the store buffer is larger).
            MessageLog.Clear();
            foreach (var msg in FlightStatus.MessageLog)
                MessageLog.Add(msg);
            TrimToVisualLines();
            NotifyPropertyChanged(nameof(MessageLog));
        }

        private void TrimToVisualLines()
        {
            while (MessageLog.Count > 0 && LogVisualLineCount() > LogMaxVisualLines)
                MessageLog.RemoveAt(0);
        }

        private int LogVisualLineCount()
        {
            int total = 0;
            foreach (var msg in MessageLog)
                total += Math.Max(1, (int)Math.Ceiling((double)(msg?.Length ?? 0) / LogCharsPerLine));
            return total;
        }
    }
}
