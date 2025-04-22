namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Service for interacting with GSX menus
    /// </summary>
    public interface IGsxMenuService
    {
        /// <summary>
        /// Open the GSX menu
        /// </summary>
        void OpenMenu();

        /// <summary>
        /// Select a menu item by index
        /// </summary>
        /// <param name="index">The 1-based index of the menu item</param>
        /// <param name="waitForMenu">Whether to wait for the menu to be ready</param>
        void SelectMenuItem(int index, bool waitForMenu = true);

        /// <summary>
        /// Wait for the menu to be ready
        /// </summary>
        void WaitForMenuReady();

        /// <summary>
        /// Check if an operator selection is active
        /// </summary>
        /// <returns>1 if active, 0 if not, -1 if undetermined</returns>
        int IsOperatorSelectionActive();

        /// <summary>
        /// Handle operator selection
        /// </summary>
        /// <param name="operatorDelay">Delay before selecting an operator</param>
        /// <returns>True if operator was selected</returns>
        bool HandleOperatorSelection(int operatorDelay = 2000);
    }
}