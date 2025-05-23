using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Windows.Gaming.Input;

namespace Prosim2GSX.Services.PTT.Helpers
{
    /// <summary>
    /// Resolves joystick display names by improving generic names and adding controller index
    /// </summary>
    public static class JoystickNameResolver
    {
        private static readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger(typeof(JoystickNameResolver));

        // Cache resolved names to avoid repeated processing
        private static readonly ConcurrentDictionary<string, string> _nameCache = new();

        /// <summary>
        /// Resolves a better name for a joystick controller with index
        /// </summary>
        /// <param name="controller">The RawGameController instance</param>
        /// <param name="controllerIndex">The index of this controller (0-based)</param>
        /// <returns>Enhanced name with controller number</returns>
        public static string ResolveJoystickName(RawGameController controller, int controllerIndex)
        {
            if (controller == null)
                return $"Unknown Device #{controllerIndex + 1}";

            string originalName = controller.DisplayName ?? "Unknown Device";
            string cacheKey = $"{originalName}_{controllerIndex}";

            // Return cached result if available
            if (_nameCache.TryGetValue(cacheKey, out string cachedName))
                return cachedName;

            try
            {
                // Try to improve the name
                string improvedName = ImproveDeviceName(originalName);

                // Always add controller index for clarity
                string finalName = $"{improvedName} #{controllerIndex + 1}";

                _nameCache[cacheKey] = finalName;
                _logger.LogDebug("Resolved joystick name: {Original} -> {Final}", originalName, finalName);
                return finalName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve joystick name for {Name}", originalName);

                // Fallback with controller number
                string fallbackName = $"{originalName} #{controllerIndex + 1}";
                _nameCache[cacheKey] = fallbackName;
                return fallbackName;
            }
        }

        /// <summary>
        /// Improves generic device names with more user-friendly alternatives
        /// </summary>
        private static string ImproveDeviceName(string originalName)
        {
            if (string.IsNullOrEmpty(originalName))
                return "Gaming Controller";

            // Dictionary of common generic names and their improvements
            var improvements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HID-compliant game controller", "Gaming Controller" },
                { "HID-compliant device", "USB Controller" },
                { "USB Input Device", "USB Controller" },
                { "Generic USB Joystick", "USB Joystick" },
                { "Wireless Controller", "Wireless Gamepad" },
                { "Controller (XBOX 360 For Windows)", "Xbox 360 Controller" },
                { "Controller (Xbox One For Windows)", "Xbox One Controller" },
                { "Controller (Xbox Wireless Controller)", "Xbox Wireless Controller" }
            };

            // Check for exact matches first
            if (improvements.TryGetValue(originalName, out string improved))
                return improved;

            // Check for partial matches
            foreach (var kvp in improvements)
            {
                if (originalName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            // If no improvement found, return original name
            return originalName;
        }

        /// <summary>
        /// Clears the name resolution cache
        /// </summary>
        public static void ClearCache()
        {
            _nameCache.Clear();
            _logger.LogDebug("Joystick name cache cleared");
        }
    }
}
