using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu.Intents;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceWater(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Water;
        protected override double NumStateCompleted { get; } = 1;
        public virtual ISimResourceSubscription SubWaterService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubWaterService;

        protected override Task<bool> DoCall() => ExecuteIntentAsync(new RequestWater());

        protected override void InitSubscriptions()
        {
            SubWaterService = RegisterStateSubscription(GsxConstants.VarServiceWater);
        }

        protected override bool CheckCalled()
        {
            return base.CheckCalled() || SequenceResult;
        }

        protected override void DoReset()
        {

        }
    }
}
