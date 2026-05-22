using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Interrupt-pushback intent: choose "Change Direction" — only present on
    /// the pre-push + redirect variant of the interrupt menu. Verification
    /// waits for the title to transition to
    /// <see cref="GsxConstants.MenuPushbackDirection"/>, which is GSX's
    /// follow-up screen.
    /// </summary>
    internal sealed class ChangePushbackDirection : KeywordIntent
    {
        public ChangePushbackDirection()
            : base(@"(?i)^change direction")
        { }

        public override GsxMenuIntent ParentMenu => null;
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuPushbackInterrupt;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Departure || phase == AutomationState.PushBack;

        public override bool ArePreconditionsSatisfied(GsxController controller) => true;

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () => controller?.Menu != null
                      && controller.Menu.MatchTitle(GsxConstants.MenuPushbackDirection),
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Choose to change the pushback direction (reopens direction menu)";
    }
}
