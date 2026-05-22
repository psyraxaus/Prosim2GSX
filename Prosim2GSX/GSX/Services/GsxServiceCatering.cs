using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu;
using Prosim2GSX.GSX.Menu.Intents;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceCatering(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Catering;
        public virtual ISimResourceSubscription SubCaterService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubCaterService;

        // Phase 3+ migration: DoCall routes through the RequestCatering intent.
        // The legacy CallSequence is preserved as a (dead) placeholder so the
        // SequenceResult-based fallbacks remain wired; Phase 6 removes both.
        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(2, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

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
