namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Discrete outcomes of <see cref="GsxMenu.ExecuteIntent"/>. The set is intentionally
    /// finite so callers can switch on it without a default arm; new values mean a new
    /// caller-visible failure mode.
    /// </summary>
    public enum MenuOutcome
    {
        /// <summary>Resolved, written, verified successfully.</summary>
        Success,
        /// <summary><see cref="GsxMenuIntent.IsValidForPhase"/> returned false — intent attempted in wrong automation phase.</summary>
        PhaseMismatch,
        /// <summary><see cref="GsxMenuIntent.ArePreconditionsSatisfied"/> returned false — GSX state does not permit this action right now.</summary>
        StatePreconditionFailed,
        /// <summary><see cref="GsxMenuIntent.IsAlreadySatisfied"/> returned true — no-op success (service already complete).</summary>
        StatePreconditionSatisfiedAlready,
        /// <summary>No menu line matched the intent's pattern. May be benign (item hidden because not applicable).</summary>
        ItemNotAvailable,
        /// <summary>Multiple menu lines matched — configuration bug.</summary>
        AmbiguousMatch,
        /// <summary>Live menu title did not match the intent's expected prefix(es).</summary>
        MenuTitleMismatch,
        /// <summary>Menu showed "Waiting for your action:" — door-control plane should handle, not us.</summary>
        DoorActionPrompt,
        /// <summary>Failed to navigate to <see cref="GsxMenuIntent.ParentMenu"/> (recursive call did not succeed) or top-level Open returned false.</summary>
        NavigationFailed,
        /// <summary>Write succeeded but expected state change did not happen within <see cref="GsxMenuIntent.VerifyOutcomeAsync"/>'s timeout.</summary>
        GsxNoResponse,
        /// <summary>General timeout during execution.</summary>
        Timeout,
        /// <summary>Exception during execution. See <see cref="GsxMenuResult.Exception"/>.</summary>
        GsxError,
    }
}
