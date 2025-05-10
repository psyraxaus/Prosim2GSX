using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Xml;
using Newtonsoft.Json.Linq;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class FlightPlanService : IFlightPlanService
    {
        private readonly IProsimInterface _prosimInterface;
        private readonly ServiceModel _model;
        private FlightPlan _flightPlan;

        public string FlightPlanID { get; private set; } = "0";
        public string FlightNumber { get; private set; } = "0";

        public FlightPlanService(IProsimInterface prosimInterface, ServiceModel model)
        {
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void SetFlightPlan(FlightPlan flightPlan)
        {
            _flightPlan = flightPlan ?? throw new ArgumentNullException(nameof(flightPlan));
        }

        public bool IsFlightplanLoaded()
        {
            if (_model.FlightPlanType == "MCDU")
            {
                return !string.IsNullOrEmpty((string)_prosimInterface.GetProsimVariable("aircraft.fms.destination"));
            }
            else
            {
                return !string.IsNullOrEmpty((string)_prosimInterface.GetProsimVariable("efb.prelimloadsheet"));
            }
        }

        public string GetFMSFlightNumber()
        {
            try
            {
                string flightNumber = string.Empty;
                var fmsXmlstr = _prosimInterface.GetProsimVariable("aircraft.fms.flightPlanXml");

                if (string.IsNullOrEmpty(fmsXmlstr))
                {
                    LogService.Log(LogLevel.Debug, nameof(FlightPlanService), "No flight plan XML available");
                    return string.Empty;
                }

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(fmsXmlstr);
                XmlNode flightNumberNode = xmlDoc.SelectSingleNode("/fms/routeData/flightNumber");

                if (flightNumberNode != null && !string.IsNullOrEmpty(flightNumberNode.InnerText))
                {
                    flightNumber = flightNumberNode.InnerText;
                    LogService.Log(LogLevel.Debug, nameof(FlightPlanService), $"Flight Number: {flightNumber}");
                }
                else
                {
                    LogService.Log(LogLevel.Debug, nameof(FlightPlanService), "No Flight number loaded in FMS");
                }

                return flightNumber;
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(FlightPlanService), $"Error getting FMS flight number: {ex.Message}");
                return string.Empty;
            }
        }

        public void Update(bool forceCurrent)
        {
            try
            {
                if (_model.FlightPlanType == "MCDU")
                {
                    if (_flightPlan == null)
                    {
                        LogService.Log(LogLevel.Warning, nameof(FlightPlanService), "Flight plan not set");
                        return;
                    }

                    if (FlightPlanID != _flightPlan.FlightPlanID)
                    {
                        LogService.Log(LogLevel.Information, nameof(FlightPlanService),
                            $"New FlightPlan with ID {_flightPlan.FlightPlanID} detected!");
                        FlightPlanID = _flightPlan.FlightPlanID;
                        FlightNumber = _flightPlan.Flight;
                    }
                }
                else
                {
                    JObject result = JObject.Parse((string)_prosimInterface.GetProsimVariable("efb.flightTimestampJSON"));
                    string innerJson = result.ToString();
                    result = JObject.Parse(innerJson);
                    innerJson = result["ProsimTimes"]["PRELIM_EDNO"].ToString();

                    if (FlightPlanID != innerJson)
                    {
                        LogService.Log(LogLevel.Information, nameof(FlightPlanService),
                            $"New FlightPlan with ID {innerJson} detected!");
                        FlightPlanID = innerJson;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(FlightPlanService), $"Exception during Update: {ex.Message}");
            }
        }

        public bool IsLoadsheetAvailable(string type)
        {
            try
            {
                var loadsheetData = _prosimInterface.GetProsimVariable($"efb.{type.ToLower()}Loadsheet");
                return loadsheetData != null && !string.IsNullOrEmpty(loadsheetData.ToString());
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(FlightPlanService),
                    $"Error checking {type} loadsheet: {ex.Message}");
                return false;
            }
        }

        public dynamic GetLoadsheetData(string type)
        {
            try
            {
                return _prosimInterface.GetProsimVariable($"efb.{type.ToLower()}Loadsheet");
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(FlightPlanService),
                    $"Error getting {type} loadsheet data: {ex.Message}");
                return null;
            }
        }

        public bool EnginesRunning
        {
            get
            {
                double engine1 = _prosimInterface.GetProsimVariable("aircraft.engine1.raw");
                double engine2 = _prosimInterface.GetProsimVariable("aircraft.engine2.raw");
                return engine1 > 18.0D || engine2 > 18.0D;
            }
        }
    }
}