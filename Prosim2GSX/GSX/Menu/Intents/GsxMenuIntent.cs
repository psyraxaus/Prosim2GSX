using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProsimInterface;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Base type for all menu-driving intents. An intent describes <em>what</em> the
    /// caller wants ("answer Yes to the de-icing question", "request boarding") in
    /// terms of the live menu's contents, never as a fixed ordinal. The resolver
    /// (<see cref="GsxMenu.ExecuteIntent"/>) walks the intent's parent chain to
    /// navigate, snapshots the live menu, verifies the title, then calls
    /// <see cref="ResolveMenuLineIndex"/> against the snapshot to obtain the
    /// 0-based choice index.
    /// </summary>
    public abstract class GsxMenuIntent
    {
        /// <summary>Human-readable name used in diagnostic logs. Defaults to the class name.</summary>
        public virtual string IntentName => GetType().Name;

        /// <summary>
        /// The parent menu intent this one navigates from. <c>null</c> means top-level:
        /// the resolver opens the GSX menu directly via <see cref="GsxMenu.Open"/> as
        /// its base case (used for both "open the gate menu" intents and for intents
        /// that operate on menus GSX itself raised — interrupt-pushback variants,
        /// question prompts).
        /// </summary>
        public abstract GsxMenuIntent ParentMenu { get; }

        /// <summary>
        /// The expected title prefix of the menu this intent resolves against. The
        /// resolver requires the live title to <c>StartsWith</c>-match this prefix
        /// (case-insensitive) before resolution; otherwise it returns
        /// <see cref="MenuOutcome.MenuTitleMismatch"/>. Intents whose menu has more
        /// than one valid title (e.g. operator selection) override
        /// <see cref="ExpectedMenuTitlePrefixes"/> instead — by default that property
        /// wraps this single prefix.
        /// </summary>
        public abstract string ExpectedMenuTitlePrefix { get; }

        /// <summary>
        /// The set of valid title prefixes for the menu this intent resolves against.
        /// Default implementation wraps the single <see cref="ExpectedMenuTitlePrefix"/>;
        /// override only for intents whose menu legitimately exposes multiple titles
        /// (e.g. <c>SelectOperator</c> on both "Select handling operator" and
        /// "Select catering operator"). The resolver accepts a live title that
        /// <c>StartsWith</c>-matches any element.
        /// </summary>
        public virtual IReadOnlyList<string> ExpectedMenuTitlePrefixes => new[] { ExpectedMenuTitlePrefix };

        /// <summary>
        /// Plausibility check. Returns true if the intent is valid for the current
        /// automation state. False → resolver returns <see cref="MenuOutcome.PhaseMismatch"/>.
        /// Cheap pure function; never reads controller state.
        /// </summary>
        public abstract bool IsValidForPhase(AutomationState phase);

        /// <summary>
        /// State precondition. Reads GSX service states via
        /// <c>controller.GsxServices[type].State</c> and returns true if the intent
        /// should proceed. False → resolver returns
        /// <see cref="MenuOutcome.StatePreconditionFailed"/>.
        /// </summary>
        public abstract bool ArePreconditionsSatisfied(GsxController controller);

        /// <summary>
        /// Optional short-circuit: returns true if the desired end state is already
        /// satisfied (e.g. requesting refuel when refuel is already complete). Distinct
        /// from <see cref="ArePreconditionsSatisfied"/> — that returns false on "wrong
        /// state to act"; this returns true on "no need to act". Default <c>false</c>.
        /// </summary>
        public virtual bool IsAlreadySatisfied(GsxController controller) => false;

        /// <summary>
        /// Resolves the 0-based menu-line index (suitable for <c>FSDT_GSX_MENU_CHOICE</c>)
        /// against the live menu lines. Returns <c>-1</c> if nothing matches. Throws
        /// <see cref="AmbiguousMatchException"/> on multi-match — the resolver translates
        /// that into <see cref="MenuOutcome.AmbiguousMatch"/>.
        /// </summary>
        public abstract int ResolveMenuLineIndex(IReadOnlyList<string> menuLines);

        /// <summary>
        /// Post-write verification. Polls a GSX LVAR (typically via
        /// <c>IntentHelpers.PollUntilAsync</c>) and returns true if the expected state
        /// change occurred within <paramref name="timeout"/>. Default returns true
        /// immediately — for intents with no observable LVAR effect (navigation,
        /// already-success short-circuits) the absence of a verify is the point.
        /// </summary>
        public virtual Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
            => Task.FromResult(true);

        /// <summary>Human-readable description for logs.</summary>
        public abstract string Describe();
    }
}
