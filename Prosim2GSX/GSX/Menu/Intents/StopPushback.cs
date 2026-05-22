using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Interrupt-pushback intent: choose "Stop here and complete pushback
    /// procedure". Only present on the active-pushback variant of the
    /// interrupt menu. The pattern matches the leading "Stop here" — the
    /// rest of GSX's entry text is descriptive and may vary in punctuation.
    /// Verification is title change (menu closes) — the
    /// <c>FSDT_GSX_VEHICLE_PUSHBACK_STATE</c> transition is less reliable as
    /// a signal because the value sequence overlaps with normal pushback
    /// progress.
    /// </summary>
    internal sealed class StopPushback : KeywordIntent
    {
        public StopPushback()
            : base(@"(?i)^stop here")
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

        public override string Describe() => "Stop the pushback here and complete the procedure";
    }
}
