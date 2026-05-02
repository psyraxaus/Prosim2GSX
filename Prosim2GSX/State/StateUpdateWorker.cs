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
                    try { UpdateChecklist(); } catch (Exception ex) { Logger.LogException(ex); }
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
            if (pushback != null)
            {
                gsx.ServicePushback = $"{pushback.State} ({pushback.PushStatus})";
                gsx.PushbackVehicleState = pushback.VehiclePushbackStateLabel;
                gsx.BypassPinInserted = pushback.IsPinInserted;
                gsx.EngineStartConfirmed = pushback.EngineStartConfirmed;
            }
            if (services.TryGetValue(GsxServiceType.Jetway, out s)) gsx.ServiceJetway = s.State;
            if (services.TryGetValue(GsxServiceType.Stairs, out s)) gsx.ServiceStairs = s.State;

            UpdateAssignedGate();
        }

        // Reads the three GSX SetGate LVARs and writes the formatted display
        // string into OfpState and GsxState. Suffix == -1 is GSX's "no
        // assignment" sentinel; Name == 0 (NONE) also means unassigned.
        // Name == 10 (GATE) yields "Gate {N}" (no letter prefix).
        // Names 12..37 map to letters A..Z, so display becomes "{Letter}{N}".
        // The two store writes only fire INPC when the value actually changes
        // (the [ObservableProperty]-generated setter does the equality check),
        // so a stable readback per tick is broadcast-cheap.
        protected virtual void UpdateAssignedGate()
        {
            var ctrl = _app.GsxService;
            var simStore = ctrl?.SimStore;
            if (simStore == null) return;

            int name, number, suffix;
            try
            {
                name = (int)(simStore[GsxConstants.VarSetGateName]?.GetNumber() ?? 0);
                number = (int)(simStore[GsxConstants.VarSetGateNumber]?.GetNumber() ?? 0);
                suffix = (int)(simStore[GsxConstants.VarSetGateSuffix]?.GetNumber() ?? -1);
            }
            catch { return; }

            var display = FormatAssignedGate(name, number, suffix);
            _app.Ofp.AssignedArrivalGate = display;
            _app.Gsx.AssignedArrivalGate = display;
        }

        protected static string FormatAssignedGate(int name, int number, int suffix)
        {
            if (suffix == -1) return "";
            if (name == 0) return "";
            if (name == 10) return $"Gate {number}";
            if (name >= 12 && name <= 37)
            {
                char letter = (char)('A' + (name - 12));
                return $"{letter}{number}";
            }
            return "";
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

        // Auto-reset state machine. Fires a full reset of the loaded checklist
        // when a flight cycle completes: engines were running while on-ground
        // (pre-departure or post-landing), then engines stop. The takeoff
        // transition (on-ground -> airborne) auto-clears PRE-START/STARTUP/TAXI
        // so those sections are not stale when the user lands.
        private bool _wasOnGround = true;
        private bool _wasEnginesRunning = false;

        protected virtual void UpdateChecklist()
        {
            var cl = _app?.Checklist;
            var fs = _app?.FlightStatus;
            var ai = _app?.GsxService?.AircraftInterface;
            if (cl == null || fs == null) return;

            // Auto-reset edges. Compare the current store values (already
            // written by UpdateApp this tick) to the previous snapshot.
            var nowOnGround = fs.AppOnGround;
            var nowEnginesRunning = fs.AppEnginesRunning;

            // Takeoff: on-ground -> airborne. Clear pre-flight sections so the
            // user lands with a clean approach/landing flow.
            if (_wasOnGround && !nowOnGround)
            {
                cl.ResetSections("PRE START", "STARTUP", "BEFORE TAXI", "TAXI", "BEFORE TAKE-OFF", "TAKE-OFF");
            }

            // Shutdown: on-ground AND engines just transitioned running -> off.
            // Full reset of all sections (fresh flight cycle next time).
            if (nowOnGround && _wasEnginesRunning && !nowEnginesRunning)
            {
                cl.ResetAll();
            }

            _wasOnGround = nowOnGround;
            _wasEnginesRunning = nowEnginesRunning;

            // Dataref-driven item evaluation. Walk the current checklist's items;
            // for each item that has a dataref, read it via the SDK and update
            // IsChecked. Manual items are NEVER touched here — they only flip via
            // user click, RESET, or the auto-reset edges above.
            if (cl.Definition?.Sections == null) return;
            var sdk = ai?.ProsimInterface?.SdkInterface;
            if (sdk == null || !sdk.IsConnected) return;

            for (int s = 0; s < cl.Definition.Sections.Count; s++)
            {
                if (!cl.ItemsBySection.TryGetValue(s, out var items)) continue;
                for (int i = 0; i < items.Count; i++)
                {
                    var def = items[i].Definition;
                    if (def.IsNote || def.IsSeparator) continue;
                    if (string.IsNullOrWhiteSpace(def.DataRef)) continue;
                    if (string.IsNullOrWhiteSpace(def.DataRefCondition)) continue;

                    bool? satisfied = EvaluateCondition(sdk, def.DataRef, def.DataRefCondition);
                    if (!satisfied.HasValue) continue;
                    if (items[i].IsChecked != satisfied.Value)
                        items[i].IsChecked = satisfied.Value;
                }
            }

            cl.RecomputeCurrentItem();
        }

        // Returns null when the dataref or condition cannot be evaluated; caller
        // leaves the existing IsChecked alone in that case.
        protected virtual bool? EvaluateCondition(global::ProsimInterface.ProsimSdkInterface sdk, string dataRef, string condition)
        {
            try
            {
                var raw = (condition ?? "").Trim();
                string op;
                string operand;
                // Two-char operators must be checked first.
                if (raw.StartsWith("==")) { op = "=="; operand = raw.Substring(2).Trim(); }
                else if (raw.StartsWith("!=")) { op = "!="; operand = raw.Substring(2).Trim(); }
                else if (raw.StartsWith(">=")) { op = ">="; operand = raw.Substring(2).Trim(); }
                else if (raw.StartsWith("<=")) { op = "<="; operand = raw.Substring(2).Trim(); }
                else if (raw.StartsWith(">")) { op = ">"; operand = raw.Substring(1).Trim(); }
                else if (raw.StartsWith("<")) { op = "<"; operand = raw.Substring(1).Trim(); }
                else return null;

                var value = sdk.ReadDataRef(dataRef);
                if (value == null) return null;

                if (operand.Equals("true", StringComparison.OrdinalIgnoreCase) || operand.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    bool target = operand.Equals("true", StringComparison.OrdinalIgnoreCase);
                    bool actual = sdk.GetBool(dataRef);
                    return op == "==" ? actual == target : op == "!=" ? actual != target : (bool?)null;
                }

                // Numeric branch. Doubles get a small tolerance for ==/!=; ints/bytes are exact.
                if (!double.TryParse(operand, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double targetD))
                    return null;

                double actualD;
                bool isFloat = false;
                if (value is bool bv) { actualD = bv ? 1 : 0; }
                else if (value is byte by) { actualD = by; }
                else if (value is int iv) { actualD = iv; }
                else if (value is long lv) { actualD = lv; }
                else if (value is double dv) { actualD = dv; isFloat = true; }
                else if (value is float fv) { actualD = fv; isFloat = true; }
                else
                {
                    try { actualD = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); }
                    catch { return null; }
                }

                const double eps = 0.01;
                return op switch
                {
                    "==" => isFloat ? Math.Abs(actualD - targetD) < eps : actualD == targetD,
                    "!=" => isFloat ? Math.Abs(actualD - targetD) >= eps : actualD != targetD,
                    ">" => actualD > targetD,
                    ">=" => actualD >= targetD,
                    "<" => actualD < targetD,
                    "<=" => actualD <= targetD,
                    _ => (bool?)null,
                };
            }
            catch { return null; }
        }
    }
}
