using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Interrupt-pushback intent: abort the active pushback. Only present on
    /// the active-pushback variant of the interrupt menu.
    /// </summary>
    internal sealed class AbortPushback : KeywordIntent
    {
        public AbortPushback()
            : base(@"(?i)^abort pushback")
        { }

        public override GsxMenuIntent ParentMenu => null;
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuPushbackInterrupt;

        public override bool IsValidForPhase(AutomationState phase) => phase == AutomationState.PushBack;

        public override bool ArePreconditionsSatisfied(GsxController controller) => true;

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () => controller?.Menu != null
                      && !controller.Menu.MatchTitle(GsxConstants.MenuPushbackInterrupt),
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Abort the active pushback";
    }
}
