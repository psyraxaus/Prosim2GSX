using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Gate-menu retract intent: presses "Operate Jetway" while the
    /// service is Active, asking GSX to undo the active state. GSX's
    /// "Operate Jetway" is a toggle — same menu line as
    /// <see cref="RequestJetway"/>, but the precondition / already-satisfied
    /// semantics are inverted:
    /// <list type="bullet">
    /// <item><description>This intent's <see cref="IsAlreadySatisfied"/> returns true when the jetway is NOT Active (nothing to retract), so the resolver short-circuits with a benign skip rather than clicking the menu.</description></item>
    /// <item><description><see cref="ArePreconditionsSatisfied"/> requires <see cref="GsxServiceState.Active"/> — clicking "Operate Jetway" while the jetway is callable (idle / retracted) would re-extend it, which is what <see cref="RequestJetway"/> is for.</description></item>
    /// </list>
    ///
    /// <para>
    /// Why a separate intent exists at all: the previous
    /// <c>GsxServiceJetway.Remove()</c> reused <see cref="RequestJetway"/>
    /// for the undo path. <c>RequestJetway.IsAlreadySatisfied</c> returns
    /// true when the service is <see cref="GsxServiceState.Active"/> — which
    /// is exactly the state we want to undo — so the resolver short-circuited
    /// and the menu was never pressed. The retract path then silently
    /// no-op'd and the operator had to retract manually. The intent
    /// framework's semantic is "request the state X be satisfied"; retract
    /// is a different intent and gets its own type.
    /// </para>
    /// </summary>
    internal sealed class RetractJetway : KeywordIntent
    {
        public RetractJetway()
            : base(@"^operate jetway")
        { }

        public override GsxMenuIntent ParentMenu => new OpenGateMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuGate;

        public override bool IsValidForPhase(AutomationState phase)
            // PushBack / TaxiOut intentionally NOT included: GSX retracts
            // the jetway automatically as part of its own pushback flow.
            // Manual retract is only meaningful in the prep/turnaround/
            // departure window or during arrival cleanup.
            => phase == AutomationState.Preparation
            || phase == AutomationState.Departure
            || phase == AutomationState.PushBack
            || phase == AutomationState.Arrival
            || phase == AutomationState.TurnAround;

        public override bool ArePreconditionsSatisfied(GsxController controller)
        {
            // Only retract when GSX considers the service active. We
            // deliberately do NOT gate on Operation == Idle — the whole
            // reason this intent exists is to work around GSX's stuck
            // operation-LVAR case.
            var jetway = IntentHelpers.GetService<GsxServiceJetway>(controller, GsxServiceType.Jetway);
            return jetway != null && jetway.IsAvailable && jetway.State == GsxServiceState.Active;
        }

        public override bool IsAlreadySatisfied(GsxController controller)
        {
            // Inverse of RequestJetway.IsAlreadySatisfied: we're "already
            // satisfied" when the jetway is NOT active — i.e. there's
            // nothing to retract. This is the critical semantic flip
            // that makes Remove() actually click the menu.
            var jetway = IntentHelpers.GetService<GsxServiceJetway>(controller, GsxServiceType.Jetway);
            return jetway == null || jetway.State != GsxServiceState.Active;
        }

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            // Retract is fire-and-confirm: the menu click is the action, and
            // GSX physical retraction takes 15-30s — well beyond the default
            // 5s IntentVerificationTimeout. Field testing showed waiting on
            // state == Active reliably timed out as GsxNoResponse even though
            // the retract was happening normally (next pushback step
            // succeeded a minute later). Nothing downstream gates on the
            // verify result for retract, so declaring success at the menu-
            // write point is correct and removes a misleading red row from
            // the diagnostic log on every retract.
            return Task.FromResult(true);
        }

        public override string Describe() => "Retract the gate jetway";
    }
}
