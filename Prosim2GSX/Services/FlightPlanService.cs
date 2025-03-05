using Prosim2GSX.Behaviours;
using Prosim2GSX.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace Prosim2GSX.Services
{
    public class FlightPlanService : IFlightPlanService
    {
        private readonly ServiceModel _model;
        private readonly HttpClient _httpClient;
        private XmlNode _currentFlightPlanData;
        private string _lastFlightPlanId = string.Empty;
        
        public event EventHandler<FlightPlanEventArgs> FlightPlanLoaded;
        
        public FlightPlanService(ServiceModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _httpClient = new HttpClient();
        }
        
        public async Task<bool> LoadFlightPlanAsync()
        {
            try
            {
                XmlNode flightPlanData = await GetFlightPlanDataAsync();
                if (flightPlanData == null)
                {
                    return false;
                }
                
                string flightPlanId = flightPlanData["params"]["request_id"].InnerText;
                
                // If this is a new flight plan, raise the event
                if (_lastFlightPlanId != flightPlanId)
                {
                    _currentFlightPlanData = flightPlanData;
                    _lastFlightPlanId = flightPlanId;
                    
                    string flightNumber = flightPlanData["general"]["icao_airline"].InnerText + 
                                         flightPlanData["general"]["flight_number"].InnerText;
                    string origin = flightPlanData["origin"]["icao_code"].InnerText;
                    string destination = flightPlanData["destination"]["icao_code"].InnerText;
                    
                    Logger.Log(LogLevel.Information, "FlightPlanService:LoadFlightPlanAsync", 
                        $"New OFP for Flight {flightNumber} loaded. ({origin} -> {destination})");
                    
                    OnFlightPlanLoaded(new FlightPlanEventArgs(flightPlanId, flightNumber, origin, destination));
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "FlightPlanService:LoadFlightPlanAsync", 
                    $"Error loading flight plan: {ex.Message}");
                return false;
            }
        }
        
        public async Task<XmlNode> GetFlightPlanDataAsync()
        {
            // Currently we only support online flight plans
            return await FetchOnlineFlightPlanAsync();
        }
        
        public async Task<XmlNode> FetchOnlineFlightPlanAsync()
        {
            if (_model.SimBriefID == "0")
            {
                Logger.Log(LogLevel.Error, "FlightPlanService:FetchOnlineFlightPlanAsync", 
                    $"SimBrief ID is not set!");
                return null;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(
                    string.Format(_model.SimBriefURL, _model.SimBriefID));

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        Logger.Log(LogLevel.Debug, "FlightPlanService:FetchOnlineFlightPlanAsync", 
                            $"HTTP Request succeeded!");
                        
                        XmlDocument xmlDoc = new();
                        
                        // Configure XML reader settings for security
                        XmlReaderSettings settings = new XmlReaderSettings
                        {
                            DtdProcessing = DtdProcessing.Prohibit,
                            XmlResolver = null
                        };
                        
                        using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(responseBody), settings))
                        {
                            xmlDoc.Load(reader);
                        }
                        
                        return xmlDoc.ChildNodes[1];
                    }
                    else
                    {
                        Logger.Log(LogLevel.Error, "FlightPlanService:FetchOnlineFlightPlanAsync", 
                            $"SimBrief Response Body is empty!");
                    }
                }
                else
                {
                    Logger.Log(LogLevel.Error, "FlightPlanService:FetchOnlineFlightPlanAsync", 
                        $"HTTP Request failed! Response Code: {response.StatusCode} Message: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "FlightPlanService:FetchOnlineFlightPlanAsync", 
                    $"Exception while fetching flight plan: {ex.Message}");
            }

            return null;
        }
        
        protected virtual void OnFlightPlanLoaded(FlightPlanEventArgs e)
        {
            FlightPlanLoaded?.Invoke(this, e);
        }
    }
}
