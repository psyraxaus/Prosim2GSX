﻿using ProSimSDK;
using System;
using Prosim2GSX.Models;
using Prosim2GSX.Services;

namespace Prosim2GSX
{
    public class ProsimInterface
    {
        protected ServiceModel Model;
        protected IProsimService ProsimService;
        protected ProSimConnect Connection;

        public ProsimInterface(ServiceModel model, ProSimConnect connection)
        {
            Model = model;
            Connection = connection;
            ProsimService = new ProsimService(model);
            
            ProsimService.ConnectionChanged += (sender, args) => {
                if (args.IsConnected)
                {
                    Logger.Log(LogLevel.Debug, "ProsimInterface", $"Connection to Prosim server established: {args.Message}");
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ProsimInterface", $"Prosim connection issue: {args.Message}");
                }
            };
        }

        public void ConnectProsimSDK()
        {
            try
            {
                ProsimService.Connect(Model.ProsimHostname);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:ConnectProsimSDK", $"Error connecting to ProSim System: {ex.Message}");
            }
        }

        public bool IsProsimReady()
        {
            if (ProsimService.IsConnected)
            {
                Logger.Log(LogLevel.Debug, "ProsimInterface:IsProsimReady", $"Connection to Prosim server established");
                return true;
            }
            else
            {
                return false;
            }
        }

        public dynamic ReadDataRef(string dataRef)
        {
            try
            {
                return ProsimService.ReadDataRef(dataRef);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:ReadDataRef", $"There was an error reading {dataRef} - exception {ex.Message}");
                return null;
            }
        }

        public void SetProsimVariable(string dataRef, object value)
        {
            try
            {
                ProsimService.SetVariable(dataRef, value);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:SetProsimVariable", $"There was an error setting {dataRef} value {value} - exception {ex.Message}");
            }
        }

        public object GetProsimVariable(string dataRef)
        {
            Logger.Log(LogLevel.Debug, "ProsimInterface:GetProsimVariable", $"Attempting to get {dataRef}");
            try
            {
                dynamic value = ProsimService.ReadDataRef(dataRef);
                Logger.Log(LogLevel.Debug, "ProsimInterface:GetProsimVariable", $"Dataref {value}");
                return value;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:GetProsimVariable", $"Error getting {dataRef} - exception {ex.Message}");
                return null;
            }
        }
    }
}
