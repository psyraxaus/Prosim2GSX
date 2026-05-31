using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu.Intents;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceDeice(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Deice;
        public virtual ISimResourceSubscription SubDeiceService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubDeiceService;

        protected override Task<bool> DoCall() => ExecuteIntentAsync(new RequestDeice());

        protected override void InitSubscriptions()
        {
            SubDeiceService = RegisterStateSubscription(GsxConstants.VarServiceDeice);
        }

        protected override void DoReset()
        {

        }
    }
}
