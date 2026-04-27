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
        protected override ISimResourceSubscription SubStateVar => SubDepartService;
        public virtual bool IsPinInserted => SubBypassPin.GetNumber() == 1;
        public virtual int PushStatus => (int)SubPushStatus.GetNumber();
        public virtual bool IsTugConnected => SubPushStatus.GetNumber() == 3 || SubPushStatus.GetNumber() == 4;
        public virtual bool TugAttachedOnBoarding { get; protected set; } = false;
        public virtual bool EngineStartConfirmed { get; protected set; } = false;
        // Latches once PushStatus reaches the active-push range (≥5). Used to
        // distinguish "pre-push, tug attached and waiting for direction" from
        // "post-push, tug attached and waiting for brake/confirm".
        public virtual bool WasPushing { get; protected set; } = false;
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
            SubBypassPin = RegisterChangeSubscription(GsxConstants.VarBypassPin, NotifyBypassPin);
        }

        protected virtual int LastLoggedPushStatus { get; set; } = -1;

        protected virtual void OnPushChange(ISimResourceSubscription sub, object data)
        {
            if (!IsProsimAircraft)
                return;

            var state = (int)sub.GetNumber();
            Logger.Information($"Push Status: {LastLoggedPushStatus} -> {state} (WasPushing={WasPushing}, EngineStartConfirmed={EngineStartConfirmed})");
            LastLoggedPushStatus = state;
            if (!TugAttachedOnBoarding && state > 0 && (Controller.GsxServices[GsxServiceType.Boarding].State == GsxServiceState.Active || Controller.GsxServices[GsxServiceType.Boarding].State == GsxServiceState.Requested))
            {
                Logger.Information($"Tug attaching during Boarding");
                TugAttachedOnBoarding = true;
                Controller.Menu.SuppressMenuRefresh = false;
            }
            if (!WasPushing && state >= 5)
            {
                WasPushing = true;
                Logger.Information($"WasPushing latched at PushStatus={state}");
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
            WasPushing = false;
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
