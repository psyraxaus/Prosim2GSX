using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu;
using System;
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

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(5, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

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
            // properties (VehiclePushbackState / Label) — no log emission
            // needed; the engine-start gate in GsxAutomationController
            // logs once when it actually fires Confirm good engine start.
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
        }

        public override async Task Call()
        {
            if (PushStatus == 0 || !IsCalled)
                await base.Call();
            else if (PushStatus > 0 && PushStatus < 5)
            {
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
