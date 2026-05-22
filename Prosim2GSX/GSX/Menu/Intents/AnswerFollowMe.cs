using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Menu-callback intent: answer the GSX follow-me question. Constructed
    /// with a <c>bool accept</c> — true matches "Yes", false matches "No".
    /// Today the only auto-answer in the codebase is the no-answer (the
    /// SkipFollowMe profile flag's existing handler at
    /// <see cref="GsxMenu.OnFollowMeQuestion"/> selects item 2 = "No");
    /// keeping a parameterised intent here lets a future profile expose
    /// "auto-accept" without a new intent class.
    /// </summary>
    internal sealed class AnswerFollowMe : KeywordIntent
    {
        private readonly bool _accept;

        public AnswerFollowMe(bool accept)
            : base(accept ? @"(?i)^yes" : @"(?i)^no")
        {
            _accept = accept;
        }

        public override GsxMenuIntent ParentMenu => null;
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuFollowMe;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Arrival || phase == AutomationState.TaxiIn;

        public override bool ArePreconditionsSatisfied(GsxController controller) => true;

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () => controller?.Menu != null
                      && !controller.Menu.MatchTitle(GsxConstants.MenuFollowMe),
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => $"Answer the GSX follow-me question ({(_accept ? "yes" : "no")})";
    }
}
