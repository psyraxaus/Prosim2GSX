using ProsimInterface;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Top-level navigation intent: open the GSX "Activate Services at &lt;airport&gt;"
    /// gate menu via the GSX menu hotkey. This is the only intent in the family
    /// whose ResolveMenuLineIndex returns <c>-1</c> by design; the resolver's
    /// navigation-only special case (top-level intent, -1 resolve, matching
    /// title) treats that combination as success without writing a choice. The
    /// "operation" is the <see cref="GsxMenu.Open"/> call the resolver makes in
    /// step 4.
    /// </summary>
    internal sealed class OpenGateMenu : GsxMenuIntent
    {
        public override GsxMenuIntent ParentMenu => null;
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuGate;

        public override bool IsValidForPhase(AutomationState phase) => true;
        public override bool ArePreconditionsSatisfied(GsxController controller) => true;

        /// <summary>
        /// Returns <c>-1</c>: this intent does not select a menu entry, it merely
        /// asks the resolver to open the menu and observe that the title matches.
        /// </summary>
        public override int ResolveMenuLineIndex(IReadOnlyList<string> menuLines) => -1;

        /// <summary>
        /// Polls until the live menu title begins with "Activate Services at" —
        /// confirms the gate menu actually appeared after <see cref="GsxMenu.Open"/>.
        /// </summary>
        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () => controller?.Menu != null
                      && controller.Menu.MatchTitle(GsxConstants.MenuGate),
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Open the gate-services menu via the GSX menu hotkey";
    }
}
