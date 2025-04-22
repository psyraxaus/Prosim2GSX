using Microsoft.Win32;
using System;
using System.IO;

namespace Prosim2GSX.Services.GSX
{
    /// <summary>
    /// Helper methods for GSX functionality
    /// </summary>
    public static class GsxHelpers
    {
        private static readonly string _pathMenuFile = @"\MSFS\fsdreamteam-gsx-pro\html_ui\InGamePanels\FSDT_GSX_Panel\menu";
        private static readonly string _registryPath = @"HKEY_CURRENT_USER\SOFTWARE\FSDreamTeam";
        private static readonly string _registryValue = @"root";

        /// <summary>
        /// Get the path to the GSX menu file
        /// </summary>
        /// <returns>Full path to the GSX menu file, or empty string if not found</returns>
        public static string GetGsxMenuFilePath()
        {
            try
            {
                // Get the GSX root path from registry
                string rootPath = (string)Registry.GetValue(_registryPath, _registryValue, null);

                if (string.IsNullOrEmpty(rootPath))
                {
                    Logger.Log(LogLevel.Warning, nameof(GsxHelpers),
                        "GSX root path not found in registry");
                    return string.Empty;
                }

                // Construct the full menu file path
                string menuPath = rootPath + _pathMenuFile;

                // Check if the path exists
                if (File.Exists(menuPath))
                {
                    return menuPath;
                }
                else
                {
                    Logger.Log(LogLevel.Warning, nameof(GsxHelpers),
                        $"GSX menu file not found at {menuPath}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxHelpers),
                    $"Error getting GSX menu file path: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Check if GSX is installed and available
        /// </summary>
        /// <returns>True if GSX is available</returns>
        public static bool IsGsxAvailable()
        {
            try
            {
                // Check if the process is running
                bool processRunning = System.Diagnostics.Process.GetProcessesByName("Couatl64_MSFS").Length > 0;

                // Check if the menu file exists
                bool menuFileExists = !string.IsNullOrEmpty(GetGsxMenuFilePath());

                return processRunning && menuFileExists;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxHelpers),
                    $"Error checking GSX availability: {ex.Message}");
                return false;
            }
        }
    }
}