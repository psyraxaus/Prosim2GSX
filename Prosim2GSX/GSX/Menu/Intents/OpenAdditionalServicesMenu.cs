using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Navigation intent: from the gate menu, click "Additional Services ▶" to
    /// open the Additional Services submenu. Resolves against the live gate
    /// menu (so its <see cref="ExpectedMenuTitlePrefix"/> is the gate-menu
    /// prefix) and verifies the new menu title is "Additional Services" before
    /// returning Success.
    /// </summary>
    internal sealed class OpenAdditionalServicesMenu : KeywordIntent
    {
        public OpenAdditionalServicesMenu()
            : base(@"^Additional Services")
        { }

        public override GsxMenuIntent ParentMenu => new OpenGateMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuGate;

        public override bool IsValidForPhase(AutomationState phase) => true;
        public override bool ArePreconditionsSatisfied(GsxController controller) => true;

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () => controller?.Menu != null
                      && controller.Menu.MatchTitle(GsxConstants.MenuAdditionalServices),
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Navigate to the Additional Services submenu";
    }
}
