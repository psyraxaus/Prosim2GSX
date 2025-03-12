using System;

namespace Prosim2GSX.UI.EFB.Navigation
{
    /// <summary>
    /// Interface for EFB pages.
    /// Defines the common properties and methods that all EFB pages must implement.
    /// </summary>
    public interface IEFBPage
    {
        /// <summary>
        /// Gets the title of the page.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets the icon source for the page.
        /// </summary>
        string IconSource { get; }

        /// <summary>
        /// Gets a value indicating whether this page can be navigated away from.
        /// </summary>
        /// <returns>True if the page can be navigated away from, false otherwise.</returns>
        bool CanNavigateAway();

        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        /// <param name="parameter">The navigation parameter.</param>
        void OnNavigatedTo(object parameter);

        /// <summary>
        /// Called when the page is navigated away from.
        /// </summary>
        void OnNavigatedFrom();
    }
}
