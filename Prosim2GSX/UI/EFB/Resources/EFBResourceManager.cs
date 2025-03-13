using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Resources
{
    /// <summary>
    /// Manages resources for the EFB UI, ensuring critical resources are available
    /// and providing fallbacks when needed.
    /// </summary>
    public class EFBResourceManager
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, object> _defaultResources;
        private ResourceDictionary _fallbackResourceDictionary;
        private bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBResourceManager"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public EFBResourceManager(ILogger logger = null)
        {
            _logger = logger;
            _defaultResources = new Dictionary<string, object>();
            _fallbackResourceDictionary = new ResourceDictionary();
            InitializeDefaultResources();
        }

        /// <summary>
        /// Gets a value indicating whether the resource manager is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the fallback resource dictionary.
        /// </summary>
        public ResourceDictionary FallbackResourceDictionary => _fallbackResourceDictionary;

        /// <summary>
        /// Initializes the default resources.
        /// </summary>
        private void InitializeDefaultResources()
        {
            _logger?.Log(LogLevel.Debug, "EFBResourceManager", "Initializing default resources");

            // Define default resources
            _defaultResources.Add("EFBPrimaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(60, 60, 60)));
            _defaultResources.Add("EFBSecondaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(30, 30, 30)));
            _defaultResources.Add("EFBPrimaryTextBrush", new SolidColorBrush(Colors.White));
            _defaultResources.Add("EFBSecondaryTextBrush", new SolidColorBrush(Color.FromRgb(204, 204, 204)));
            _defaultResources.Add("EFBHighlightBrush", new SolidColorBrush(Color.FromRgb(51, 153, 255)) { Opacity = 0.3 });
            _defaultResources.Add("EFBPrimaryBorderBrush", new SolidColorBrush(Color.FromRgb(69, 69, 69)));
            _defaultResources.Add("EFBAccentBrush", new SolidColorBrush(Color.FromRgb(255, 153, 0)));
            // Define color resources (for use in other resources)
            _defaultResources.Add("BackgroundColorValue", Color.FromRgb(60, 60, 60));
            _defaultResources.Add("ForegroundColorValue", Colors.White);
            _defaultResources.Add("HeaderColorValue", Color.FromRgb(30, 30, 30));
            _defaultResources.Add("SecondaryColorValue", Color.FromRgb(45, 45, 45));
            _defaultResources.Add("ButtonHoverColorValue", Color.FromRgb(80, 80, 80));
            _defaultResources.Add("ButtonPressedColorValue", Color.FromRgb(100, 100, 100));
            
            // Define brush resources (for use in UI elements)
            _defaultResources.Add("BackgroundColor", new SolidColorBrush(Color.FromRgb(60, 60, 60)));
            _defaultResources.Add("ForegroundColor", new SolidColorBrush(Colors.White));
            _defaultResources.Add("HeaderColor", new SolidColorBrush(Color.FromRgb(30, 30, 30)));
            _defaultResources.Add("SecondaryColor", new SolidColorBrush(Color.FromRgb(45, 45, 45)));
            _defaultResources.Add("ButtonHoverColor", new SolidColorBrush(Color.FromRgb(80, 80, 80)));
            _defaultResources.Add("ButtonPressedColor", new SolidColorBrush(Color.FromRgb(100, 100, 100)));

            _logger?.Log(LogLevel.Debug, "EFBResourceManager", $"Initialized {_defaultResources.Count} default resources");
        }

        /// <summary>
        /// Initializes the resource manager.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                _logger?.Log(LogLevel.Debug, "EFBResourceManager", "Resource manager already initialized");
                return;
            }

            try
            {
                _logger?.Log(LogLevel.Debug, "EFBResourceManager", "Initializing resource manager");

                // Create fallback resource dictionary
                _fallbackResourceDictionary = new ResourceDictionary();
                foreach (var resource in _defaultResources)
                {
                    _fallbackResourceDictionary[resource.Key] = resource.Value;
                }

                // Add fallback resource dictionary to application resources
                if (Application.Current != null && Application.Current.Resources != null)
                {
                    // Add as the first dictionary to ensure it has the lowest precedence
                    if (Application.Current.Resources.MergedDictionaries.Count > 0)
                    {
                        Application.Current.Resources.MergedDictionaries.Insert(0, _fallbackResourceDictionary);
                    }
                    else
                    {
                        Application.Current.Resources.MergedDictionaries.Add(_fallbackResourceDictionary);
                    }

                    _logger?.Log(LogLevel.Debug, "EFBResourceManager", "Added fallback resource dictionary to application resources");
                }
                else
                {
                    _logger?.Log(LogLevel.Warning, "EFBResourceManager", "Application.Current or Application.Current.Resources is null");
                }

                _isInitialized = true;
                _logger?.Log(LogLevel.Debug, "EFBResourceManager", "Resource manager initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBResourceManager", ex, "Error initializing resource manager");
            }
        }

        /// <summary>
        /// Ensures that all critical resources are available, adding fallbacks if needed.
        /// </summary>
        public void EnsureCriticalResources()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            _logger?.Log(LogLevel.Debug, "EFBResourceManager", "Ensuring critical resources are available");

            try
            {
                // Check if critical resources exist in application resources
                foreach (var resource in _defaultResources)
                {
                    EnsureResourceExists(resource.Key, resource.Value);
                }

                _logger?.Log(LogLevel.Debug, "EFBResourceManager", "Critical resources check completed");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBResourceManager", ex, "Error ensuring critical resources");
            }
        }

        /// <summary>
        /// Ensures that a specific resource exists, adding a fallback if needed.
        /// </summary>
        /// <param name="resourceKey">The resource key.</param>
        /// <param name="defaultValue">The default value.</param>
        public void EnsureResourceExists(string resourceKey, object defaultValue)
        {
            try
            {
                bool exists = false;

                // Check if the resource exists in application resources
                if (Application.Current != null && Application.Current.Resources != null)
                {
                    exists = Application.Current.Resources.Contains(resourceKey);
                }

                if (!exists)
                {
                    _logger?.Log(LogLevel.Warning, "EFBResourceManager", $"Resource '{resourceKey}' not found, adding default value");

                    // Add to application resources
                    if (Application.Current != null && Application.Current.Resources != null)
                    {
                        Application.Current.Resources[resourceKey] = defaultValue;
                    }

                    // Also add to fallback dictionary if not already there
                    if (!_fallbackResourceDictionary.Contains(resourceKey))
                    {
                        _fallbackResourceDictionary[resourceKey] = defaultValue;
                    }
                }
                else
                {
                    _logger?.Log(LogLevel.Debug, "EFBResourceManager", $"Resource '{resourceKey}' exists");
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBResourceManager", ex, $"Error ensuring resource '{resourceKey}' exists");
            }
        }

        /// <summary>
        /// Gets a default resource value for a given resource key.
        /// </summary>
        /// <param name="resourceKey">The resource key.</param>
        /// <returns>The default resource value, or null if not found.</returns>
        public object GetDefaultResource(string resourceKey)
        {
            if (_defaultResources.TryGetValue(resourceKey, out object value))
            {
                return value;
            }

            _logger?.Log(LogLevel.Warning, "EFBResourceManager", $"No default value defined for resource '{resourceKey}'");
            return null;
        }

        /// <summary>
        /// Applies the fallback resources to a specific framework element.
        /// </summary>
        /// <param name="element">The element to apply resources to.</param>
        public void ApplyFallbackResources(FrameworkElement element)
        {
            if (element == null)
            {
                _logger?.Log(LogLevel.Warning, "EFBResourceManager", "Cannot apply fallback resources to null element");
                return;
            }

            try
            {
                _logger?.Log(LogLevel.Debug, "EFBResourceManager", $"Applying fallback resources to {element.GetType().Name}");

                // Create a new resource dictionary for the element if it doesn't have one
                if (element.Resources == null)
                {
                    element.Resources = new ResourceDictionary();
                }

                // Add fallback resources to the element's resource dictionary
                foreach (var resource in _defaultResources)
                {
                    if (!element.Resources.Contains(resource.Key))
                    {
                        element.Resources[resource.Key] = resource.Value;
                    }
                }

                _logger?.Log(LogLevel.Debug, "EFBResourceManager", "Fallback resources applied successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBResourceManager", ex, "Error applying fallback resources");
            }
        }

        /// <summary>
        /// Logs the status of all critical resources.
        /// </summary>
        public void LogResourceStatus()
        {
            if (_logger == null)
                return;

            _logger.Log(LogLevel.Debug, "EFBResourceManager", "Logging resource status");

            try
            {
                foreach (var resource in _defaultResources)
                {
                    bool existsInApp = false;
                    bool existsInFallback = false;

                    // Check if the resource exists in application resources
                    if (Application.Current != null && Application.Current.Resources != null)
                    {
                        existsInApp = Application.Current.Resources.Contains(resource.Key);
                    }

                    // Check if the resource exists in fallback dictionary
                    existsInFallback = _fallbackResourceDictionary.Contains(resource.Key);

                    _logger.Log(LogLevel.Debug, "EFBResourceManager", 
                        $"Resource '{resource.Key}': App={existsInApp}, Fallback={existsInFallback}");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "EFBResourceManager", ex, "Error logging resource status");
            }
        }
    }
}
