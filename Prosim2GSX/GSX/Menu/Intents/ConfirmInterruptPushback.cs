using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Interrupt-pushback intent: answer "Yes" on the GSX interrupt-pushback
    /// menu. Per the Phase 1 archaeology (Capture H), the
    /// <see cref="GsxConstants.MenuPushbackInterrupt"/> title is reused by
    /// three structurally different menus — pre-push confirm
    /// (Yes/No), pre-push + redirect (Yes/No/Change Direction), and active
    /// pushback (Pause/Stop here/Abort). The anchored <c>^yes$</c> pattern
    /// only matches in the pre-push variants; if this intent fires while the
    /// active-pushback variant is up, the resolver returns
    /// <see cref="MenuOutcome.ItemNotAvailable"/> rather than blindly
    /// selecting "Pause pushback".
    ///
    /// <see cref="ParentMenu"/> is <c>null</c> because GSX raises the
    /// interrupt menu itself in response to a pushback-related event; we
    /// don't navigate to it.
    /// </summary>
    internal sealed class ConfirmInterruptPushback : KeywordIntent
    {
        public ConfirmInterruptPushback()
            : base(@"(?i)^yes$")
        { }

        public override GsxMenuIntent ParentMenu => null;
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuPushbackInterrupt;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.PushBack || phase == AutomationState.Departure;

        public override bool ArePreconditionsSatisfied(GsxController controller) => true;

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            // Interrupt menu closes → title moves off MenuPushbackInterrupt.
            return IntentHelpers.PollUntilAsync(
                () => controller?.Menu != null
                      && !controller.Menu.MatchTitle(GsxConstants.MenuPushbackInterrupt),
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Confirm 'yes' to interrupt pushback";
    }
}
