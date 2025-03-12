using System;

namespace Prosim2GSX.UI.EFB.Navigation
{
    /// <summary>
    /// Event arguments for navigation events.
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationEventArgs"/> class.
        /// </summary>
        /// <param name="pageKey">The page key.</param>
        /// <param name="parameter">The navigation parameter.</param>
        public NavigationEventArgs(string pageKey, object parameter = null)
        {
            PageKey = pageKey ?? throw new ArgumentNullException(nameof(pageKey));
            Parameter = parameter;
        }

        /// <summary>
        /// Gets the page key.
        /// </summary>
        public string PageKey { get; }

        /// <summary>
        /// Gets the navigation parameter.
        /// </summary>
        public object Parameter { get; }
    }
}
