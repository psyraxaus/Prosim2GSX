using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.GSX.Menu;
using Prosim2GSX.GSX.Menu.Intents;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public class GsxServiceJetway(GsxController controller) : GsxService(controller)
    {

        public override GsxServiceType Type => GsxServiceType.Jetway;
        public virtual ISimResourceSubscription SubService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubService;
        public virtual ISimResourceSubscription SubOperating { get; protected set; }

        public virtual bool IsAvailable => State != GsxServiceState.NotAvailable;
        public virtual bool IsConnected => SubService.GetNumber() == (int)GsxServiceState.Active && SubOperating.GetNumber() < 3;
        public virtual bool IsOperating => SubService.GetNumber() == (int)GsxServiceState.Requested || SubOperating.GetNumber() > 3;

        // Phase 3+ migration: DoCall routes through the RequestJetway intent.
        // Legacy sequence preserved as a dead placeholder; Phase 6 removes both.
        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(6, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubService = RegisterStateSubscription(GsxConstants.VarServiceJetway);
            SubOperating = RegisterReadOnlyVariable(GsxConstants.VarServiceJetwayOperation);
        }

        protected override void DoReset()
        {

        }

        protected override bool CheckCalled()
        {
            return IsOperating || IsRunning;
        }

        protected override async Task<bool> DoCall()
        {
            if (!IsAvailable)
            {
                // Preserve the legacy "no-op success when not available" behaviour.
                // The RequestJetway intent's precondition would map this to
                // StatePreconditionFailed (a real failure), which would change
                // caller-visible semantics — guarding at the override level keeps
                // the existing IsAvailable=false path silently successful.
                if (CallSequence != null) CallSequence.IsSuccess = true;
                return true;
            }
            return await ExecuteIntentAsync(new RequestJetway());
        }

        public virtual async Task Remove()
        {
            if (!IsConnected || !IsAvailable || IsOperating)
                return;

            await DoCall();
        }
    }
}
