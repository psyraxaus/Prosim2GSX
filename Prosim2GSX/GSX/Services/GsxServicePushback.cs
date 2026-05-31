using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu.Intents;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServicePushback(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Pushback;
        public virtual ISimResourceSubscription SubDepartService { get; protected set; }
        public virtual ISimResourceSubscription SubPushStatus { get; protected set; }
        public virtual ISimResourceSubscription SubVehiclePushbackState { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubDepartService;
        public virtual bool IsPinInserted => SubBypassPin.GetNumber() == 1;
        public virtual int PushStatus => (int)SubPushStatus.GetNumber();
        // Raw int kept for backwards compatibility with existing callers
        // (StateUpdateWorker, DebugDataService, the GsxAutomationController
        // == 12 check). New callers should prefer the strongly-typed
        // <see cref="Phase"/> property below.
        public virtual int VehiclePushbackState => (int)SubVehiclePushbackState.GetNumber();
        /// <summary>
        /// Strongly-typed view of <c>FSDT_GSX_VEHICLE_PUSHBACK_STATE</c>.
        /// Reads the raw LVAR via <see cref="SubVehiclePushbackState"/> and
        /// maps it through <see cref="PushbackPhaseExtensions.FromRaw"/>.
        /// </summary>
        public virtual PushbackPhase Phase
            => PushbackPhaseExtensions.FromRaw(SubVehiclePushbackState?.GetNumber() ?? -1);
        public virtual string VehiclePushbackStateLabel => Phase.ToDisplayLabel();
        public virtual bool IsTugConnected => SubPushStatus.GetNumber() == 3 || SubPushStatus.GetNumber() == 4;
        public virtual bool TugAttachedOnBoarding { get; protected set; } = false;
        public virtual bool EngineStartConfirmed { get; protected set; } = false;
        public virtual ISimResourceSubscription SubBypassPin { get; protected set; }

        public event Action<GsxServicePushback> OnBypassPin;

        /// <summary>
        /// Phase 5 diagnostic record of the most recent
        /// <see cref="GsxMenu.OnPushbackDirection"/> decision. Set by
        /// <c>GsxMenu</c> when the direction menu is auto-picked; consumed and
        /// cleared by the pushback-completion follow-up emission in
        /// <see cref="OnVehiclePushbackStateChange"/>. Null when no decision
        /// is outstanding (initial state, after follow-up emission, after
        /// reset).
        /// </summary>
        public virtual PushbackDirectionDecision LastDirectionDecision { get; set; }

        protected override Task<bool> DoCall() => ExecuteIntentAsync(new RequestPushbackPrepare());

        protected override void InitSubscriptions()
        {
            SubDepartService = RegisterStateSubscription(GsxConstants.VarServiceDeparture);
            SubPushStatus = RegisterChangeSubscription(GsxConstants.VarPusbackStatus, OnPushChange);
            SubVehiclePushbackState = RegisterChangeSubscription(GsxConstants.VarVehiclePushbackState, OnVehiclePushbackStateChange);
            SubBypassPin = RegisterChangeSubscription(GsxConstants.VarBypassPin, NotifyBypassPin);
        }

        // Retained as a thin shim over the enum's ToDisplayLabel so the
        // diagnostic emission below — which knows the raw int it observed
        // — keeps a single call site. New code should use
        // <c>PushbackPhaseExtensions.FromRaw(state).ToDisplayLabel()</c>
        // directly.
        protected static string MapVehiclePushbackState(int state)
            => PushbackPhaseExtensions.FromRaw(state).ToDisplayLabel();

        protected virtual void OnVehiclePushbackStateChange(ISimResourceSubscription sub, object data)
        {
            if (!IsProsimAircraft)
                return;
            // Subscription registered for its side-effects on derived
            // properties (VehiclePushbackState / Label) — no main-Logger
            // emission needed; the engine-start gate in
            // GsxAutomationController logs once when it actually fires
            // Confirm good engine start.

            // Phase 5: pushback-direction follow-up. PushbackPhase.Disconnecting
            // and PushbackPhase.ClearToStart indicate the physical push is
            // over; emit a diagnostic comparing our recorded decision to
            // the post-push observed state. One-shot — clear the decision
            // so a subsequent transition (re-attach, etc.) doesn't re-fire.
            var state = (int)sub.GetNumber();
            var phase = PushbackPhaseExtensions.FromRaw(state);
            if ((phase == PushbackPhase.Disconnecting || phase == PushbackPhase.ClearToStart)
                && LastDirectionDecision != null)
            {
                var decision = LastDirectionDecision;
                LastDirectionDecision = null;
                try { EmitPushbackDirectionFollowup(decision, state); }
                catch (Exception ex) { Logger.LogException(ex); }
            }
        }

        private void EmitPushbackDirectionFollowup(PushbackDirectionDecision decision, int observedState)
        {
            var diag = Controller?.GsxMenuDiagnosticLog;
            if (diag == null || decision == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("Pushback completion follow-up");
            sb.Append("  Decision at: ")
              .AppendLine(decision.At.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
            sb.Append("  Pushback state: ")
              .Append(observedState).Append(" (")
              .Append(MapVehiclePushbackState(observedState)).AppendLine(")");
            sb.Append("  Preference: ").AppendLine(decision.Preference.ToString());
            sb.Append("  Strategy: ").AppendLine(decision.Strategy ?? "<null>");
            sb.Append("  Selected entry: ").AppendLine(decision.SelectedEntryText ?? "<none>");
            if (decision.ParsedSelectedHeading.HasValue)
                sb.Append("  Parsed selected bearing: ")
                  .Append(decision.ParsedSelectedHeading.Value.ToString("F1", CultureInfo.InvariantCulture))
                  .AppendLine("°");
            else
                sb.AppendLine("  Parsed selected bearing: unparseable");
            sb.AppendLine("  Heading delta from start: unknown (no SimConnect heading source wired)");
            sb.AppendLine("  Verdict: unknown — bearing-vs-prediction analysis disabled until a heading source is wired");

            diag.LogDiagnostic("pushback-direction-followup", sb.ToString());
        }

        protected virtual void OnPushChange(ISimResourceSubscription sub, object data)
        {
            if (!IsProsimAircraft)
                return;

            var state = (int)sub.GetNumber();
            if (!TugAttachedOnBoarding && state > 0 && (Controller.GsxServices[GsxServiceType.Boarding].State == GsxServiceState.Active || Controller.GsxServices[GsxServiceType.Boarding].State == GsxServiceState.Requested))
            {
                Logger.Information($"Tug attaching during Boarding");
                TugAttachedOnBoarding = true;
                Controller.Menu.SuppressMenuRefresh = false;
            }
        }

        protected virtual void NotifyBypassPin(ISimResourceSubscription sub, object data)
        {
            if (!IsProsimAircraft)
                return;

            TaskTools.RunLogged(() => OnBypassPin?.Invoke(this), Controller.Token);
        }

        protected override void DoReset()
        {
            TugAttachedOnBoarding = false;
            EngineStartConfirmed = false;
            Controller.PushbackDirectionAutoSelected = false;
            // Phase 5: drop any pending direction-decision context so a
            // reset (turnaround, profile change) does not leak the previous
            // cycle's data into a future pushback-direction-followup row.
            LastDirectionDecision = null;
        }

        public override async Task Call()
        {
            if (PushStatus == 0 || !IsCalled)
                await base.Call();
            else if (PushStatus > 0 && PushStatus < 5)
            {
                // Mid-pushback re-open: the menu is already on the gate menu
                // and we need to re-press "Request Pushback" (line 5) without
                // hiding first. RequestPushbackPrepare can't be reused — its
                // precondition rejects non-Callable states. Open(true) waits
                // for menu-ready without the Hide, mirroring NoHide=true.
                if (await Controller.Menu.Open(true) == false)
                    return;
                if (!Controller.Menu.MatchTitle(GsxConstants.MenuGate))
                {
                    Logger.Warning($"Pushback mid-reopen aborted - title '{Controller.Menu.MenuTitle}' does not start with '{GsxConstants.MenuGate}'");
                    return;
                }
                await Controller.Menu.Select(5, waitReady: true);
            }
        }

        public virtual async Task EndPushback(int selection = 1)
        {
            Logger.Debug($"End Pushback ({PushStatus})");
            if (PushStatus < 5)
                return;

            if (await Controller.Menu.OpenHide() == false)
                return;
            if (!Controller.Menu.MatchTitle(GsxConstants.MenuPushbackInterrupt))
            {
                Logger.Warning($"EndPushback aborted - title '{Controller.Menu.MenuTitle}' does not start with '{GsxConstants.MenuPushbackInterrupt}'");
                return;
            }
            await Controller.Menu.Select(selection, waitReady: true);
            await Task.Delay(Controller.Config.MenuCheckInterval * 2, Controller.Token);
        }

        // Sends "Interrupt pushback" → "Confirm good engine start" (menu position 1)
        // after the physical push has completed and the crew set the parking brake.
        // GSX shows this option on the Interrupt menu once it's waiting for engine
        // confirmation; selecting it lets the tug detach safely.
        // Caller is responsible for gating on push state, brakes, and engines —
        // this method only de-duplicates and ensures the tug is still attached.
        public virtual async Task ConfirmEngineStart()
        {
            if (EngineStartConfirmed)
                return;
            if (PushStatus == 0)
                return;

            Logger.Information($"Confirm good engine start ({PushStatus})");
            EngineStartConfirmed = true;

            if (await Controller.Menu.OpenHide() == false)
                return;
            if (!Controller.Menu.MatchTitle(GsxConstants.MenuPushbackInterrupt))
            {
                Logger.Warning($"ConfirmEngineStart aborted - title '{Controller.Menu.MenuTitle}' does not start with '{GsxConstants.MenuPushbackInterrupt}'");
                return;
            }
            await Controller.Menu.Select(1, waitReady: true);
            await Task.Delay(Controller.Config.MenuCheckInterval * 2, Controller.Token);
        }
    }
}
