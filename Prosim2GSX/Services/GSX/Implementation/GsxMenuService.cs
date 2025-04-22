using Microsoft.FlightSimulator.SimConnect;
using Prosim2GSX.Services.GSX.Interfaces;
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
            Logger.Log(LogLevel.Debug, nameof(GsxMenuService), "Opening GSX Menu");
        }

        /// <inheritdoc/>
        public void SelectMenuItem(int index, bool waitForMenu = true)
        {
            if (waitForMenu)
                WaitForMenuReady();

            Logger.Log(LogLevel.Debug, nameof(GsxMenuService),
                $"Selecting Menu Option {index} (L-Var Value {index - 1})");
            _simConnectService.WriteGsxLvar("FSDT_GSX_MENU_CHOICE", index - 1);
        }

        /// <inheritdoc/>
        public void WaitForMenuReady()
        {
            int counter = 0;
            while (counter < 100)
            {
                // Pause execution
                Thread.Sleep(100);
                counter++;

                // Check if the GSX menu is ready
                // This would normally use SimConnect.IsGsxMenuReady, but we'll adapt for our service
                if (_simConnectService.ReadGsxLvar("FSDT_GSX_MENU_READY") == 1)
                    break;
            }

            Logger.Log(LogLevel.Debug, nameof(GsxMenuService),
                $"Menu wait ended after {counter * 100}ms");
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
                            Logger.Log(LogLevel.Debug, nameof(GsxMenuService),
                                $"Match found for operator Selection: '{lines[0]}'");
                            result = 1;
                        }
                        else if (string.IsNullOrEmpty(lines[0]))
                        {
                            Logger.Log(LogLevel.Debug, nameof(GsxMenuService),
                                $"Line is empty! Lines total: {lines.Length}");
                            result = -1;
                        }
                        else
                        {
                            Logger.Log(LogLevel.Debug, nameof(GsxMenuService),
                                $"No Match found for operator Selection: '{lines[0]}'");
                            result = 0;
                        }
                    }
                    else
                    {
                        Logger.Log(LogLevel.Debug, nameof(GsxMenuService),
                            $"Menu Lines not above 1 ({lines.Length})");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, nameof(GsxMenuService),
                        $"Error reading menu file: {ex.Message}");
                    result = -1;
                }
            }
            else
            {
                Logger.Log(LogLevel.Debug, nameof(GsxMenuService),
                    $"Menu File was empty or not found");
            }

            return result;
        }

        /// <inheritdoc/>
        public bool HandleOperatorSelection(int operatorDelay = 2000)
        {
            Thread.Sleep(2000);

            int result = IsOperatorSelectionActive();
            if (result == -1)
            {
                Logger.Log(LogLevel.Information, nameof(GsxMenuService),
                    $"Waiting {operatorDelay}ms for Operator Selection");
                Thread.Sleep(operatorDelay);

                // Check again after waiting
                result = IsOperatorSelectionActive();
            }

            if (result == 1)
            {
                Logger.Log(LogLevel.Information, nameof(GsxMenuService),
                    $"Operator Selection active, choosing Option 1");
                SelectMenuItem(1);
                _operatorWasSelected = true;
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Information, nameof(GsxMenuService),
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