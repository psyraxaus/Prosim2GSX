using Microsoft.FlightSimulator.SimConnect;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of GSX menu service
    /// </summary>
    public class GsxMenuService : IGsxMenuService
    {
        private readonly IGsxSimConnectService _simConnectService;
        private readonly string _menuFile;
        private bool _operatorWasSelected = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxMenuService(IGsxSimConnectService simConnectService, string menuFile)
        {
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
            _menuFile = menuFile;
        }

        /// <inheritdoc/>
        public void OpenMenu()
        {
            _simConnectService.WriteGsxLvar("FSDT_GSX_MENU_OPEN", 1);
            LogService.Log(LogLevel.Debug, nameof(GsxMenuService), "Opening GSX Menu", LogCategory.Menu);
        }

        /// <inheritdoc/>
        public void SelectMenuItem(int index, bool waitForMenu = true)
        {
            if (waitForMenu)
                WaitForMenuReady();

            LogService.Log(LogLevel.Debug, nameof(GsxMenuService),
                $"Selecting Menu Option {index} (L-Var Value {index - 1})", LogCategory.Menu);
            _simConnectService.WriteGsxLvar("FSDT_GSX_MENU_CHOICE", index - 1);

            // Small delay after selection to allow GSX to process
            Thread.Sleep(100);
        }

        /// <inheritdoc/>
        public void WaitForMenuReady()
        {
            // Reduce max iterations from 100 to 30 (3 seconds max)
            int counter = 0;
            int maxIterations = 30;
            int sleepTime = 100;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (counter < maxIterations)
            {
                // Pause execution
                Thread.Sleep(sleepTime);
                counter++;

                // Check if the GSX menu is ready
                if (_simConnectService.ReadGsxLvar("FSDT_GSX_MENU_READY") == 1)
                    break;
            }

            stopwatch.Stop();

            LogService.Log(LogLevel.Debug, nameof(GsxMenuService),
                $"Menu wait ended after {stopwatch.ElapsedMilliseconds}ms ({counter} iterations)", LogCategory.Menu);

            // Warn if we hit the maximum iterations
            if (counter >= maxIterations)
            {
                LogService.Log(LogLevel.Warning, nameof(GsxMenuService),
                    $"WaitForMenuReady timed out after {maxIterations * sleepTime}ms");
            }
        }

        /// <inheritdoc/>
        public int IsOperatorSelectionActive()
        {
            int result = -1;

            if (!string.IsNullOrEmpty(_menuFile) && File.Exists(_menuFile))
            {
                try
                {
                    string[] lines = File.ReadLines(_menuFile).ToArray();
                    if (lines.Length > 1)
                    {
                        if (!string.IsNullOrEmpty(lines[0]) &&
                            (lines[0] == "Select handling operator" || lines[0] == "Select catering operator"))
                        {
                            LogService.Log(LogLevel.Debug, nameof(GsxMenuService),
                                $"Match found for operator Selection: '{lines[0]}'", LogCategory.Menu);
                            result = 1;
                        }
                        else if (string.IsNullOrEmpty(lines[0]))
                        {
                            LogService.Log(LogLevel.Debug, nameof(GsxMenuService),
                                $"Line is empty! Lines total: {lines.Length}", LogCategory.Menu);
                            result = -1;
                        }
                        else
                        {
                            LogService.Log(LogLevel.Debug, nameof(GsxMenuService),
                                $"No Match found for operator Selection: '{lines[0]}'", LogCategory.Menu);
                            result = 0;
                        }
                    }
                    else
                    {
                        LogService.Log(LogLevel.Debug, nameof(GsxMenuService),
                            $"Menu Lines not above 1 ({lines.Length})", LogCategory.Menu);
                    }
                }
                catch (Exception ex)
                {
                    LogService.Log(LogLevel.Error, nameof(GsxMenuService),
                        $"Error reading menu file: {ex.Message}");
                    result = -1;
                }
            }
            else
            {
                LogService.Log(LogLevel.Debug, nameof(GsxMenuService),
                    $"Menu File was empty or not found", LogCategory.Menu);
            }

            return result;
        }

        /// <inheritdoc/>
        public bool HandleOperatorSelection(int operatorDelay = 500)  // Reduced from 2000 to 500
        {
            // Remove the fixed 2-second delay here
            // Thread.Sleep(2000);  - REMOVE THIS LINE

            int result = IsOperatorSelectionActive();
            if (result == -1)
            {
                LogService.Log(LogLevel.Information, nameof(GsxMenuService),
                    $"Waiting {operatorDelay}ms for Operator Selection");
                Thread.Sleep(operatorDelay);

                // Check again after waiting
                result = IsOperatorSelectionActive();
            }

            if (result == 1)
            {
                LogService.Log(LogLevel.Information, nameof(GsxMenuService),
                    $"Operator Selection active, choosing Option 1");
                SelectMenuItem(1);
                _operatorWasSelected = true;
                return true;
            }
            else
            {
                LogService.Log(LogLevel.Information, nameof(GsxMenuService),
                    $"No Operator Selection needed");
                return false;
            }
        }

        /// <summary>
        /// Was an operator selected in the last operation
        /// </summary>
        /// <returns>True if an operator was selected</returns>
        public bool WasOperatorSelected()
        {
            bool result = _operatorWasSelected;
            _operatorWasSelected = false;
            return result;
        }
    }
}