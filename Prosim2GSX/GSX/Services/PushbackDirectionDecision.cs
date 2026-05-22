using System;

namespace Prosim2GSX.GSX.Services
{
    /// <summary>
    /// Phase 5 diagnostic record. One instance is created per
    /// <see cref="GsxMenu.OnPushbackDirection"/> invocation and stored on
    /// <see cref="GsxServicePushback.LastDirectionDecision"/>; the
    /// pushback-completion observer in
    /// <see cref="GsxServicePushback.OnVehiclePushbackStateChange"/> reads
    /// (and clears) it to emit the <c>pushback-direction-followup</c>
    /// diagnostic. The fields are deliberately permissive — when the
    /// existing matching logic returns no match, the slot still records
    /// preference + timestamp so the follow-up can note "MANUAL" outcomes.
    /// </summary>
    public sealed class PushbackDirectionDecision
    {
        /// <summary>UTC instant the decision was made.</summary>
        public DateTime At { get; init; }

        /// <summary>User's pushback-direction preference at decision time.</summary>
        public PushbackPreference Preference { get; init; }

        /// <summary>The menu entry text the existing logic selected, or <c>null</c> on no-match.</summary>
        public string SelectedEntryText { get; init; }

        /// <summary>"text" | "fixed-index" | "manual" — which legacy matcher fired.</summary>
        public string Strategy { get; init; }

        /// <summary>
        /// Compass bearing the diagnostic parser extracted from
        /// <see cref="SelectedEntryText"/>, or <c>null</c> if the line
        /// carried no recognisable compass / numeric token. Independent of
        /// the legacy matching logic; the parse result is used only for the
        /// follow-up diagnostic, never to drive selection.
        /// </summary>
        public double? ParsedSelectedHeading { get; init; }
    }
}
