using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Prosim2GSX.UI.EFB.Themes;

namespace Prosim2GSX.UI.EFB.Windows
{
    /// <summary>
    /// Manager for EFB windows.
    /// </summary>
    public class EFBWindowManager
    {
        private readonly List<EFBWindow> _windows = new();
        private readonly EFBThemeManager _themeManager;
        private readonly Dictionary<string, Type> _registeredPages = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBWindowManager"/> class.
        /// </summary>
        /// <param name="themeManager">The theme manager.</param>
        public EFBWindowManager(EFBThemeManager themeManager)
        {
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        }

        /// <summary>
        /// Gets the windows managed by this manager.
        /// </summary>
        public IReadOnlyList<EFBWindow> Windows => _windows.AsReadOnly();

        /// <summary>
        /// Registers a page with the window manager.
        /// </summary>
        /// <param name="key">The page key.</param>
        /// <param name="pageType">The page type.</param>
        /// <param name="title">The page title.</param>
        /// <param name="icon">The page icon.</param>
        public void RegisterPage(string key, Type pageType, string title, string icon)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Page key cannot be null or empty.", nameof(key));
            }

            if (pageType == null)
            {
                throw new ArgumentNullException(nameof(pageType));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Page title cannot be null or empty.", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(icon))
            {
                throw new ArgumentException("Page icon cannot be null or empty.", nameof(icon));
            }

            // Store the page registration information
            _registeredPages[key] = pageType;

            // Register the page with all existing windows
            foreach (var window in _windows)
            {
                window.RegisterPage(key, pageType, title, icon);
            }
        }

        /// <summary>
        /// Creates a new EFB window.
        /// </summary>
        /// <returns>The created window.</returns>
        public EFBWindow CreateWindow()
        {
            var window = new EFBWindow();
            window.SetThemeManager(_themeManager);
            window.Closed += Window_Closed;

            // Register all pages with the new window
            foreach (var page in _registeredPages)
            {
                // TODO: Store title and icon information for registered pages
                window.RegisterPage(page.Key, page.Value, page.Key, "\uE8A5"); // Default icon
            }

            _windows.Add(window);
            return window;
        }

        /// <summary>
        /// Closes a window.
        /// </summary>
        /// <param name="window">The window to close.</param>
        public void CloseWindow(EFBWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            window.Close();
        }

        /// <summary>
        /// Detaches a window to a secondary monitor.
        /// </summary>
        /// <param name="window">The window to detach.</param>
        public void DetachWindow(EFBWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            var screens = Screen.AllScreens;
            if (screens.Length <= 1)
            {
                // No secondary monitor available
                return;
            }

            // Find the current screen
            var currentScreen = Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(window).Handle);
            
            // Find a different screen
            var targetScreen = screens.FirstOrDefault(s => s != currentScreen) ?? screens[0];
            
            // Position the window on the target screen
            var workingArea = targetScreen.WorkingArea;
            window.Left = workingArea.Left + (workingArea.Width - window.Width) / 2;
            window.Top = workingArea.Top + (workingArea.Height - window.Height) / 2;
        }

        /// <summary>
        /// Gets the available screens.
        /// </summary>
        /// <returns>The available screens.</returns>
        public IEnumerable<Screen> GetAvailableScreens()
        {
            return Screen.AllScreens;
        }

        /// <summary>
        /// Closes all windows.
        /// </summary>
        public void CloseAllWindows()
        {
            // Create a copy of the windows list to avoid modification during enumeration
            var windows = _windows.ToList();
            foreach (var window in windows)
            {
                window.Close();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (sender is EFBWindow window)
            {
                window.Closed -= Window_Closed;
                _windows.Remove(window);
            }
        }
    }
}
