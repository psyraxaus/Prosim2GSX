using CFIT.AppLogger;
using Prosim2GSX.GSX;
using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Prosim2GSX.State
{
    // Polls SimConnect / Gsx / Audio / Prosim controllers and writes the results
    // into FlightStatusState and GsxState. Long-lived worker owned by AppService —
    // the previous implementation lived in ModelMonitor.OnUpdate and only ran
    // while the Monitor tab was active. Lifting it here means the web/WS layer
    // and any other consumer always sees current state.
    //
    // The DispatcherTimer ticks on the WPF dispatcher; the actual polling work is
    // offloaded via Task.Run so blocking framework calls (SimConnect WaitSimLoop
    // locks, etc.) never stall the UI thread. Property writes on the [ObservableProperty]
    // backed state classes therefore happen on a background thread — WPF tolerates
    // this for scalar bindings, matching the prior pattern.
    public class StateUpdateWorker
    {
        private readonly AppService _app;
        private readonly DispatcherTimer _timer;
        private volatile bool _isUpdating;

        public StateUpdateWorker(AppService app)
        {
            _app = app;

            int interval = Math.Max(100, app?.Config?.UiRefreshInterval ?? 500);
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(interval),
            };
            _timer.Tick += OnTick;
        }

        public virtual void Start()
        {
            // Start can be called from any thread (e.g. AppService init); marshal
            // to the timer's dispatcher because DispatcherTimer.Start requires it.
            var d = _timer.Dispatcher;
            if (d.CheckAccess()) _timer.Start();
            else d.InvokeAsync(_timer.Start);
        }

        public virtual void Stop()
        {
            var d = _timer.Dispatcher;
            if (d.CheckAccess()) _timer.Stop();
            else d.InvokeAsync(_timer.Stop);
        }

        protected virtual async void OnTick(object? sender, EventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;
            try
            {
                await Task.Run(() =>
                {
                    try { UpdateSim(); } catch (Exception ex) { Logger.LogException(ex); }
                    try { UpdateGsx(); } catch { }
                    try { UpdateApp(); } catch { }
                });
            }
            finally
            {
                _isUpdating = false;
            }
        }

        protected virtual void UpdateSim()
        {
            var fs = _app.FlightStatus;
            var simConnectCtrl = _app.SimService?.Controller;
            var simConnect = _app.SimConnect;

            if (simConnect == null) return;

            fs.SimRunning = simConnectCtrl?.IsSimRunning ?? false;
            fs.SimConnected = simConnect.IsSimConnected;
            fs.SimSession = simConnect.IsSessionRunning && !simConnect.IsSessionStopped;
            fs.SimPaused = simConnect.IsPaused;
            fs.SimWalkaround = _app.GsxService?.IsWalkaround ?? false;
            fs.CameraState = simConnect.CameraState;
            fs.SimVersion = simConnect.SimVersionString ?? "";
            fs.AircraftString = simConnect.AircraftString ?? "";
        }

        protected virtual void UpdateGsx()
        {
            var gsx = _app.Gsx;
            var ctrl = _app.GsxService;
            if (ctrl == null) return;

            gsx.GsxRunning = ctrl.CheckBinaries();
            gsx.GsxStarted = $"{ctrl.CouatlLastStarted} | {ctrl.CouatlLastProgress}";
            gsx.GsxStartedValid = ctrl.CouatlVarsValid;
            gsx.GsxMenu = ctrl.Menu?.MenuState ?? GSX.Menu.GsxMenuState.UNKNOWN;

            var services = ctrl.GsxServices;
            if (services == null) return;

            var board = services.TryGetValue(GsxServiceType.Boarding, out var b) ? b as GsxServiceBoarding : null;
            var deboard = services.TryGetValue(GsxServiceType.Deboarding, out var d) ? d as GsxServiceDeboarding : null;
            var pushback = services.TryGetValue(GsxServiceType.Pushback, out var p) ? p as GsxServicePushback : null;

            gsx.GsxPaxTarget = board?.SubPaxTarget?.GetValue<int>() ?? 0;
            gsx.GsxPaxTotal = $"{board?.SubPaxTotal?.GetValue<int>() ?? 0} | {deboard?.SubPaxTotal?.GetValue<int>() ?? 0}";
            gsx.GsxCargoProgress = $"{board?.SubCargoPercent?.GetValue<int>() ?? 0} | {deboard?.SubCargoPercent?.GetValue<int>() ?? 0}";

            if (services.TryGetValue(GsxServiceType.Reposition, out var s)) gsx.ServiceReposition = s.State;
            if (services.TryGetValue(GsxServiceType.Refuel, out s)) gsx.ServiceRefuel = LatchCompleted(s);
            if (services.TryGetValue(GsxServiceType.Catering, out s)) gsx.ServiceCatering = LatchCompleted(s);
            if (services.TryGetValue(GsxServiceType.Lavatory, out s)) gsx.ServiceLavatory = LatchCompleted(s);
            if (services.TryGetValue(GsxServiceType.Water, out s)) gsx.ServiceWater = LatchCompleted(s);
            if (services.TryGetValue(GsxServiceType.Cleaning, out s)) gsx.ServiceCleaning = LatchCompleted(s);

            UpdateGpu();

            if (services.TryGetValue(GsxServiceType.Boarding, out s)) gsx.ServiceBoarding = LatchCompleted(s);
            if (services.TryGetValue(GsxServiceType.Deboarding, out s)) gsx.ServiceDeboarding = s.State;
            if (pushback != null) gsx.ServicePushback = $"{pushback.State} ({pushback.PushStatus})";
            if (services.TryGetValue(GsxServiceType.Jetway, out s)) gsx.ServiceJetway = s.State;
            if (services.TryGetValue(GsxServiceType.Stairs, out s)) gsx.ServiceStairs = s.State;
        }

        // Mirrors ModelMonitor.LatchCompleted: services that latch to Completed in
        // their own GetState() (Refuel/Water/Lavatory/Cleaning) need a presentation
        // override so the indicators don't stay green through taxi-out and flight.
        protected virtual GsxServiceState LatchCompleted(GsxService service)
        {
            var phase = _app.GsxService?.AutomationController?.State ?? AutomationState.SessionStart;

            if (phase == AutomationState.TaxiOut
                || phase == AutomationState.Flight
                || phase == AutomationState.TaxiIn
                || phase == AutomationState.Arrival)
                return GsxServiceState.Callable;

            if (service.WasCompleted && (phase == AutomationState.Departure || phase == AutomationState.PushBack))
                return GsxServiceState.Completed;

            return service.State;
        }

        protected virtual void UpdateGpu()
        {
            var gsx = _app.Gsx;
            var phase = _app.GsxService?.AutomationController?.State ?? AutomationState.SessionStart;
            // PushBack is included so the GPU indicator reflects reality during
            // the pre-pushback window where ground equipment is physically still
            // attached until the departure sequence clears it.
            bool relevant = phase == AutomationState.Preparation
                         || phase == AutomationState.Departure
                         || phase == AutomationState.PushBack
                         || phase == AutomationState.Arrival
                         || phase == AutomationState.TurnAround;

            gsx.ServiceGpuPhaseRelevant = relevant;
            gsx.ServiceGpuConnected = relevant && (_app.GsxService?.AircraftInterface?.EquipmentGpu ?? false);
        }

        protected virtual void UpdateApp()
        {
            var fs = _app.FlightStatus;
            var gsx = _app.Gsx;
            var ctrl = _app.GsxService;
            var ai = ctrl?.AircraftInterface;
            var auto = ctrl?.AutomationController;

            fs.AppGsxController = ctrl?.IsActive ?? false;
            fs.AppAircraftBinary = ctrl?.AircraftBinary ?? false;
            fs.AppAircraftInterface = ai?.IsLoaded ?? false;
            fs.AppProsimSdkConnected = ai?.ProsimInterface?.SdkInterface?.IsConnected ?? false;
            fs.AppAutomationController = auto?.IsStarted ?? false;
            fs.AppAudioController = _app.AudioService?.IsActive ?? false;

            gsx.AppAutomationState = auto?.State ?? AutomationState.SessionStart;
            gsx.AppAutomationDepartureServices = $"{auto?.ServiceCountCompleted ?? 0} / {auto?.ServiceCountRunning ?? 0} / {auto?.ServiceCountTotal ?? 0}";

            try
            {
                fs.AppOnGround = auto?.IsOnGround ?? true;
                fs.AppEnginesRunning = ai?.EnginesRunning ?? false;
                fs.AppInMotion = (ai?.GroundSpeed ?? 0) > (_app.Config?.SpeedTresholdTaxiOut ?? 2);
            }
            catch { }

            fs.AppProfile = ctrl?.AircraftProfile?.ToString() ?? "";
            fs.AppAircraft = $"{ai?.Airline ?? ""} / {ai?.Title ?? ""} / {ai?.Registration ?? ""}";

            // Header strip values — mirror HeaderBarControl.OnUpdate so the web
            // header reads identically to the WPF top bar.
            try
            {
                fs.FlightNumber = !string.IsNullOrWhiteSpace(ai?.FlightNumber) ? ai.FlightNumber : "--------";

                var sim = _app.SimConnect;
                bool simConnected = sim?.IsSimConnected == true && sim?.IsSessionRunning == true;
                if (simConnected && ai != null)
                {
                    int zuluSec = ai.ZuluTimeSeconds;
                    int hours = (zuluSec / 3600) % 24;
                    int minutes = (zuluSec % 3600) / 60;
                    fs.UtcTime = $"{hours:D2}:{minutes:D2}Z";
                }
                else
                {
                    fs.UtcTime = DateTime.UtcNow.ToString("HH:mm") + "Z";
                }

                fs.UtcDate = DateTime.UtcNow.ToString("dd MMM").ToUpper();
            }
            catch { }
        }
    }
}
