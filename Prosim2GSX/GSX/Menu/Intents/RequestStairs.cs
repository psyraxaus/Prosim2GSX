using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Gate-menu service intent: operate the gate passenger stairs. The
    /// anchored regex (<c>^operate stairs</c>) deliberately does not match
    /// "Stairs disabled" — the GSX 4.0.0 variant text at airports where
    /// stairs are unavailable. As with <see cref="RequestJetway"/>, the
    /// non-match path returns <see cref="MenuOutcome.ItemNotAvailable"/> and
    /// the caller treats it as a benign skip.
    /// </summary>
    internal sealed class RequestStairs : KeywordIntent
    {
        public RequestStairs()
            : base(@"^operate stairs")
        { }

        public override GsxMenuIntent ParentMenu => new OpenGateMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuGate;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Preparation
            || phase == AutomationState.Arrival
            || phase == AutomationState.TurnAround;

        public override bool ArePreconditionsSatisfied(GsxController controller)
        {
            var stairs = IntentHelpers.GetService<GsxServiceStairs>(controller, GsxServiceType.Stairs);
            return stairs != null && stairs.IsAvailable && !stairs.IsOperating;
        }

        public override bool IsAlreadySatisfied(GsxController controller)
        {
            var stairs = IntentHelpers.GetService<GsxServiceStairs>(controller, GsxServiceType.Stairs);
            return stairs != null && stairs.IsConnected;
        }

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () =>
                {
                    var stairs = IntentHelpers.GetService<GsxServiceStairs>(controller, GsxServiceType.Stairs);
                    return stairs != null && (stairs.IsOperating || stairs.IsConnected);
                },
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Operate the gate passenger stairs";
    }
}
