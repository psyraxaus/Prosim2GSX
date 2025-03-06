using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX menu interaction service
    /// </summary>
    public interface IGSXMenuService
    {
        /// <summary>
        /// Gets or sets whether an operator was selected
        /// </summary>
        bool OperatorWasSelected { get; set; }
        
        /// <summary>
        /// Opens the GSX menu
        /// </summary>
        void MenuOpen();
        
        /// <summary>
        /// Selects a menu item by index
        /// </summary>
        /// <param name="index">The index of the menu item to select (1-based)</param>
        /// <param name="waitForMenu">Whether to wait for the menu to be ready before selecting</param>
        void MenuItem(int index, bool waitForMenu = true);
        
        /// <summary>
        /// Waits for the GSX menu to be ready
        /// </summary>
        void MenuWaitReady();
        
        /// <summary>
        /// Checks if operator selection is active
        /// </summary>
        /// <returns>1 if operator selection is active, 0 if not, -1 if unknown</returns>
        int IsOperatorSelectionActive();
        
        /// <summary>
        /// Handles operator selection
        /// </summary>
        void OperatorSelection();
    }
}
