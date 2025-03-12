using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Prosim2GSX.UI.EFB.Input
{
    /// <summary>
    /// Manages keyboard shortcuts and navigation for the EFB UI.
    /// </summary>
    public class KeyboardManager
    {
        private static readonly KeyboardManager _instance = new();
        private readonly Dictionary<KeyGesture, CommandBinding> _shortcuts = new();
        private readonly Dictionary<string, KeyGesture> _shortcutDescriptions = new();

        /// <summary>
        /// Gets the singleton instance of the KeyboardManager.
        /// </summary>
        public static KeyboardManager Instance => _instance;

        /// <summary>
        /// Initializes a new instance of the KeyboardManager class.
        /// </summary>
        private KeyboardManager()
        {
            // Initialize with default shortcuts
            RegisterDefaultShortcuts();
        }

        /// <summary>
        /// Registers the default keyboard shortcuts.
        /// </summary>
        private void RegisterDefaultShortcuts()
        {
            // Navigation shortcuts
            RegisterShortcut(new KeyGesture(Key.Home, ModifierKeys.None), "Navigate to Home", NavigateToHome);
            RegisterShortcut(new KeyGesture(Key.A, ModifierKeys.Control), "Navigate to Aircraft", NavigateToAircraft);
            RegisterShortcut(new KeyGesture(Key.S, ModifierKeys.Control), "Navigate to Services", NavigateToServices);
            RegisterShortcut(new KeyGesture(Key.P, ModifierKeys.Control), "Navigate to Plan", NavigateToPlan);
            RegisterShortcut(new KeyGesture(Key.G, ModifierKeys.Control), "Navigate to Ground", NavigateToGround);
            RegisterShortcut(new KeyGesture(Key.L, ModifierKeys.Control), "Navigate to Logs", NavigateToLogs);
            
            // Window management shortcuts
            RegisterShortcut(new KeyGesture(Key.D, ModifierKeys.Control), "Detach Window", DetachWindow);
            RegisterShortcut(new KeyGesture(Key.F, ModifierKeys.Control), "Toggle Fullscreen", ToggleFullscreen);
            RegisterShortcut(new KeyGesture(Key.M, ModifierKeys.Control), "Toggle Compact Mode", ToggleCompactMode);
            
            // Theme shortcuts
            RegisterShortcut(new KeyGesture(Key.T, ModifierKeys.Control), "Cycle Themes", CycleThemes);
            RegisterShortcut(new KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Shift), "Open Theme Selector", OpenThemeSelector);
            
            // Help shortcuts
            RegisterShortcut(new KeyGesture(Key.F1, ModifierKeys.None), "Show Help", ShowHelp);
            RegisterShortcut(new KeyGesture(Key.OemQuestion, ModifierKeys.Shift), "Show Keyboard Shortcuts", ShowKeyboardShortcuts);
        }

        /// <summary>
        /// Registers a keyboard shortcut.
        /// </summary>
        /// <param name="gesture">The key gesture for the shortcut.</param>
        /// <param name="description">A description of the shortcut.</param>
        /// <param name="action">The action to execute when the shortcut is triggered.</param>
        public void RegisterShortcut(KeyGesture gesture, string description, Action action)
        {
            if (gesture == null)
                throw new ArgumentNullException(nameof(gesture));
            
            if (string.IsNullOrEmpty(description))
                throw new ArgumentException("Description cannot be null or empty.", nameof(description));
            
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            
            // Create a command that executes the action
            var command = new RoutedCommand();
            
            // Create a command binding
            var binding = new CommandBinding(command, (sender, e) => action());
            
            // Store the shortcut
            _shortcuts[gesture] = binding;
            _shortcutDescriptions[description] = gesture;
        }

        /// <summary>
        /// Unregisters a keyboard shortcut.
        /// </summary>
        /// <param name="gesture">The key gesture for the shortcut.</param>
        public void UnregisterShortcut(KeyGesture gesture)
        {
            if (gesture == null)
                throw new ArgumentNullException(nameof(gesture));
            
            if (_shortcuts.TryGetValue(gesture, out var binding))
            {
                _shortcuts.Remove(gesture);
                
                // Remove the description as well
                string descriptionToRemove = null;
                foreach (var kvp in _shortcutDescriptions)
                {
                    if (kvp.Value.Equals(gesture))
                    {
                        descriptionToRemove = kvp.Key;
                        break;
                    }
                }
                
                if (descriptionToRemove != null)
                {
                    _shortcutDescriptions.Remove(descriptionToRemove);
                }
            }
        }

        /// <summary>
        /// Applies the keyboard shortcuts to a window.
        /// </summary>
        /// <param name="window">The window to apply the shortcuts to.</param>
        public void ApplyShortcutsToWindow(Window window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            
            // Add the command bindings to the window
            foreach (var shortcut in _shortcuts)
            {
                var gesture = shortcut.Key;
                var binding = shortcut.Value;
                
                // Add the command binding to the window
                window.CommandBindings.Add(binding);
                
                // Add the input binding to the window
                window.InputBindings.Add(new KeyBinding(binding.Command, gesture));
            }
        }

        /// <summary>
        /// Gets a dictionary of all registered shortcuts and their descriptions.
        /// </summary>
        /// <returns>A dictionary of shortcut descriptions and key gestures.</returns>
        public Dictionary<string, KeyGesture> GetShortcutDescriptions()
        {
            return new Dictionary<string, KeyGesture>(_shortcutDescriptions);
        }

        /// <summary>
        /// Handles a key press event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        public void HandleKeyPress(object sender, KeyEventArgs e)
        {
            // Check if the key press matches any registered shortcuts
            foreach (var shortcut in _shortcuts)
            {
                var gesture = shortcut.Key;
                
                if (gesture.Key == e.Key && gesture.Modifiers == Keyboard.Modifiers)
                {
                    // Execute the command
                    shortcut.Value.Command.Execute(null, sender as IInputElement);
                    
                    // Mark the event as handled
                    e.Handled = true;
                    return;
                }
            }
        }

        #region Navigation Actions

        private void NavigateToHome()
        {
            // Navigate to the home page
            // This will be implemented by the navigation service
            var navigationService = Prosim2GSX.UI.EFB.Navigation.EFBNavigationService.Instance;
            navigationService.NavigateTo("HomePage");
        }

        private void NavigateToAircraft()
        {
            // Navigate to the aircraft page
            var navigationService = Prosim2GSX.UI.EFB.Navigation.EFBNavigationService.Instance;
            navigationService.NavigateTo("AircraftPage");
        }

        private void NavigateToServices()
        {
            // Navigate to the services page
            var navigationService = Prosim2GSX.UI.EFB.Navigation.EFBNavigationService.Instance;
            navigationService.NavigateTo("ServicesPage");
        }

        private void NavigateToPlan()
        {
            // Navigate to the plan page
            var navigationService = Prosim2GSX.UI.EFB.Navigation.EFBNavigationService.Instance;
            navigationService.NavigateTo("PlanPage");
        }

        private void NavigateToGround()
        {
            // Navigate to the ground page
            var navigationService = Prosim2GSX.UI.EFB.Navigation.EFBNavigationService.Instance;
            navigationService.NavigateTo("GroundPage");
        }

        private void NavigateToLogs()
        {
            // Navigate to the logs page
            var navigationService = Prosim2GSX.UI.EFB.Navigation.EFBNavigationService.Instance;
            navigationService.NavigateTo("LogsPage");
        }

        #endregion

        #region Window Management Actions

        private void DetachWindow()
        {
            // Detach the current window
            var windowManager = Prosim2GSX.UI.EFB.Windows.EFBWindowManager.Instance;
            windowManager.DetachCurrentWindow();
        }

        private void ToggleFullscreen()
        {
            // Toggle fullscreen mode
            var windowManager = Prosim2GSX.UI.EFB.Windows.EFBWindowManager.Instance;
            windowManager.ToggleFullscreen();
        }

        private void ToggleCompactMode()
        {
            // Toggle compact mode
            var windowManager = Prosim2GSX.UI.EFB.Windows.EFBWindowManager.Instance;
            windowManager.ToggleCompactMode();
        }

        #endregion

        #region Theme Actions

        private void CycleThemes()
        {
            // Cycle through available themes
            var themeManager = Prosim2GSX.UI.EFB.Themes.EFBThemeManager.Instance;
            themeManager.CycleThemes();
        }

        private void OpenThemeSelector()
        {
            // Open the theme selector
            var themeManager = Prosim2GSX.UI.EFB.Themes.EFBThemeManager.Instance;
            themeManager.ShowThemeSelector();
        }

        #endregion

        #region Help Actions

        private void ShowHelp()
        {
            // Show the help window
            MessageBox.Show("Help functionality will be implemented in a future update.", "Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowKeyboardShortcuts()
        {
            // Show the keyboard shortcuts window
            var shortcuts = GetShortcutDescriptions();
            var shortcutText = "Keyboard Shortcuts:\n\n";
            
            foreach (var shortcut in shortcuts)
            {
                shortcutText += $"{shortcut.Key}: {shortcut.Value}\n";
            }
            
            MessageBox.Show(shortcutText, "Keyboard Shortcuts", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}
