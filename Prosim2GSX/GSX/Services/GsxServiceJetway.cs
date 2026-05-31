using CFIT.SimConnectLib.SimResources;
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

        /// <summary>
        /// Strongly-typed view of <c>FSDT_GSX_JETWAY_OPERATION</c>. Reads
        /// the raw LVAR via <see cref="SubOperating"/> and maps it through
        /// <see cref="JetwayOperationExtensions.FromRaw"/>.
        /// </summary>
        public virtual JetwayOperation Operation
            => JetwayOperationExtensions.FromRaw(SubOperating?.GetNumber() ?? 0);

        public virtual bool IsAvailable => State != GsxServiceState.NotAvailable;
        // Behaviour preserved from the pre-enum implementation:
        //   IsConnected = state == Active AND operation < 3 (idle / docked).
        //   IsOperating = state == Requested OR operation > 3 (in motion).
        // The enum extension methods encode the same gates with named
        // categories so the intent matches the code's vocabulary.
        public virtual bool IsConnected => State == GsxServiceState.Active && Operation.IsIdle();
        public virtual bool IsOperating => State == GsxServiceState.Requested || Operation.IsInMotion();

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
                LastCallResult = true;
                return true;
            }
            return await ExecuteIntentAsync(new RequestJetway());
        }

        /// <summary>
        /// Retracts the jetway when GSX considers the service Active.
        ///
        /// <para>
        /// Routes through <see cref="RetractJetway"/> rather than
        /// <see cref="RequestJetway"/>: the request intent's
        /// <c>IsAlreadySatisfied</c> returns true on Active and would
        /// short-circuit without pressing the menu — which historically
        /// meant the retract silently no-op'd whenever the GSX operation
        /// LVAR was stuck (a known v4 Remote Control symptom). The retract
        /// intent inverts the predicate so the menu is actually clicked.
        /// </para>
        ///
        /// <para>
        /// The Operation LVAR is intentionally NOT consulted here. The
        /// state machine ("is GSX in the Active service state?") is the
        /// authoritative signal for "is there something to retract"; the
        /// animation LVAR is GSX's internal positioning hint and can
        /// disagree with reality.
        /// </para>
        /// </summary>
        public virtual async Task Remove()
        {
            if (!IsAvailable) return;
            if (State != GsxServiceState.Active) return;
            await ExecuteIntentAsync(new RetractJetway(), handleOperatorPicker: false);
        }
    }
}
