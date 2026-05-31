using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu.Intents;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceCleaning(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Cleaning;
        protected override double NumStateCompleted { get; } = 1;
        public virtual ISimResourceSubscription SubCleaningService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubCleaningService;

        protected override Task<bool> DoCall() => ExecuteIntentAsync(new RequestCleaning());

        protected override void InitSubscriptions()
        {
            SubCleaningService = RegisterStateSubscription(GsxConstants.VarServiceCleaning);
        }

        protected override bool CheckCalled()
        {
            return base.CheckCalled() || SequenceResult;
        }

        protected override void DoReset()
        {

        }

        protected override void RunStateRequested()
        {
            base.RunStateRequested();
            WasActive = true;
            NotifyActive();
        }

        protected override void RunStateActive()
        {
            if (!WasActive)
            {
                WasActive = true;
                NotifyActive();
            }
        }
    }
}
