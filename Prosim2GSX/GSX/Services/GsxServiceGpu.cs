using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu;
using Prosim2GSX.GSX.Menu.Intents;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceGpu(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.GPU;
        public virtual ISimResourceSubscription SubGpuService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubGpuService;

        // Phase 4 migration: DoCall routes through the RequestGpu intent. The
        // intent's ParentMenu chain (OpenAdditionalServicesMenu → OpenGateMenu)
        // navigates without any AlternateTitles.Add(MenuGate) escape — a slow
        // submenu transition leaves the title on MenuGate and the resolver
        // returns MenuTitleMismatch instead of writing choice 0 on the gate
        // menu (which would land on "Request Deboarding").
        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(8, GsxConstants.MenuGate, true));
            var additional = new GsxMenuCommand(1, GsxConstants.MenuAdditionalServices) { WaitReady = true };
            additional.AlternateTitles.Add(GsxConstants.MenuGate);
            sequence.Commands.Add(additional);
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateReset());

            return sequence;
        }

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
