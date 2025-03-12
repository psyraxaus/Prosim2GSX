using ProSimSDK;
using Prosim2GSX.Models;
using System;

namespace Prosim2GSX.Services
{
    public class ProsimService : IProsimService
    {
        private readonly ProSimConnect _connection;
        private readonly ServiceModel _model;

        public bool IsConnected => _connection.isConnected;
        
        public object Connection => _connection;

        public event EventHandler<ProsimConnectionEventArgs> ConnectionChanged;

        public ProsimService(ServiceModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _connection = new ProSimConnect();
        }

        public void Connect(string hostname)
        {
            try
            {
                Logger.Log(LogLevel.Debug, "ProsimService:Connect", $"Attempting to connect to Prosim Server: {hostname}");
                _connection.Connect(hostname);
                
                OnConnectionChanged(true, "Connected to ProSim server");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimService:Connect", $"Error connecting to ProSim System: {ex.Message}");
                OnConnectionChanged(false, $"Connection failed: {ex.Message}");
                throw;
            }
        }

        public dynamic ReadDataRef(string dataRef)
        {
            try
            {
                return _connection.ReadDataRef(dataRef);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimService:ReadDataRef", $"Error reading dataRef {dataRef}: {ex.Message}");
                throw;
            }
        }

        public void SetVariable(string dataRef, object value)
        {
            try
            {
                DataRef prosimDataRef = new DataRef(dataRef, 100, _connection);
                prosimDataRef.value = value;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimService:SetVariable", $"Error setting {dataRef} to {value}: {ex.Message}");
                throw;
            }
        }

        private void OnConnectionChanged(bool isConnected, string message)
        {
            ConnectionChanged?.Invoke(this, new ProsimConnectionEventArgs(isConnected, message));
        }
    }
}
