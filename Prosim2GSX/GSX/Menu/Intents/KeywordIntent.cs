using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Concrete base for the common "resolve a menu line by regex" intent pattern.
    /// The regex is compiled once at construction (case-insensitive by default) and
    /// reused for every <see cref="ResolveMenuLineIndex"/> call — intents are
    /// long-lived and stateless. Subclasses still own <see cref="GsxMenuIntent.ParentMenu"/>,
    /// <see cref="GsxMenuIntent.ExpectedMenuTitlePrefix"/>,
    /// <see cref="GsxMenuIntent.IsValidForPhase"/>,
    /// <see cref="GsxMenuIntent.ArePreconditionsSatisfied"/>, and
    /// <see cref="GsxMenuIntent.Describe"/>.
    /// </summary>
    public abstract class KeywordIntent : GsxMenuIntent
    {
        private readonly Regex _regex;

        /// <summary>The original pattern source — surfaced in <see cref="AmbiguousMatchException"/>.</summary>
        public string Pattern { get; }

        /// <summary>
        /// Constructs the intent with the given pattern. Default options are
        /// <see cref="RegexOptions.Compiled"/> | <see cref="RegexOptions.IgnoreCase"/>;
        /// subclasses that need anchoring or single-line semantics pass them
        /// explicitly. The regex is compiled once and cached.
        /// </summary>
        protected KeywordIntent(string pattern, RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
            Pattern = pattern;
            _regex = new Regex(pattern, options);
        }

        /// <summary>
        /// Iterates <paramref name="menuLines"/> once. Returns the single matching
        /// 0-based index, <c>-1</c> on no match, throws
        /// <see cref="AmbiguousMatchException"/> on more than one match.
        /// </summary>
        public override int ResolveMenuLineIndex(IReadOnlyList<string> menuLines)
        {
            List<int> hits = null;
            for (int i = 0; i < menuLines.Count; i++)
            {
                var line = menuLines[i] ?? string.Empty;
                if (_regex.IsMatch(line))
                {
                    if (hits == null) hits = new List<int>(2);
                    hits.Add(i);
                }
            }

            if (hits == null) return -1;
            if (hits.Count == 1) return hits[0];
            throw new AmbiguousMatchException(IntentName, Pattern, hits);
        }
    }
}
