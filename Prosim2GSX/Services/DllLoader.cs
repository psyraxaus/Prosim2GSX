using System;
using System.IO;
using System.Runtime.InteropServices;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Utility class for loading DLLs dynamically
    /// </summary>
    public static class DllLoader
    {
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
                LogService.Log(LogLevel.Error, nameof(DllLoader),
                    $"Error adding DLL directory {directory}: {ex.Message}");
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
                LogService.Log(LogLevel.Error, nameof(DllLoader),
                    $"Error loading DLL {dllPath}: {ex.Message}");
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
                LogService.Log(LogLevel.Error, nameof(DllLoader),
                    $"Error getting function pointer for {functionName}: {ex.Message}");
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
                LogService.Log(LogLevel.Error, nameof(DllLoader),
                    $"Error freeing DLL: {ex.Message}");
                return false;
            }
        }
    }
}