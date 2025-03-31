using ProSimSDK;
using System;

using Prosim2GSX.Models;

namespace Prosim2GSX
{
    public class ProsimInterface
    {
        protected ServiceModel Model;
        protected ProSimConnect Connection;

        public ProsimInterface(ServiceModel model, ProSimConnect _connection)
        {
            Model = model;
            Connection = _connection;
        }

        public void ConnectProsimSDK()
        {
            try
            {
                Logger.Log(LogLevel.Debug, "ProsimInterface:ConnectProsimSDK", $"Attempting to connect to Prosim Server: {Model.ProsimHostname}");
                Connection.Connect(Model.ProsimHostname);

            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:ConnectProsimSDK", $"Error connecting to ProSim System: {ex.Message}");

            }
        }

        public bool IsProsimReady()
        {
            if (Connection.isConnected)
            {
                Logger.Log(LogLevel.Debug, "ProsimInterface:IsProsimReady", $"Connection to Prosim server established populating dataref table");
                //ParseSupportedDatarefs();
                return true;
            }
            else
            {
                return false;
            }
        }

        public dynamic GetProsimVariable(string _dataRef)
        {
            Logger.Log(LogLevel.Debug, "ProsimInterface:GetProsimVariable", $"Attempting to get dataref: {_dataRef}");
            try
            {
                return Connection.ReadDataRef(_dataRef);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:ReadDataRef", $"There was an error reading {_dataRef} - exception {ex.ToString()}");
                return null;
            }
        }

        public void SetProsimVariable(string _dataRef, object value)
        {
            DataRef dataRef = new DataRef(_dataRef, 100, Connection);
            Logger.Log(LogLevel.Debug, "ProsimInterface:SetProsimVariable", $"Attempting to set dataref: {_dataRef}");
            try
            {
                dataRef.value = value;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:SetProsimSetVariable", $"There was an error setting {_dataRef} value {value} - exception {ex.ToString()}");
            }
        }
    }
}