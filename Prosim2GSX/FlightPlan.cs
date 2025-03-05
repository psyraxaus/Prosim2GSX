using Prosim2GSX.Behaviours;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using System;
using System.Globalization;
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

        private readonly ServiceModel _model;
        private readonly IFlightPlanService _flightPlanService;

        public FlightPlan(ServiceModel model, IFlightPlanService flightPlanService)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _flightPlanService = flightPlanService ?? throw new ArgumentNullException(nameof(flightPlanService));
        }

        public bool Load()
        {
            try
            {
                // Use the service to load the flight plan
                bool result = _flightPlanService.LoadFlightPlanAsync().Result;
                
                if (result)
                {
                    // Parse the flight plan data into this object's properties
                    XmlNode flightPlanData = _flightPlanService.GetFlightPlanDataAsync().Result;
                    if (flightPlanData != null)
                    {
                        ParseFlightPlanData(flightPlanData);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "FlightPlan:Load", $"Error loading flight plan: {ex.Message}");
                return false;
            }
        }
        
        private void ParseFlightPlanData(XmlNode sbOFP)
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
            MaxmimumLandingWeight = Convert.ToInt32(sbOFP["weights"]["max_ldw"].InnerText, new RealInvariantFormat(sbOFP["weights"]["max_ldw"].InnerText));
            MaximumTakeOffWeight = Convert.ToInt32(sbOFP["weights"]["max_tow"].InnerText, new RealInvariantFormat(sbOFP["weights"]["max_tow"].InnerText));
            MaximumZeroFuelWeight = Convert.ToInt32(sbOFP["weights"]["max_zfw"].InnerText, new RealInvariantFormat(sbOFP["weights"]["max_zfw"].InnerText));
            Origin = sbOFP["origin"]["icao_code"].InnerText;
            TailNumber = sbOFP["aircraft"]["reg"].InnerText;
            Units = sbOFP["params"]["units"].InnerText;
            WeightPax = Convert.ToDouble(sbOFP["weights"]["pax_weight"].InnerText, new RealInvariantFormat(sbOFP["weights"]["pax_weight"].InnerText));
            WeightBag = Convert.ToDouble(sbOFP["weights"]["bag_weight"].InnerText, new RealInvariantFormat(sbOFP["weights"]["bag_weight"].InnerText));

            if (_model.UseActualPaxValue)
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
        }
        /*
        public void SetPassengersGSX()
        {
            FSUIPCConnection.WriteLVar("FSDT_GSX_NUMPASSENGERS", Passenger);
            if (Model.NoCrewBoarding)
            {
                FSUIPCConnection.WriteLVar("FSDT_GSX_CREW_NOT_DEBOARDING", 1);
                FSUIPCConnection.WriteLVar("FSDT_GSX_CREW_NOT_BOARDING", 1);
                FSUIPCConnection.WriteLVar("FSDT_GSX_PILOTS_NOT_DEBOARDING", 1);
                FSUIPCConnection.WriteLVar("FSDT_GSX_PILOTS_NOT_BOARDING", 1);
                FSUIPCConnection.WriteLVar("FSDT_GSX_NUMCREW", 9);
                FSUIPCConnection.WriteLVar("FSDT_GSX_NUMPILOTS", 3);
                Logger.Log(LogLevel.Information, "FlightPlan:SetPassengersGSX", $"GSX Passengers set to {Passenger} (Crew Boarding disabled)");
            }
            else
                Logger.Log(LogLevel.Information, "FlightPlan:SetPassengersGSX", $"GSX Passengers set to {Passenger}");
        }
        */
    }
}
