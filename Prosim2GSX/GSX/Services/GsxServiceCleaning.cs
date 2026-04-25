using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceCleaning(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Cleaning;
        protected override double NumStateCompleted { get; } = 1;
        public virtual ISimResourceSubscription SubCleaningService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubCleaningService;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(8, GsxConstants.MenuGate, true));
            var additional = new GsxMenuCommand(5, GsxConstants.MenuAdditionalServices) { WaitReady = true };
            additional.AlternateTitles.Add(GsxConstants.MenuGate);
            sequence.Commands.Add(additional);
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateReset());

            return sequence;
        }

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
