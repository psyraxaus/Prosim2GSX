using System;
using System.Collections.Generic;
using System.Linq;
using Prosim2GSX.UI.EFB.Themes;

namespace Prosim2GSX.UI.EFB.Windows
{
    /// <summary>
    /// Manages EFB windows.
    /// </summary>
    public class EFBWindowManager
    {
        private readonly List<EFBWindow> _windows = new List<EFBWindow>();
        private readonly Dictionary<string, Type> _pageTypes = new Dictionary<string, Type>();
        private readonly Dictionary<string, (string Title, string Icon)> _pageInfo = new Dictionary<string, (string Title, string Icon)>();
        private readonly EFBThemeManager _themeManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBWindowManager"/> class.
        /// </summary>
        /// <param name="themeManager">The theme manager.</param>
        public EFBWindowManager(EFBThemeManager themeManager)
        {
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        }

        /// <summary>
        /// Gets all windows.
        /// </summary>
        public IReadOnlyList<EFBWindow> Windows => _windows.AsReadOnly();

        /// <summary>
        /// Creates a new window.
        /// </summary>
        /// <returns>The created window.</returns>
        public EFBWindow CreateWindow()
        {
            var window = new EFBWindow();
            window.SetThemeManager(_themeManager);
            
            // Register pages with the window
            foreach (var pageKey in _pageTypes.Keys)
            {
                var (title, icon) = _pageInfo[pageKey];
                window.RegisterPage(pageKey, _pageTypes[pageKey], title, icon);
            }
            
            _windows.Add(window);
            window.Closed += Window_Closed;
            
            return window;
        }

        /// <summary>
        /// Closes all windows.
        /// </summary>
        public void CloseAllWindows()
        {
            foreach (var window in _windows.ToList())
            {
                window.Close();
            }
        }

        /// <summary>
        /// Registers a page with the window manager.
        /// </summary>
        /// <param name="pageKey">The page key.</param>
        /// <param name="pageType">The page type.</param>
        /// <param name="title">The page title.</param>
        /// <param name="icon">The page icon.</param>
        public void RegisterPage(string pageKey, Type pageType, string title, string icon)
        {
            if (string.IsNullOrEmpty(pageKey))
            {
                throw new ArgumentException("Page key cannot be null or empty.", nameof(pageKey));
            }

            if (pageType == null)
            {
                throw new ArgumentNullException(nameof(pageType));
            }

            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("Page title cannot be null or empty.", nameof(title));
            }

            if (string.IsNullOrEmpty(icon))
            {
                throw new ArgumentException("Page icon cannot be null or empty.", nameof(icon));
            }

            _pageTypes[pageKey] = pageType;
            _pageInfo[pageKey] = (title, icon);

            // Register the page with all existing windows
            foreach (var window in _windows)
            {
                window.RegisterPage(pageKey, pageType, title, icon);
            }
        }

        /// <summary>
        /// Gets a window by index.
        /// </summary>
        /// <param name="index">The window index.</param>
        /// <returns>The window at the specified index.</returns>
        public EFBWindow GetWindow(int index)
        {
            if (index < 0 || index >= _windows.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _windows[index];
        }

        /// <summary>
        /// Gets the number of windows.
        /// </summary>
        public int WindowCount => _windows.Count;

        /// <summary>
        /// Detaches the current window.
        /// </summary>
        public void DetachCurrentWindow()
        {
            // This would be implemented to detach the current window
            // For now, just create a new window
            var window = CreateWindow();
            window.Show();
        }

        /// <summary>
        /// Toggles fullscreen mode for the current window.
        /// </summary>
        public void ToggleFullscreen()
        {
            // This would be implemented to toggle fullscreen mode for the current window
            // For now, just log a message
            System.Diagnostics.Debug.WriteLine("ToggleFullscreen called");
        }

        /// <summary>
        /// Toggles compact mode for the current window.
        /// </summary>
        public void ToggleCompactMode()
        {
            // This would be implemented to toggle compact mode for the current window
            // For now, just log a message
            System.Diagnostics.Debug.WriteLine("ToggleCompactMode called");
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
