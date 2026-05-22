using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu;
using Prosim2GSX.GSX.Menu.Intents;
using System;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceReposition(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Reposition;
        protected override ISimResourceSubscription SubStateVar => null;
        // Phase 3 migration: DoCall routes through OpenParkingSelectMenu +
        // direct Select(1) + OpenHide rather than the legacy sequence. This
        // eliminates the parkingSelect + AlternateTitles.Add(MenuGate) pattern
        // that was the smoking-gun bug — there is no longer any command that
        // can land on the gate menu's index 1 ("Request Deboarding") when the
        // submenu transition is slow. Legacy sequence preserved as a dead
        // placeholder. IgnoreGsxState becomes inert here (ExecuteIntent does
        // not consult it); the new path simply runs through the resolver's
        // standard guards.
        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(10, GsxConstants.MenuGate, true));
            var parkingSelect = new GsxMenuCommand(1, GsxConstants.MenuParkingSelect) { WaitReady = true };
            parkingSelect.AlternateTitles.Add(GsxConstants.MenuGate);
            sequence.Commands.Add(parkingSelect);
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());
            sequence.Commands.Add(GsxMenuCommand.CreateReset());
            sequence.IgnoreGsxState = true;

            return sequence;
        }

        protected override async Task<bool> DoCall()
        {
            var phase = Controller.AutomationController.State;

            // Step 1: navigate from gate menu to "Select Position at" via the
            // dedicated intent. Failure here means we never reached the
            // parking-select submenu and must NOT write a fallback choice —
            // returning false propagates the no-op upstream.
            var nav = await Controller.Menu.ExecuteIntent(new OpenParkingSelectMenu(), phase, Controller.Token);
            bool navSuccess = nav.IsSuccess || nav.IsBenignSkip;
            if (!navSuccess)
            {
                Logger.Warning($"{Type}: navigation to parking-select failed — {nav.Outcome}: {nav.Reason}");
                if (CallSequence != null) CallSequence.IsSuccess = false;
                return false;
            }

            // Step 2: preserve the legacy "Select(1) on the parking-select
            // submenu" behaviour. The legacy flow had no gate-aware logic
            // here — GsxParkingSelector is a separate path used by
            // GsxController.SetArrivalParkingAsync. Phase 3 keeps this
            // byte-for-byte to avoid scope creep; a future enhancement could
            // route arrival-gate-aware reposition through ParkingSelector.
            try
            {
                await Controller.Menu.Select(1, waitReady: false);
            }
            catch (Exception ex)
            {
                Logger.Warning($"{Type}: parking-select item 1 write failed — {ex.Message}");
                if (CallSequence != null) CallSequence.IsSuccess = false;
                return false;
            }

            // Steps 3-4: mirror the legacy Dummy + Dummy + Reset tail. The
            // legacy timing was 2*MenuCheckInterval + 2*MenuCheckInterval +
            // 1*MenuCheckInterval before OpenHide; keep the same total.
            await Task.Delay(Controller.Config.MenuCheckInterval * 5, Controller.Token);
            await Controller.Menu.OpenHide();

            if (CallSequence != null) CallSequence.IsSuccess = true;
            return true;
        }

        protected override void InitSubscriptions()
        {

        }

        protected override void DoReset()
        {

        }

        protected override GsxServiceState GetState()
        {
            if (SequenceResult)
                return GsxServiceState.Completed;
            else
                return GsxServiceState.Callable;
        }

        protected override bool CheckCalled()
        {
            return SequenceResult;
        }

        protected override void SetStateVariable(GsxServiceState state)
        {

        }
    }
}
