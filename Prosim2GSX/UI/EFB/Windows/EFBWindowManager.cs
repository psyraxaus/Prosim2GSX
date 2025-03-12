using System;
using System.Collections.Generic;
using System.Linq;
using Prosim2GSX.UI.EFB.Themes;
using Prosim2GSX.Services;

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
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBWindowManager"/> class.
        /// </summary>
        /// <param name="themeManager">The theme manager.</param>
        /// <param name="logger">Optional logger instance.</param>
        public EFBWindowManager(EFBThemeManager themeManager, ILogger logger = null)
        {
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
            _logger = logger;
            _logger?.Log(LogLevel.Debug, "EFBWindowManager:Constructor", "EFBWindowManager initialized");
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
            _logger?.Log(LogLevel.Debug, "EFBWindowManager:CreateWindow", "Creating new EFB window");
            
            try
            {
                var window = new EFBWindow();
                
                _logger?.Log(LogLevel.Debug, "EFBWindowManager:CreateWindow", "Setting theme manager");
                window.SetThemeManager(_themeManager);
                
                // Register pages with the window
                _logger?.Log(LogLevel.Debug, "EFBWindowManager:CreateWindow", $"Registering {_pageTypes.Count} pages with window");
                foreach (var pageKey in _pageTypes.Keys)
                {
                    var (title, icon) = _pageInfo[pageKey];
                    window.RegisterPage(pageKey, _pageTypes[pageKey], title, icon);
                }
                
                _windows.Add(window);
                window.Closed += Window_Closed;
                
                _logger?.Log(LogLevel.Debug, "EFBWindowManager:CreateWindow", "Window created successfully");
                return window;
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBWindowManager:CreateWindow", ex, "Failed to create EFB window");
                throw; // Re-throw the exception to be handled by the caller
            }
        }

        /// <summary>
        /// Closes all windows.
        /// </summary>
        public void CloseAllWindows()
        {
            _logger?.Log(LogLevel.Debug, "EFBWindowManager:CloseAllWindows", $"Closing {_windows.Count} windows");
            
            try
            {
                foreach (var window in _windows.ToList())
                {
                    window.Close();
                }
                
                _logger?.Log(LogLevel.Debug, "EFBWindowManager:CloseAllWindows", "All windows closed successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBWindowManager:CloseAllWindows", ex, "Error closing windows");
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
            _logger?.Log(LogLevel.Debug, "EFBWindowManager:RegisterPage", 
                $"Registering page '{pageKey}' with type '{pageType.FullName}', title '{title}', icon '{icon}'");
            
            try
            {
                if (string.IsNullOrEmpty(pageKey))
                {
                    var errorMessage = "Page key cannot be null or empty.";
                    _logger?.Log(LogLevel.Error, "EFBWindowManager:RegisterPage", errorMessage);
                    throw new ArgumentException(errorMessage, nameof(pageKey));
                }

                if (pageType == null)
                {
                    var errorMessage = "Page type cannot be null.";
                    _logger?.Log(LogLevel.Error, "EFBWindowManager:RegisterPage", errorMessage);
                    throw new ArgumentNullException(nameof(pageType), errorMessage);
                }

                if (string.IsNullOrEmpty(title))
                {
                    var errorMessage = "Page title cannot be null or empty.";
                    _logger?.Log(LogLevel.Error, "EFBWindowManager:RegisterPage", errorMessage);
                    throw new ArgumentException(errorMessage, nameof(title));
                }

                if (string.IsNullOrEmpty(icon))
                {
                    var errorMessage = "Page icon cannot be null or empty.";
                    _logger?.Log(LogLevel.Error, "EFBWindowManager:RegisterPage", errorMessage);
                    throw new ArgumentException(errorMessage, nameof(icon));
                }

                _pageTypes[pageKey] = pageType;
                _pageInfo[pageKey] = (title, icon);
                _logger?.Log(LogLevel.Debug, "EFBWindowManager:RegisterPage", 
                    $"Page '{pageKey}' registered with window manager");

                // Register the page with all existing windows
                if (_windows.Count > 0)
                {
                    _logger?.Log(LogLevel.Debug, "EFBWindowManager:RegisterPage", 
                        $"Registering page '{pageKey}' with {_windows.Count} existing windows");
                    
                    foreach (var window in _windows)
                    {
                        window.RegisterPage(pageKey, pageType, title, icon);
                    }
                }
                
                _logger?.Log(LogLevel.Debug, "EFBWindowManager:RegisterPage", 
                    $"Page '{pageKey}' registration completed successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBWindowManager:RegisterPage", ex, 
                    $"Error registering page '{pageKey}'");
                throw; // Re-throw the exception to be handled by the caller
            }
        }

        /// <summary>
        /// Gets a window by index.
        /// </summary>
        /// <param name="index">The window index.</param>
        /// <returns>The window at the specified index.</returns>
        public EFBWindow GetWindow(int index)
        {
            _logger?.Log(LogLevel.Debug, "EFBWindowManager:GetWindow", $"Getting window at index {index}");
            
            try
            {
                if (index < 0 || index >= _windows.Count)
                {
                    var errorMessage = $"Window index {index} is out of range. Valid range: 0-{_windows.Count - 1}";
                    _logger?.Log(LogLevel.Error, "EFBWindowManager:GetWindow", errorMessage);
                    throw new ArgumentOutOfRangeException(nameof(index), errorMessage);
                }

                _logger?.Log(LogLevel.Debug, "EFBWindowManager:GetWindow", $"Successfully retrieved window at index {index}");
                return _windows[index];
            }
            catch (Exception ex) when (!(ex is ArgumentOutOfRangeException))
            {
                _logger?.Log(LogLevel.Error, "EFBWindowManager:GetWindow", ex, $"Error getting window at index {index}");
                throw;
            }
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
            _logger?.Log(LogLevel.Debug, "EFBWindowManager:DetachCurrentWindow", "Detaching current window");
            
            try
            {
                // This would be implemented to detach the current window
                // For now, just create a new window
                _logger?.Log(LogLevel.Debug, "EFBWindowManager:DetachCurrentWindow", "Creating new window for detachment");
                var window = CreateWindow();
                window.Show();
                
                _logger?.Log(LogLevel.Debug, "EFBWindowManager:DetachCurrentWindow", "Window detached successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBWindowManager:DetachCurrentWindow", ex, "Error detaching window");
                throw;
            }
        }

        /// <summary>
        /// Toggles fullscreen mode for the current window.
        /// </summary>
        public void ToggleFullscreen()
        {
            // This would be implemented to toggle fullscreen mode for the current window
            // For now, just log a message
            _logger?.Log(LogLevel.Debug, "EFBWindowManager:ToggleFullscreen", "ToggleFullscreen called");
        }

        /// <summary>
        /// Toggles compact mode for the current window.
        /// </summary>
        public void ToggleCompactMode()
        {
            // This would be implemented to toggle compact mode for the current window
            // For now, just log a message
            _logger?.Log(LogLevel.Debug, "EFBWindowManager:ToggleCompactMode", "ToggleCompactMode called");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (sender is EFBWindow window)
            {
                _logger?.Log(LogLevel.Debug, "EFBWindowManager:Window_Closed", "Window closed event received");
                
                window.Closed -= Window_Closed;
                _windows.Remove(window);
                
                _logger?.Log(LogLevel.Debug, "EFBWindowManager:Window_Closed", 
                    $"Window removed from window list. Remaining windows: {_windows.Count}");
            }
        }
    }
}
