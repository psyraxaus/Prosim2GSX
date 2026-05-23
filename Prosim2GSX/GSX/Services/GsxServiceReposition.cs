using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu.Intents;
using System;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceReposition(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Reposition;
        protected override ISimResourceSubscription SubStateVar => null;

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
                LastCallResult = false;
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
                LastCallResult = false;
                return false;
            }

            // Steps 3-4: mirror the legacy Dummy + Dummy + Reset tail. The
            // legacy timing was 2*MenuCheckInterval + 2*MenuCheckInterval +
            // 1*MenuCheckInterval before OpenHide; keep the same total.
            await Task.Delay(Controller.Config.MenuCheckInterval * 5, Controller.Token);
            await Controller.Menu.OpenHide();

            LastCallResult = true;
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
