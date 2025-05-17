using Microsoft.Extensions.Logging;
using Prosim2GSX.Models;
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
        private readonly ServiceModel _model;
        private readonly MobiSimConnect _mobiSimConnect;
        private readonly string _menuFile;
        private readonly ILogger<GsxMenuService> _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxMenuService(
            ILogger<GsxMenuService> logger,
            IGsxSimConnectService simConnectService,
            string menuFile,
            ServiceModel model,
            MobiSimConnect mobiSimConnect)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
            _menuFile = menuFile ?? throw new ArgumentNullException(nameof(menuFile));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _mobiSimConnect = mobiSimConnect ?? throw new ArgumentNullException(nameof(mobiSimConnect));

            _logger.LogInformation("GSX Menu Service initialized");
        }

        /// <inheritdoc/>
        public void OpenMenu()
        {
            _mobiSimConnect.IsGsxMenuReady = false;
            _simConnectService.WriteGsxLvar("FSDT_GSX_MENU_OPEN", 1);
            _logger.LogDebug("Opening GSX Menu");
        }

        /// <inheritdoc/>
        public void SelectMenuItem(int index, bool waitForMenu = true)
        {
            if (waitForMenu)
                WaitForMenuReady();
            _mobiSimConnect.IsGsxMenuReady = false;
            _logger.LogDebug("Selecting Menu Option {Index} (L-Var Value {LvarValue})", index, index - 1);
            _simConnectService.WriteGsxLvar("FSDT_GSX_MENU_CHOICE", index - 1);

            // Small delay after selection to allow GSX to process
            Thread.Sleep(100);
        }

        /// <inheritdoc/>
        public void WaitForMenuReady()
        {
            int counter = 0;
            while (!_mobiSimConnect.IsGsxMenuReady && counter < 1000) { Thread.Sleep(100); counter++; }
            _logger.LogDebug("Wait ended after {WaitTime}ms", counter * 100);
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
                            _logger.LogDebug("Match found for operator Selection: '{Line}'", lines[0]);
                            result = 1;
                        }
                        else if (string.IsNullOrEmpty(lines[0]))
                        {
                            _logger.LogDebug("Line is empty! Lines total: {LinesCount}", lines.Length);
                            result = -1;
                        }
                        else
                        {
                            _logger.LogDebug("No Match found for operator Selection: '{Line}'", lines[0]);
                            result = 0;
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Menu Lines not above 1 ({LinesCount})", lines.Length);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading menu file");
                    result = -1;
                }
            }
            else
            {
                _logger.LogDebug("Menu File was empty or not found");
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
                _logger.LogInformation("Waiting {Delay}ms for Operator Selection", _model.OperatorDelay);
                Thread.Sleep((int)_model.OperatorDelay);
            }

            if (result == 1)
            {
                _logger.LogInformation("Operator Selection active, choosing Option 1");
                SelectMenuItem(1);
            }
            else
            {
                _logger.LogInformation("No Operator Selection needed");
            }
        }
    }
}
