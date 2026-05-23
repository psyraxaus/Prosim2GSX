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

        /// <summary>
        /// Strongly-typed view of <c>FSDT_GSX_STAIRS_OPERATION</c>. See
        /// <see cref="JetwayOperation"/> for the shared semantics.
        /// </summary>
        public virtual JetwayOperation Operation
            => JetwayOperationExtensions.FromRaw(SubOperating?.GetNumber() ?? 0);

        public virtual bool IsAvailable => State != GsxServiceState.NotAvailable;
        // Behaviour preserved from the pre-enum implementation; same
        // semantic notes as GsxServiceJetway.
        public virtual bool IsConnected => State == GsxServiceState.Active && Operation.IsIdle();
        public virtual bool IsOperating => State == GsxServiceState.Requested || Operation.IsInMotion();

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

        /// <summary>
        /// Retracts the stairs when GSX considers the service Active.
        /// Routes through <see cref="RetractStairs"/> for the same reason
        /// <see cref="GsxServiceJetway.Remove"/> uses
        /// <see cref="RetractJetway"/> — see that method for the rationale.
        /// </summary>
        public virtual async Task Remove()
        {
            if (!IsAvailable) return;
            if (State != GsxServiceState.Active) return;
            await ExecuteIntentAsync(new RetractStairs(), handleOperatorPicker: false);
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
