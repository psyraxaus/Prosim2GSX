using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceWater(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Water;
        protected override double NumStateCompleted { get; } = 1;
        public virtual ISimResourceSubscription SubWaterService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubWaterService;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(8, GsxConstants.MenuGate, true));
            var additional = new GsxMenuCommand(4, GsxConstants.MenuAdditionalServices) { WaitReady = true };
            additional.AlternateTitles.Add(GsxConstants.MenuGate);
            sequence.Commands.Add(additional);
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateReset());

            return sequence;
        }

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
