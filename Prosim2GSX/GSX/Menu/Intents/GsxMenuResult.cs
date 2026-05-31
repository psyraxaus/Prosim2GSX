using System;
using System.Collections.Generic;
using ProsimInterface;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Rich, immutable record describing one <see cref="GsxMenu.ExecuteIntent"/> invocation.
    /// Carries everything the diagnostic logger needs to record the decision and outcome
    /// without going back to mutable controller state — the snapshot is taken inside the
    /// resolver so post-emission mutations on <see cref="GsxMenu.MenuLines"/> cannot
    /// retroactively change the record.
    /// </summary>
    /// <param name="Outcome">Discrete result classification.</param>
    /// <param name="Intent">The intent that was executed (never null).</param>
    /// <param name="MenuTitleObserved">The live menu title at resolution time. May be null if execution failed before snapshot.</param>
    /// <param name="MenuLinesObserved">Defensive copy of the menu entries (never the live mutable list).</param>
    /// <param name="ResolvedIndex">0-based menu-line index chosen; null when resolution did not happen.</param>
    /// <param name="MatchedEntryText">Text of the matched entry; null when resolution did not happen.</param>
    /// <param name="PhaseAtExecution">Automation phase passed to ExecuteIntent.</param>
    /// <param name="Reason">Human-readable explanation of the outcome — always populated.</param>
    /// <param name="Exception">Exception captured for <see cref="MenuOutcome.GsxError"/>; null otherwise.</param>
    /// <param name="Duration">Wall-clock duration of the whole ExecuteIntent call.</param>
    /// <param name="PostWriteStateObservation">Free-form post-write GSX state notes (typically a LVAR value); may be null.</param>
    public sealed record GsxMenuResult(
        MenuOutcome Outcome,
        GsxMenuIntent Intent,
        string MenuTitleObserved,
        IReadOnlyList<string> MenuLinesObserved,
        int? ResolvedIndex,
        string MatchedEntryText,
        AutomationState PhaseAtExecution,
        string Reason,
        Exception Exception = null,
        TimeSpan? Duration = null,
        string PostWriteStateObservation = null)
    {
        /// <summary>True for <see cref="MenuOutcome.Success"/> only.</summary>
        public bool IsSuccess => Outcome == MenuOutcome.Success;

        /// <summary>
        /// True for outcomes that callers should treat as "nothing to do, no error":
        /// the requested item is not present, the precondition already-satisfied
        /// short-circuit fired, or the menu is showing a door-action prompt our
        /// menu plane must not click on. Phase 3+ callers collapse
        /// <c>IsSuccess || IsBenignSkip</c> into today's "sequence succeeded" return.
        /// </summary>
        public bool IsBenignSkip => Outcome is MenuOutcome.ItemNotAvailable
                                             or MenuOutcome.StatePreconditionSatisfiedAlready
                                             or MenuOutcome.DoorActionPrompt;
    }
}
