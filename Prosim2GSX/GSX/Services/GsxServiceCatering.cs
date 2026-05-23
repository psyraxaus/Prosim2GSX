using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu.Intents;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceCatering(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Catering;
        public virtual ISimResourceSubscription SubCaterService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubCaterService;

        protected override Task<bool> DoCall() => ExecuteIntentAsync(new RequestCatering());

        protected override void InitSubscriptions()
        {
            SubCaterService = RegisterStateSubscription(GsxConstants.VarServiceCatering);
        }

        protected override void DoReset()
        {

        }
    }
}
