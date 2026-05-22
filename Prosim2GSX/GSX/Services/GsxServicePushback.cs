using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu;
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
        public virtual int VehiclePushbackState => (int)SubVehiclePushbackState.GetNumber();
        public virtual string VehiclePushbackStateLabel => MapVehiclePushbackState(VehiclePushbackState);
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

        // Phase 3 migration: the primary Call() path (PushStatus == 0 || !IsCalled
        // branch in Call() below) now routes through the RequestPushbackPrepare
        // intent. The legacy InitCallSequence is preserved as a dead placeholder.
        // EndPushback / ConfirmEngineStart and the mid-pushback re-open branch
        // of Call() are intentionally NOT migrated here — Phase 4 handles them.
        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(5, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override Task<bool> DoCall() => ExecuteIntentAsync(new RequestPushbackPrepare());

        protected override void InitSubscriptions()
        {
            SubDepartService = RegisterStateSubscription(GsxConstants.VarServiceDeparture);
            SubPushStatus = RegisterChangeSubscription(GsxConstants.VarPusbackStatus, OnPushChange);
            SubVehiclePushbackState = RegisterChangeSubscription(GsxConstants.VarVehiclePushbackState, OnVehiclePushbackStateChange);
            SubBypassPin = RegisterChangeSubscription(GsxConstants.VarBypassPin, NotifyBypassPin);
        }

        protected static string MapVehiclePushbackState(int state) => state switch
        {
            8 => "Pushing back",
            11 => "Waiting for engine shutdown",
            12 => "Awaiting engine start confirmation",
            13 => "Disconnecting",
            14 => "Clear to start",
            0 => "Idle",
            _ => $"State {state}",
        };

        protected virtual void OnVehiclePushbackStateChange(ISimResourceSubscription sub, object data)
        {
            if (!IsProsimAircraft)
                return;
            // Subscription registered for its side-effects on derived
            // properties (VehiclePushbackState / Label) — no main-Logger
            // emission needed; the engine-start gate in
            // GsxAutomationController logs once when it actually fires
            // Confirm good engine start.

            // Phase 5: pushback-direction follow-up. States 13 (Disconnecting)
            // and 14 (Clear to start) indicate the physical push is over;
            // emit a diagnostic comparing our recorded decision to the post-
            // push observed state. One-shot — clear the decision so a
            // subsequent transition (re-attach, etc.) doesn't re-fire.
            var state = (int)sub.GetNumber();
            if ((state == 13 || state == 14) && LastDirectionDecision != null)
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
                // PHASE 4 TODO: this mid-pushback re-open branch still constructs
                // a GsxMenuSequence directly. It can't reuse RequestPushbackPrepare
                // (its precondition rejects non-Callable states). Either define a
                // ReopenPushbackMenu intent or move this branch into Phase 4's
                // interrupt-pushback migration. Tracked in the design recap
                // (Section G item 5).
                var sequence = new GsxMenuSequence();
                sequence.Commands.Add(new(5, GsxConstants.MenuGate, true) { NoHide = true });
                await Controller.Menu.RunSequence(sequence);
            }
        }

        public virtual async Task EndPushback(int selection = 1)
        {
            Logger.Debug($"End Pushback ({PushStatus})");
            if (PushStatus < 5)
                return;

            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(selection, GsxConstants.MenuPushbackInterrupt, true));
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());
            await Controller.Menu.RunSequence(sequence);
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

            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(1, GsxConstants.MenuPushbackInterrupt, true));
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());
            await Controller.Menu.RunSequence(sequence);
        }
    }
}
