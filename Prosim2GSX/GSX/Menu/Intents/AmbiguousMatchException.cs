using System;
using System.Collections.Generic;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Thrown by <see cref="KeywordIntent.ResolveMenuLineIndex"/> when more than one
    /// menu entry matches the intent's regex. The resolver translates this into a
    /// <see cref="MenuOutcome.AmbiguousMatch"/> result rather than picking arbitrarily —
    /// an intent that matches twice is a configuration bug and silently selecting one
    /// can fire the wrong action.
    /// </summary>
    public sealed class AmbiguousMatchException : Exception
    {
        /// <summary>The <see cref="GsxMenuIntent.IntentName"/> that produced the ambiguity.</summary>
        public string IntentName { get; }

        /// <summary>The regex pattern source that matched more than once.</summary>
        public string Pattern { get; }

        /// <summary>Zero-based menu-line indices that all matched the pattern.</summary>
        public IReadOnlyList<int> MatchedIndices { get; }

        public AmbiguousMatchException(string intentName, string pattern, IReadOnlyList<int> matchedIndices)
            : base($"Intent '{intentName}' pattern '{pattern}' matched multiple menu lines: [{string.Join(", ", matchedIndices)}]")
        {
            IntentName = intentName;
            Pattern = pattern;
            MatchedIndices = matchedIndices;
        }
    }
}
