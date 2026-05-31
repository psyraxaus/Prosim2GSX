using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Menu-callback intent: answer the GSX "do you request the de-icing
    /// treatment" question. Constructed with a <c>bool accept</c>.
    /// The existing <see cref="GsxMenu.OnDeiceQuestion"/> auto-answers Yes
    /// (item 1) only when <c>Config.AutoDeiceEnabled</c> is true and the
    /// question hasn't been answered yet; that gating moves to the caller
    /// invoking this intent — the intent itself remains pure ("answer
    /// yes/no").
    /// </summary>
    internal sealed class AnswerDeIceQuestion : KeywordIntent
    {
        private readonly bool _accept;

        public AnswerDeIceQuestion(bool accept)
            : base(accept ? @"(?i)^yes" : @"(?i)^no")
        {
            _accept = accept;
        }

        public override GsxMenuIntent ParentMenu => null;
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuDeiceOnPush;

        public override bool IsValidForPhase(AutomationState phase) => phase == AutomationState.Departure;

        public override bool ArePreconditionsSatisfied(GsxController controller) => true;

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () => controller?.Menu != null
                      && !controller.Menu.MatchTitle(GsxConstants.MenuDeiceOnPush),
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => $"Answer the GSX de-icing question ({(_accept ? "yes" : "no")})";
    }
}
