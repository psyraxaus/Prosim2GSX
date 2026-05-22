using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu;
using Prosim2GSX.GSX.Menu.Intents;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceDeice(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Deice;
        public virtual ISimResourceSubscription SubDeiceService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubDeiceService;

        // Phase 4 migration: DoCall routes through the RequestDeice intent.
        // AlternateTitles.Add(MenuGate) is dead in this code path — see GsxServiceGpu
        // for the structural argument.
        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(8, GsxConstants.MenuGate, true));
            var additional = new GsxMenuCommand(2, GsxConstants.MenuAdditionalServices) { WaitReady = true };
            additional.AlternateTitles.Add(GsxConstants.MenuGate);
            sequence.Commands.Add(additional);
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

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
