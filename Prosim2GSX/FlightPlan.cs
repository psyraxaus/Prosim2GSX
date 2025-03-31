using Prosim2GSX.Behaviours;
using Prosim2GSX.Models;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace Prosim2GSX
{
    public class FlightPlan
    {
        public int Bags { get; set; }
        public int CargoTotal { get; set; }
        public string Destination { get; set; }
        public string DateOfFlight { get; set; }
        public string DayOfFlight { get; set; }
        public double EstimatedLandingWeight { get; set; }
        public double EstimatedTakeOffWeight { get; set; }
        public double EstimatedZeroFuelWeight { get; set; }
        public string Flight { get; set; } = "";
        public string FlightPlanID { get; set; } = "";
        public double Fuel { get; set; }
        public double FuelLanding { get; set; }
        public double MaxmimumLandingWeight { get; set; }
        public double MaximumTakeOffWeight { get; set; }
        public double MaximumZeroFuelWeight { get; set; }
        public string Origin { get; set; }
        public int Passenger { get; set; }
        public int ScheduledDepartureTime { get; set; }
        public string TailNumber { get; set; }
        public string Units { get; set; }
        public double WeightBag { get; set; }
        public double WeightPax { get; set; }

        private ServiceModel Model { get; set; }

        public FlightPlan(ServiceModel model)
        {
            Model = model;
        }

        protected async Task<string> GetHttpContent(HttpResponseMessage response)
        {
            return await response.Content.ReadAsStringAsync();
        }

        protected XmlNode FetchOnline()
        {
            if (Model.SimBriefID == "0")
            {
                Logger.Log(LogLevel.Error, "FlightPlan:FetchOnline", $"SimBrief ID is not set!");
                return null;
            }

            HttpClient httpClient = new();
            HttpResponseMessage response = httpClient.GetAsync(string.Format(Model.SimBriefURL, Model.SimBriefID)).Result;

            if (response.IsSuccessStatusCode)
            {
                string responseBody = GetHttpContent(response).Result;
                if (responseBody != null && responseBody.Length > 0)
                {
                    Logger.Log(LogLevel.Debug, "FlightPlan:FetchOnline", $"HTTP Request succeded!");
                    XmlDocument xmlDoc = new();
                    xmlDoc.LoadXml(responseBody);
                    return xmlDoc.ChildNodes[1];
                }
                else
                {
                    Logger.Log(LogLevel.Error, "FlightPlan:FetchOnline", $"SimBrief Response Body is empty!");
                }
            }
            else
            {
                Logger.Log(LogLevel.Error, "FlightPlan:FetchOnline", $"HTTP Request failed! Response Code: {response.StatusCode} Message: {response.ReasonPhrase}");
            }

            return null;
        }

        protected XmlNode LoadOFP()
        {
            return FetchOnline();
        }

        public bool Load()
        {
            XmlNode sbOFP = LoadOFP();
            if (sbOFP != null)
            {
                string lastID = FlightPlanID;
                CargoTotal = Convert.ToInt32(sbOFP["weights"]["cargo"].InnerText);
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(sbOFP["api_params"]["date"].InnerText));
                DateTime dateTime = dateTimeOffset.DateTime;
                DateOfFlight = dateTime.ToString("ddMMMyy").ToUpper();
                DateTime dayOfFlight = DateTime.Parse(DateOfFlight);
                DayOfFlight = dayOfFlight.Day.ToString();
                Destination = sbOFP["destination"]["icao_code"].InnerText;
                EstimatedLandingWeight = Convert.ToDouble(sbOFP["weights"]["est_ldw"].InnerText, new RealInvariantFormat(sbOFP["weights"]["est_ldw"].InnerText));
                EstimatedTakeOffWeight = Convert.ToDouble(sbOFP["weights"]["est_tow"].InnerText, new RealInvariantFormat(sbOFP["weights"]["est_ldw"].InnerText));
                EstimatedZeroFuelWeight = Convert.ToDouble(sbOFP["weights"]["est_zfw"].InnerText, new RealInvariantFormat(sbOFP["weights"]["est_ldw"].InnerText));
                Flight = sbOFP["general"]["icao_airline"].InnerText + sbOFP["general"]["flight_number"].InnerText;
                FlightPlanID = sbOFP["params"]["request_id"].InnerText;
                Fuel = Convert.ToDouble(sbOFP["fuel"]["plan_ramp"].InnerText, new RealInvariantFormat(sbOFP["fuel"]["plan_ramp"].InnerText));
                FuelLanding = Convert.ToDouble(sbOFP["fuel"]["plan_landing"].InnerText, new RealInvariantFormat(sbOFP["fuel"]["plan_ramp"].InnerText));
                MaxmimumLandingWeight = Convert.ToInt32(sbOFP["weights"]["max_ldw"].InnerText, new RealInvariantFormat(sbOFP["weights"]["max_ldw"].InnerText));
                MaximumTakeOffWeight = Convert.ToInt32(sbOFP["weights"]["max_tow"].InnerText, new RealInvariantFormat(sbOFP["weights"]["max_tow"].InnerText));
                MaximumZeroFuelWeight = Convert.ToInt32(sbOFP["weights"]["max_zfw"].InnerText, new RealInvariantFormat(sbOFP["weights"]["max_zfw"].InnerText));
                Origin = sbOFP["origin"]["icao_code"].InnerText;
                TailNumber = sbOFP["aircraft"]["reg"].InnerText;
                Units = sbOFP["params"]["units"].InnerText;
                WeightPax = Convert.ToDouble(sbOFP["weights"]["pax_weight"].InnerText, new RealInvariantFormat(sbOFP["weights"]["pax_weight"].InnerText));
                WeightBag = Convert.ToDouble(sbOFP["weights"]["bag_weight"].InnerText, new RealInvariantFormat(sbOFP["weights"]["bag_weight"].InnerText));

                if (Model.UseActualPaxValue)
                {
                    Passenger = Convert.ToInt32(sbOFP["weights"]["pax_count_actual"].InnerText);
                    Bags = Convert.ToInt32(sbOFP["weights"]["bag_count_actual"].InnerText);
                }
                else
                {
                    Passenger = Convert.ToInt32(sbOFP["weights"]["pax_count"].InnerText);
                    Bags = Convert.ToInt32(sbOFP["weights"]["bag_count"].InnerText);
                }
                ScheduledDepartureTime = Convert.ToInt32(sbOFP["api_params"]["dephour"].InnerText) + Convert.ToInt32(sbOFP["api_params"]["depmin"].InnerText);

                if (lastID != FlightPlanID)
                {
                    Logger.Log(LogLevel.Information, "FlightPlan:Load", $"New OFP for Flight {Flight} loaded. ({Origin} -> {Destination})");
                }

                return lastID != FlightPlanID;
            }
            else
                return false;
        }
    }
}
