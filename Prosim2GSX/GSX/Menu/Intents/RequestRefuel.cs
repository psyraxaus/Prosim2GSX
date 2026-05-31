using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Gate-menu service intent: request GSX refuel service. Refuel has two
    /// state quirks the verification logic accommodates:
    ///   1. <c>FSDT_GSX_REFUELING_STATE</c> uses <c>1</c> as its "completed"
    ///      value (<see cref="GsxServiceRefuel.NumStateCompleted"/>=1), not the
    ///      usual <c>6</c>. <c>GsxServiceRefuel.GetState()</c> maps that into
    ///      <see cref="GsxServiceState.Completed"/> after WasActive is observed,
    ///      so reading <c>service.State</c> is correct here.
    ///   2. The hose-connected LVAR (<c>FSDT_GSX_FUELHOSE_CONNECTED</c>) goes
    ///      to <c>1</c> at hose attach — sometimes before the service state
    ///      transitions out of Callable. The precondition guards against
    ///      re-requesting while the hose is already connected, and the verify
    ///      probe accepts either the state transition or hose-connected as
    ///      evidence the request landed.
    /// </summary>
    internal sealed class RequestRefuel : KeywordIntent
    {
        public RequestRefuel()
            : base(@"(?i)^request refuel")
        { }

        public override GsxMenuIntent ParentMenu => new OpenGateMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuGate;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Preparation
            || phase == AutomationState.Departure
            || phase == AutomationState.TurnAround;

        public override bool ArePreconditionsSatisfied(GsxController controller)
        {
            var refuel = IntentHelpers.GetService<GsxServiceRefuel>(controller, GsxServiceType.Refuel);
            if (refuel == null) return false;
            return refuel.State == GsxServiceState.Callable && !refuel.IsRefueling;
        }

        public override bool IsAlreadySatisfied(GsxController controller)
        {
            // Refuel exposes Completed via GsxServiceRefuel.GetState's
            // NumStateCompleted=1 mapping; the broader "in flight or done"
            // semantic also covers Requested/Active (mid-refuel, hose
            // attached). See RequestDeboarding for the rationale.
            var refuel = IntentHelpers.GetService<GsxServiceRefuel>(controller, GsxServiceType.Refuel);
            if (refuel == null) return false;
            var state = refuel.State;
            return state == GsxServiceState.Requested
                || state == GsxServiceState.Active
                || state == GsxServiceState.Completed;
        }

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () =>
                {
                    var refuel = IntentHelpers.GetService<GsxServiceRefuel>(controller, GsxServiceType.Refuel);
                    if (refuel == null) return false;
                    return refuel.State == GsxServiceState.Requested
                        || refuel.State == GsxServiceState.Active
                        || refuel.IsRefueling;
                },
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Request GSX refueling service";
    }
}
