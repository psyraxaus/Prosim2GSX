using Prosim2GSX.AppConfig;
using ProsimInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Operator-selection intent. Resolves an operator on either the
    /// "Select handling operator" or "Select catering operator" menu (both
    /// share this intent via the multi-prefix
    /// <see cref="ExpectedMenuTitlePrefixes"/> override added in Phase 1).
    ///
    /// Resolution delegates to <see cref="GsxOperator.OperatorSelection"/>
    /// — the existing helper already handles the <c>[GSX choice]</c> floating
    /// token plus the per-profile <c>OperatorPreferences</c> keyword matching,
    /// and absorbing rather than duplicating it honours the shared-context
    /// "absorb existing logic" rule. The helper returns a 1-based
    /// <see cref="GsxOperator.Number"/> which is decremented to the 0-based
    /// index the resolver expects.
    ///
    /// Precondition: the active profile must have
    /// <see cref="AircraftProfile.OperatorAutoSelect"/> enabled. When it's
    /// disabled, the resolver returns
    /// <see cref="MenuOutcome.StatePreconditionFailed"/> so the caller can
    /// fall back to the existing wait-for-human flow.
    /// </summary>
    internal sealed class SelectOperator : GsxMenuIntent
    {
        private static readonly IReadOnlyList<string> _titlePrefixes = new[]
        {
            GsxConstants.MenuOperatorHandling,
            GsxConstants.MenuOperatorCater,
        };

        private readonly AircraftProfile _profile;

        public SelectOperator(AircraftProfile profile)
        {
            _profile = profile;
        }

        public override GsxMenuIntent ParentMenu => null;

        /// <summary>Used only as the multi-prefix fallback — the resolver reads <see cref="ExpectedMenuTitlePrefixes"/>.</summary>
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuOperatorHandling;

        public override IReadOnlyList<string> ExpectedMenuTitlePrefixes => _titlePrefixes;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Preparation
            || phase == AutomationState.Arrival
            || phase == AutomationState.Departure
            || phase == AutomationState.TurnAround;

        public override bool ArePreconditionsSatisfied(GsxController controller)
            => _profile != null && _profile.OperatorAutoSelect;

        public override int ResolveMenuLineIndex(IReadOnlyList<string> menuLines)
        {
            if (_profile == null || menuLines == null) return -1;
            // GsxOperator.OperatorSelection requires a List<string>; .ToList()
            // gives us a fresh defensive copy, no mutation back into the
            // resolver's already-defensive snapshot.
            var op = GsxOperator.OperatorSelection(_profile, menuLines.ToList());
            if (op == null) return -1;
            // GsxOperator.Number is 1-based per ParseOperators; return the
            // 0-based menu-line index the resolver expects.
            return op.Number - 1;
        }

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            // Operator picker closes → title is no longer either operator prefix.
            return IntentHelpers.PollUntilAsync(
                () =>
                {
                    var title = controller?.Menu?.MenuTitle;
                    if (string.IsNullOrWhiteSpace(title)) return true;
                    foreach (var p in _titlePrefixes)
                    {
                        if (title.StartsWith(p, StringComparison.InvariantCultureIgnoreCase))
                            return false;
                    }
                    return true;
                },
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe()
        {
            var prefs = _profile?.OperatorPreferences != null
                ? string.Join(",", _profile.OperatorPreferences)
                : "<no profile>";
            return $"Select GSX handling/catering operator (preferences: {prefs})";
        }
    }
}
