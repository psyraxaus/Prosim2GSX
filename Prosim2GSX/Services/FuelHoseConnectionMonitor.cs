using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Monitors the connection status of the fuel hose
    /// </summary>
    public class FuelHoseConnectionMonitor
    {
        private readonly MobiSimConnect _simConnect;
        private readonly ILogger _logger;
        private bool _isConnected;
        
        /// <summary>
        /// Event raised when the fuel hose connection status changes
        /// </summary>
        public event EventHandler<bool> ConnectionChanged;
        
        /// <summary>
        /// Gets a value indicating whether the fuel hose is connected
        /// </summary>
        public bool IsConnected => _isConnected;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FuelHoseConnectionMonitor"/> class
        /// </summary>
        /// <param name="simConnect">The SimConnect instance</param>
        /// <param name="logger">The logger</param>
        public FuelHoseConnectionMonitor(MobiSimConnect simConnect, ILogger logger)
        {
            _simConnect = simConnect ?? throw new ArgumentNullException(nameof(simConnect));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isConnected = false;
        }
        
        /// <summary>
        /// Checks the current connection status of the fuel hose
        /// </summary>
        public void CheckConnection()
        {
            try
            {
                bool isConnected = _simConnect.ReadLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1;
                
                if (isConnected != _isConnected)
                {
                    _isConnected = isConnected;
                    
                    _logger.Log(LogLevel.Information, "FuelHoseConnectionMonitor:CheckConnection", 
                        $"Fuel hose connection changed to {(_isConnected ? "connected" : "disconnected")}");
                    
                    OnConnectionChanged(_isConnected);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "FuelHoseConnectionMonitor:CheckConnection", 
                    $"Error checking fuel hose connection: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Raises the ConnectionChanged event
        /// </summary>
        /// <param name="isConnected">Whether the fuel hose is connected</param>
        protected virtual void OnConnectionChanged(bool isConnected)
        {
            try
            {
                ConnectionChanged?.Invoke(this, isConnected);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "FuelHoseConnectionMonitor:OnConnectionChanged", 
                    $"Error raising ConnectionChanged event: {ex.Message}");
            }
        }
    }
}
