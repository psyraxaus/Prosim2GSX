using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Gate-menu service intent: operate the gate jetway. The anchored regex
    /// (<c>^operate jetway</c>) deliberately does not match "No Jetways here"
    /// — the GSX 4.0.0 text variant at airports with no jetway. When that
    /// variant is what entry 6 of the gate menu reads, the resolver returns
    /// <see cref="MenuOutcome.ItemNotAvailable"/> and the caller treats it as
    /// a benign skip (no jetway, no operation). The legacy fixed-index code
    /// would have written choice 5 (jetway slot) regardless, which on a
    /// no-jetway stand would still hit "No Jetways here" (a harmless no-op
    /// today, but only by accident); intent matching makes the absence
    /// explicit.
    /// </summary>
    internal sealed class RequestJetway : KeywordIntent
    {
        public RequestJetway()
            : base(@"^operate jetway")
        { }

        public override GsxMenuIntent ParentMenu => new OpenGateMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuGate;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Preparation
            || phase == AutomationState.Arrival
            || phase == AutomationState.TurnAround;

        public override bool ArePreconditionsSatisfied(GsxController controller)
        {
            var jetway = IntentHelpers.GetService<GsxServiceJetway>(controller, GsxServiceType.Jetway);
            return jetway != null && jetway.IsAvailable && !jetway.IsOperating;
        }

        public override bool IsAlreadySatisfied(GsxController controller)
        {
            // Both "connected" and "currently moving" are "no menu action
            // needed" — without IsOperating here the resolver returns
            // StatePreconditionFailed during a jetway-in-motion tick and the
            // automation re-fires Call() every 500ms (same spam class as
            // RequestDeboarding's Active-state bug).
            var jetway = IntentHelpers.GetService<GsxServiceJetway>(controller, GsxServiceType.Jetway);
            return jetway != null && (jetway.IsConnected || jetway.IsOperating);
        }

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () =>
                {
                    var jetway = IntentHelpers.GetService<GsxServiceJetway>(controller, GsxServiceType.Jetway);
                    return jetway != null && (jetway.IsOperating || jetway.IsConnected);
                },
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Operate the gate jetway";
    }
}
