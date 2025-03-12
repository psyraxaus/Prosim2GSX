using System;
using System.Windows.Controls;

namespace Prosim2GSX.UI.EFB.Navigation
{
    /// <summary>
    /// Interface for EFB pages.
    /// </summary>
    public interface IEFBPage
    {
        /// <summary>
        /// Gets the page title.
        /// </summary>
        string Title { get; }
        
        /// <summary>
        /// Gets the page icon.
        /// </summary>
        string Icon { get; }
        
        /// <summary>
        /// Gets the page content.
        /// </summary>
        UserControl Content { get; }
        
        /// <summary>
        /// Gets a value indicating whether the page is visible in the navigation menu.
        /// </summary>
        bool IsVisibleInMenu { get; }
        
        /// <summary>
        /// Gets a value indicating whether the page can be navigated to.
        /// </summary>
        bool CanNavigateTo { get; }
        
        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        void OnNavigatedTo();
        
        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        void OnNavigatedFrom();
        
        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        void OnActivated();
        
        /// <summary>
        /// Called when the page is deactivated.
        /// </summary>
        void OnDeactivated();
        
        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        void OnRefresh();
    }
}
