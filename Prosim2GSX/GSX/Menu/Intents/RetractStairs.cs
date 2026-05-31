using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Gate-menu retract intent for passenger stairs. Parallel to
    /// <see cref="RetractJetway"/> — same menu line as
    /// <see cref="RequestStairs"/>, inverted IsAlreadySatisfied so
    /// <c>GsxServiceStairs.Remove()</c> actually presses the menu when the
    /// service is Active. See <see cref="RetractJetway"/> for the full
    /// rationale.
    /// </summary>
    internal sealed class RetractStairs : KeywordIntent
    {
        public RetractStairs()
            : base(@"^operate stairs")
        { }

        public override GsxMenuIntent ParentMenu => new OpenGateMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuGate;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Preparation
            || phase == AutomationState.Departure
            || phase == AutomationState.PushBack
            || phase == AutomationState.Arrival
            || phase == AutomationState.TurnAround;

        public override bool ArePreconditionsSatisfied(GsxController controller)
        {
            var stairs = IntentHelpers.GetService<GsxServiceStairs>(controller, GsxServiceType.Stairs);
            return stairs != null && stairs.IsAvailable && stairs.State == GsxServiceState.Active;
        }

        public override bool IsAlreadySatisfied(GsxController controller)
        {
            var stairs = IntentHelpers.GetService<GsxServiceStairs>(controller, GsxServiceType.Stairs);
            return stairs == null || stairs.State != GsxServiceState.Active;
        }

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            // Same fire-and-confirm pattern as RetractJetway — see that
            // class for the rationale.
            return Task.FromResult(true);
        }

        public override string Describe() => "Retract the gate passenger stairs";
    }
}
