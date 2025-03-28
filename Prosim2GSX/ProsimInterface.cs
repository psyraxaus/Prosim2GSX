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

        public dynamic ReadProsimVariable(string _dataRef)
        {
            //Logger.Log(LogLevel.Debug, "ProsimInterface:ReadDataRef", $"Dataref {_dataRef} - typeof {Connection.ReadDataRef(_dataRef).GetType()}");
            try
            {
                return Connection.ReadDataRef(_dataRef);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:ReadDataRef", $"There was an error reading {_dataRef} - exception {ex.ToString()}");
                return null;
            }

            //            return Connection.ReadDataRef(_dataRef);
        }

        public void SetProsimVariable(string _dataRef, object value)
        {
            DataRef dataRef = new DataRef(_dataRef, 100, Connection);
            try
            {
                dataRef.value = value;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:SetProsimSetVariable", $"There was an error setting {_dataRef} value {value} - exception {ex.ToString()}");
            }
        }

        public object GetProsimVariable(string _dataRef)
        {
            Logger.Log(LogLevel.Debug, "ProsimInterface:GetProsimVariable", $"Attempting to get {_dataRef}");
            //Connection.ReadDataRef( _dataRef );
            DataRef dataRef = new DataRef(_dataRef, 100, Connection);
            Logger.Log(LogLevel.Debug, "ProsimInterface:GetProsimVariable", $"Dataref {(string)dataRef.value}");
            return dataRef.value;

        }
    }
}