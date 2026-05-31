using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu.Intents;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceGpu(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.GPU;
        public virtual ISimResourceSubscription SubGpuService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubGpuService;

        protected override Task<bool> DoCall() => ExecuteIntentAsync(new RequestGpu());

        protected override void InitSubscriptions()
        {
            SubGpuService = RegisterStateSubscription(GsxConstants.VarServiceGpu);
        }

        protected override void DoReset()
        {

        }
    }
}
