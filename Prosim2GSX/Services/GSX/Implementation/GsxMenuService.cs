using Prosim2GSX.Models;
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
        private readonly ServiceModel _model;
        private readonly MobiSimConnect _mobiSimConnect;
        private readonly string _menuFile;

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxMenuService(IGsxSimConnectService simConnectService, string menuFile, ServiceModel model, MobiSimConnect mobiSimConnect)
        {
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
            _menuFile = menuFile ?? throw new ArgumentNullException(nameof(menuFile));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _mobiSimConnect = mobiSimConnect ?? throw new ArgumentNullException(nameof(mobiSimConnect));
        }

        /// <inheritdoc/>
        public void OpenMenu()
        {
            _mobiSimConnect.IsGsxMenuReady = false;
            _simConnectService.WriteGsxLvar("FSDT_GSX_MENU_OPEN", 1);
            LogService.Log(LogLevel.Debug, nameof(GsxMenuService), "Opening GSX Menu", LogCategory.Menu);
        }

        /// <inheritdoc/>
        public void SelectMenuItem(int index, bool waitForMenu = true)
        {
            if (waitForMenu)
                WaitForMenuReady();
            _mobiSimConnect.IsGsxMenuReady = false;
            LogService.Log(LogLevel.Debug, nameof(GsxMenuService),
                $"Selecting Menu Option {index} (L-Var Value {index - 1})", LogCategory.Menu);
            _simConnectService.WriteGsxLvar("FSDT_GSX_MENU_CHOICE", index - 1);

            // Small delay after selection to allow GSX to process
            Thread.Sleep(100);
        }

        /// <inheritdoc/>
        public void WaitForMenuReady()
        {

            int counter = 0;
            while (!_mobiSimConnect.IsGsxMenuReady && counter < 1000) { Thread.Sleep(100); counter++; }
            LogService.Log(LogLevel.Debug, nameof(GsxMenuService), 
                $"Wait ended after {counter * 100}ms", LogCategory.Menu);
        }

        /// <inheritdoc/>
        public int IsOperatorSelectionActive()
        {
            int result = -1;

            if (!string.IsNullOrEmpty(_menuFile))
            {
                try
                {
                    string[] lines = File.ReadLines(_menuFile).ToArray();
                    if (lines.Length > 1)
                    {
                        if (!string.IsNullOrEmpty(lines[0]) && (lines[0] == "Select handling operator" || lines[0] == "Select catering operator"))
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
        public void HandleOperatorSelection()
        {
            Thread.Sleep(2000);

            int result = IsOperatorSelectionActive();
            if (result == -1)
            {
                LogService.Log(LogLevel.Information, nameof(GsxMenuService),
                    $"Waiting {_model.OperatorDelay}ms for Operator Selection");
                Thread.Sleep((int)_model.OperatorDelay);

            }

            if (result == 1)
            {
                LogService.Log(LogLevel.Information, nameof(GsxMenuService),
                    $"Operator Selection active, choosing Option 1");
                SelectMenuItem(1);
            }
            else
            {
                LogService.Log(LogLevel.Information, nameof(GsxMenuService),
                    $"No Operator Selection needed");
            }
        }
    }
}