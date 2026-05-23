using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Navigation intent: from the gate menu, click "Reposition Aircraft" to
    /// open the "Select Position at &lt;airport&gt;" submenu. Resolves against
    /// the live gate menu title (MenuGate) and verifies the new menu is
    /// MenuParkingSelect — a slow submenu transition leaves the title at
    /// MenuGate and the resolver returns MenuTitleMismatch on the next attempt
    /// instead of blindly writing a choice (which on the gate menu would land
    /// on index 1, "Request Deboarding").
    /// </summary>
    internal sealed class OpenParkingSelectMenu : KeywordIntent
    {
        public OpenParkingSelectMenu()
            : base(@"^Reposition Aircraft")
        { }

        public override GsxMenuIntent ParentMenu => new OpenGateMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuGate;

        /// <summary>
        /// Reposition is only meaningful during <see cref="AutomationState.Preparation"/>
        /// — the existing <c>RunPreparation</c> call site is the only legitimate
        /// caller. Other phases would either find the aircraft already at the
        /// gate or attempt a teleport mid-departure.
        /// </summary>
        public override bool IsValidForPhase(AutomationState phase) => phase == AutomationState.Preparation;

        public override bool ArePreconditionsSatisfied(GsxController controller) => true;

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () => controller?.Menu != null
                      && controller.Menu.MatchTitle(GsxConstants.MenuParkingSelect),
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Open the parking-position selection submenu via Reposition Aircraft";
    }
}
