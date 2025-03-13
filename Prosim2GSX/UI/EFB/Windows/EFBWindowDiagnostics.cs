using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Resources;

namespace Prosim2GSX.UI.EFB.Windows
{
    /// <summary>
    /// Provides diagnostic functionality for the EFB window.
    /// </summary>
    public static class EFBWindowDiagnostics
    {
        private static ILogger _logger;
        private static EFBResourceManager _resourceManager;

        /// <summary>
        /// Initializes the diagnostics with a logger.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        public static void Initialize(ILogger logger)
        {
            _logger = logger;
            _logger?.Log(LogLevel.Debug, "EFBWindowDiagnostics", "Diagnostics initialized");
        }
        
        /// <summary>
        /// Sets the resource manager to use for diagnostics.
        /// </summary>
        /// <param name="resourceManager">The resource manager to use.</param>
        public static void SetResourceManager(EFBResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _logger?.Log(LogLevel.Debug, "EFBWindowDiagnostics", "Resource manager set");
        }

        /// <summary>
        /// Adds diagnostic event handlers to an EFBWindow.
        /// </summary>
        /// <param name="window">The window to add diagnostics to.</param>
        public static void AddDiagnostics(EFBWindow window)
        {
            if (window == null)
            {
                _logger?.Log(LogLevel.Error, "EFBWindowDiagnostics", "Cannot add diagnostics to null window");
                return;
            }

            _logger?.Log(LogLevel.Debug, "EFBWindowDiagnostics", "Adding diagnostics to window");

            try
            {
                // Add loaded event handler
                window.Loaded += (sender, e) => Window_Loaded(window, e);

                // Add resource checking and fallbacks
                if (_resourceManager != null)
                {
                    _resourceManager.EnsureCriticalResources();
                    _resourceManager.ApplyFallbackResources(window);
                    _logger?.Log(LogLevel.Debug, "EFBWindowDiagnostics", "Applied resource manager fallbacks");
                }
                else
                {
                    // Fallback to old method if resource manager is not available
                    CheckCriticalResources();
                    AddDefaultResources();
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBWindowDiagnostics", ex, "Error adding diagnostics to window");
            }
        }

        /// <summary>
        /// Handles the Loaded event of the window.
        /// </summary>
        private static void Window_Loaded(EFBWindow window, RoutedEventArgs e)
        {
            _logger?.Log(LogLevel.Debug, "EFBWindowDiagnostics", "Window loaded");

            // Log the visual tree for debugging
            LogVisualTree(window);

            // Check if PageContent exists and has content
            var pageContent = FindChild<ContentControl>(window, "PageContent");
            if (pageContent != null)
            {
                _logger?.Log(LogLevel.Debug, "EFBWindowDiagnostics", $"PageContent found, Content: {pageContent.Content?.GetType().Name ?? "null"}");
            }
            else
            {
                _logger?.Log(LogLevel.Warning, "EFBWindowDiagnostics", "PageContent not found");
            }
        }

        /// <summary>
        /// Checks if critical resources are available.
        /// </summary>
        private static void CheckCriticalResources()
        {
            _logger?.Log(LogLevel.Debug, "EFBWindowDiagnostics", "Checking critical resources");

            var criticalResources = new[] {
                "EFBPrimaryBackgroundBrush",
                "EFBSecondaryBackgroundBrush",
                "EFBPrimaryTextBrush",
                "EFBSecondaryTextBrush",
                "EFBHighlightBrush",
                "EFBPrimaryBorderBrush",
                "EFBAccentBrush",
                "BackgroundColor",
                "ForegroundColor",
                "HeaderColor",
                "SecondaryColor",
                "ButtonHoverColor",
                "ButtonPressedColor"
            };

            foreach (var resource in criticalResources)
            {
                try
                {
                    bool exists = Application.Current.Resources.Contains(resource);
                    _logger?.Log(LogLevel.Debug, "EFBWindowDiagnostics", $"Resource '{resource}' exists: {exists}");

                    if (exists)
                    {
                        var resourceValue = Application.Current.Resources[resource];
                        _logger?.Log(LogLevel.Debug, "EFBWindowDiagnostics", $"Resource '{resource}' type: {resourceValue?.GetType().Name ?? "null"}");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, "EFBWindowDiagnostics", ex, $"Error checking resource '{resource}'");
                }
            }
        }

        /// <summary>
        /// Adds default resources if they don't exist.
        /// </summary>
        private static void AddDefaultResources()
        {
            _logger?.Log(LogLevel.Debug, "EFBWindowDiagnostics", "Adding default resources");

            var defaultResources = new[] {
                ("EFBPrimaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(60, 60, 60))),
                ("EFBSecondaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(30, 30, 30))),
                ("EFBPrimaryTextBrush", new SolidColorBrush(Colors.White)),
                ("EFBSecondaryTextBrush", new SolidColorBrush(Color.FromRgb(204, 204, 204))),
                ("EFBHighlightBrush", new SolidColorBrush(Color.FromRgb(51, 153, 255)) { Opacity = 0.3 }),
                ("EFBPrimaryBorderBrush", new SolidColorBrush(Color.FromRgb(69, 69, 69))),
                ("EFBAccentBrush", new SolidColorBrush(Color.FromRgb(255, 153, 0)))
            };

            foreach (var (key, value) in defaultResources)
            {
                try
                {
                    if (!Application.Current.Resources.Contains(key))
                    {
                        Application.Current.Resources[key] = value;
                        _logger?.Log(LogLevel.Debug, "EFBWindowDiagnostics", $"Added default resource '{key}'");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, "EFBWindowDiagnostics", ex, $"Error adding default resource '{key}'");
                }
            }
        }

        /// <summary>
        /// Logs the visual tree of an element.
        /// </summary>
        /// <param name="element">The root element.</param>
        public static void LogVisualTree(DependencyObject element, int depth = 0)
        {
            if (element == null || depth > 10) // Limit depth to avoid infinite recursion
                return;

            try
            {
                string indent = new string(' ', depth * 2);
                string typeName = element.GetType().Name;

                // Get additional properties based on element type
                string additionalInfo = "";

                if (element is FrameworkElement fe)
                {
                    additionalInfo = $"Name='{fe.Name}', Visibility={fe.Visibility}, " +
                                    $"Width={fe.Width}, Height={fe.Height}, " +
                                    $"ActualWidth={fe.ActualWidth}, ActualHeight={fe.ActualHeight}";
                }

                if (element is Control control)
                {
                    additionalInfo += $", Background={control.Background}";
                }

                if (element is Panel panel)
                {
                    additionalInfo += $", Background={panel.Background}, Children={panel.Children.Count}";
                }

                if (element is ContentControl cc)
                {
                    additionalInfo += $", Content={(cc.Content != null ? cc.Content.GetType().Name : "null")}";
                }

                _logger?.Log(LogLevel.Debug, "VisualTree", $"{indent}{typeName} {additionalInfo}");

                // Recursively log children
                int childCount = VisualTreeHelper.GetChildrenCount(element);
                for (int i = 0; i < childCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(element, i);
                    LogVisualTree(child, depth + 1);
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBWindowDiagnostics", ex, "Error logging visual tree");
            }
        }

        /// <summary>
        /// Finds a child of a given type and name in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the child to find.</typeparam>
        /// <param name="parent">The parent element.</param>
        /// <param name="childName">The name of the child to find.</param>
        /// <returns>The child element, or null if not found.</returns>
        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null)
                return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // If the child is not of the requested type, recurse
                T childType = child as T;
                if (childType == null)
                {
                    // Search in the child's children
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child
                    if (foundChild != null)
                        break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    // If the child's name is set for search
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        // If the child's name is of the requested name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // Child is of the requested type with no name specified
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }
    }
}
