using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu.Intents;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceStairs(GsxController controller) : GsxService(controller)
    {

        public override GsxServiceType Type => GsxServiceType.Stairs;
        public virtual ISimResourceSubscription SubService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubService;
        public virtual ISimResourceSubscription SubOperating { get; protected set; }

        public virtual bool IsAvailable => State != GsxServiceState.NotAvailable;
        public virtual bool IsConnected => SubService.GetNumber() == (int)GsxServiceState.Active && SubOperating.GetNumber() < 3;
        public virtual bool IsOperating => SubService.GetNumber() == (int)GsxServiceState.Requested || SubOperating.GetNumber() > 3;

        protected override void InitSubscriptions()
        {
            SubService = RegisterStateSubscription(GsxConstants.VarServiceStairs);
            SubOperating = RegisterReadOnlyVariable(GsxConstants.VarServiceStairsOperation);
        }

        protected override void DoReset()
        {

        }

        protected override bool CheckCalled()
        {
            return IsOperating || IsRunning;
        }

        protected override async Task<bool> DoCall()
        {
            if (!IsAvailable)
            {
                // Same legacy-preservation guard as Jetway — see that class for details.
                LastCallResult = true;
                return true;
            }
            return await ExecuteIntentAsync(new RequestStairs());
        }

        public virtual async Task Remove()
        {
            if (!IsConnected || !IsAvailable || IsOperating)
                return;

            await DoCall();
        }

        protected override void OnStateChange(ISimResourceSubscription sub, object data)
        {
            base.OnStateChange(sub, data);
            if (State == GsxServiceState.Callable && IsCalled)
            {
                Logger.Debug($"Reset IsCalled for Stairs");
                IsCalled = false;
            }
        }
    }
}
