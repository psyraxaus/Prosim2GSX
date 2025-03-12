using System;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Event arguments for theme changed events.
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldTheme">The old theme.</param>
        /// <param name="newTheme">The new theme.</param>
        public ThemeChangedEventArgs(EFBThemeDefinition oldTheme, EFBThemeDefinition newTheme)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme ?? throw new ArgumentNullException(nameof(newTheme));
        }

        /// <summary>
        /// Gets the old theme.
        /// </summary>
        public EFBThemeDefinition OldTheme { get; }

        /// <summary>
        /// Gets the new theme.
        /// </summary>
        public EFBThemeDefinition NewTheme { get; }
    }
}
