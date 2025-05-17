using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Utility class for loading DLLs dynamically
    /// </summary>
    public static class DllLoader
    {
        // Static logger instance used by the methods
        private static ILogger _logger;

        /// <summary>
        /// Initializes the DllLoader with a logger
        /// </summary>
        /// <param name="logger">The logger to use</param>
        public static void Initialize(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        /// <summary>
        /// Adds a directory to the DLL search path
        /// </summary>
        /// <param name="directory">Directory to add</param>
        /// <returns>True if successful</returns>
        public static bool AddDllDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return false;

            try
            {
                return SetDllDirectory(directory);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding DLL directory {Directory}", directory);
                return false;
            }
        }

        /// <summary>
        /// Loads a DLL from a specific path
        /// </summary>
        /// <param name="dllPath">Full path to the DLL</param>
        /// <returns>Handle to the loaded DLL or IntPtr.Zero on failure</returns>
        public static IntPtr LoadDll(string dllPath)
        {
            if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
                return IntPtr.Zero;

            try
            {
                // First try setting the directory
                string directory = Path.GetDirectoryName(dllPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    AddDllDirectory(directory);
                }

                // Then load the DLL directly
                return LoadLibrary(dllPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading DLL {DllPath}", dllPath);
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Gets a function pointer from a loaded DLL
        /// </summary>
        /// <param name="dllHandle">Handle to the loaded DLL</param>
        /// <param name="functionName">Name of the function</param>
        /// <returns>Function pointer or IntPtr.Zero on failure</returns>
        public static IntPtr GetFunctionPointer(IntPtr dllHandle, string functionName)
        {
            if (dllHandle == IntPtr.Zero || string.IsNullOrEmpty(functionName))
                return IntPtr.Zero;

            try
            {
                return GetProcAddress(dllHandle, functionName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting function pointer for {FunctionName}", functionName);
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Frees a loaded DLL
        /// </summary>
        /// <param name="dllHandle">Handle to the loaded DLL</param>
        /// <returns>True if successful</returns>
        public static bool FreeDll(IntPtr dllHandle)
        {
            if (dllHandle == IntPtr.Zero)
                return false;

            try
            {
                return FreeLibrary(dllHandle);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error freeing DLL");
                return false;
            }
        }
    }
}
