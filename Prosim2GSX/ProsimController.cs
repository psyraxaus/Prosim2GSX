using Newtonsoft.Json.Linq;
using ProSimSDK;
using System;
using System.Xml;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.FlightSimulator.SimConnect;
using Prosim2GSX.Models;
using System.Drawing.Text;
using System.Globalization;
using Prosim2GSX.Events;

namespace Prosim2GSX
{
    public partial class ProsimController
    {
        public ProsimInterface Interface;
        protected ServiceModel Model;
        protected FlightPlan FlightPlan;
        private MobiSimConnect SimConnect;
        // Our main ProSim connection
        private readonly ProSimConnect _connection = new ProSimConnect();

        public static readonly int waitDuration = 30000;

        private double fuelCurrent = 0;
        private double fuelPlanned = 0;
        private string fuelUnits = "KG";
        private double fuelTarget;

        private bool[] paxPlanned;
        private int[] paxSeats;
        private bool[] paxCurrent;
        private int paxLast;
        public int paxZone1;
        public int paxZone2;
        public int paxZone3;
        public int paxZone4;
        private bool randomizePaxSeat = false;

        private int cargoPlanned;
        private const float cargoDistMain = 4000.0f / 9440.0f;
        private const float cargoDistBulk = 1440.0f / 9440.0f;
        private int cargoLast;

        public string flightPlanID = "0";
        public string flightNumber = "0";
        public bool enginesRunning = false;
        public static readonly float weightConversion = 2.205f;

        public bool useZeroFuel;

        public ProsimController(ServiceModel model)
        {
            Interface = new(model, _connection);
            paxCurrent = new bool[132];
            paxSeats = null;
            Model = model;
        }

        public void Update(bool forceCurrent)
        {
            try
            {
                double engine1 = Interface.GetProsimVariable("aircraft.engine1.raw");
                double engine2 = Interface.GetProsimVariable("aircraft.engine2.raw");
                enginesRunning = engine1 > 18.0D || engine2 > 18.0D;

                fuelCurrent = Interface.GetProsimVariable("aircraft.fuel.total.amount.kg");

                useZeroFuel = Model.SetZeroFuel;

                fuelUnits = Interface.GetProsimVariable("system.config.Units.Weight");
                if (fuelUnits == "LBS")
                    fuelPlanned /= weightConversion;

                if (Model.FlightPlanType == "MCDU")
                {
                    cargoPlanned = FlightPlan.CargoTotal;
                    fuelPlanned = FlightPlan.Fuel;

                    if (!randomizePaxSeat)
                    {
                        paxPlanned = RandomizePaxSeating(FlightPlan.Passenger);
                        Logger.Log(LogLevel.Debug, "ProsimController:Update", $"seatOccupation bool: {string.Join(", ", paxPlanned)}");
                        Interface.SetProsimVariable("aircraft.passengers.seatOccupation", paxPlanned); // string.Join(", ", paxPlanned));
                        paxZone1 = Interface.GetProsimVariable("aircraft.passengers.zone1.amount");
                        paxZone2 = Interface.GetProsimVariable("aircraft.passengers.zone2.amount");
                        paxZone3 = Interface.GetProsimVariable("aircraft.passengers.zone3.amount");
                        paxZone4 = Interface.GetProsimVariable("aircraft.passengers.zone4.amount");

                        Interface.SetProsimVariable("aircraft.cargo.aft.amount", Convert.ToDouble(FlightPlan.CargoTotal / 2));
                        Interface.SetProsimVariable("aircraft.cargo.forward.amount", Convert.ToDouble(FlightPlan.CargoTotal / 2));
                        Logger.Log(LogLevel.Debug, "ProsimController:Update", $"Temp Cargo set: forward {Interface.GetProsimVariable("aircraft.cargo.forward.amount")} aft {Interface.GetProsimVariable("aircraft.cargo.aft.amount")}");

                        randomizePaxSeat = true;
                    }

                    if (forceCurrent)
                        paxCurrent = paxPlanned;

                    if (flightPlanID != FlightPlan.FlightPlanID)
                    {
                        Logger.Log(LogLevel.Information, "ProsimController:Update", $"New FlightPlan with ID {FlightPlan.FlightPlanID} detected!");
                        flightPlanID = FlightPlan.FlightPlanID;
                        flightNumber = FlightPlan.Flight;
                    }
                }
                else
                {
                    fuelPlanned = Interface.GetProsimVariable("aircraft.refuel.fuelTarget");

                    string str = (string)Interface.GetProsimVariable("efb.loading");
                    if (!string.IsNullOrWhiteSpace(str))
                        int.TryParse(str[1..], out cargoPlanned);

                    paxPlanned = Interface.GetProsimVariable("efb.passengers.booked");
                    if (forceCurrent)
                        paxCurrent = paxPlanned;


                    JObject result = JObject.Parse((string)Interface.GetProsimVariable("efb.flightTimestampJSON"));
                    string innerJson = result.ToString();
                    result = JObject.Parse(innerJson);
                    innerJson = result["ProsimTimes"]["PRELIM_EDNO"].ToString();

                    if (flightPlanID != innerJson)
                    {
                        Logger.Log(LogLevel.Information, "ProsimController:Update", $"New FlightPlan with ID {innerJson} detected!");
                        flightPlanID = innerJson;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimController:Update", $"Exception during Update {ex.Message}");
            }
        }

        public void SetSimBriefID(ServiceModel model)
        {
            model.SimBriefID = (string)Interface.GetProsimVariable("efb.simbrief.id");
        }

        public bool IsProsimConnectionAvailable(ServiceModel model)
        {
            Thread.Sleep(250);
            Interface.ConnectProsimSDK();
            Thread.Sleep(5000);

            bool isProsimReady = Interface.IsProsimReady();
            Logger.Log(LogLevel.Debug, "ProsimController:IsProsimConnectionAvailable", $"Prosim Available: {isProsimReady}");
            EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Prosim", isProsimReady));

            while (Model.IsSimRunning && !isProsimReady && !model.CancellationRequested)
            {
                Logger.Log(LogLevel.Information, "ProsimController:IsProsimConnectionAvailable", $"Is Prosim available? {isProsimReady} - waiting {waitDuration / 1000}s for Retry");
                Interface.ConnectProsimSDK();
                Thread.Sleep(waitDuration);
                isProsimReady = Interface.IsProsimReady();
            }

            if (!isProsimReady || !Model.IsSimRunning)
            {
                Logger.Log(LogLevel.Error, "ProsimController:IsProsimConnectionAvailable", $"Prosim not available - aborting");
                return false;
            }

            return true;
        }

        public bool IsFlightplanLoaded()
        {
            if (Model.FlightPlanType == "MCDU")
            {
                return !string.IsNullOrEmpty((string)Interface.GetProsimVariable("aircraft.fms.destination"));
            }
            else
            {
                return !string.IsNullOrEmpty((string)Interface.GetProsimVariable("efb.prelimloadsheet"));
            }
        }

        public int GetPaxPlanned()
        {
            return paxPlanned.Count(i => i);
        }

        public int GetPaxCurrent()
        {
            return paxCurrent.Count(i => i);
        }

        public string GetFMSFlightNumber()
        {
            string flightNumber;
            var fmsXmlstr = Interface.GetProsimVariable("aircraft.fms.flightPlanXml");
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(fmsXmlstr);
            XmlNode flightNumberNode = xmlDoc.SelectSingleNode("/fms/routeData/flightNumber");

            if (flightNumberNode != null && !string.IsNullOrEmpty(flightNumberNode.InnerText))
            {
                flightNumber = flightNumberNode.InnerText;
                Logger.Log(LogLevel.Debug, "ProsimController:GetFMSFlightNumber", $"Flight Number: {flightNumberNode.InnerText}");
            }
            else
            {
                flightNumber = "";
                Logger.Log(LogLevel.Debug, "ProsimController:GetFMSFlightNumber", $"No Flight number loaded in FMS");
            }
            return flightNumber;
        }

        public double GetFuelAmount()
        {
            double arrivalFuel = Interface.GetProsimVariable("aircraft.fuel.total.amount.kg");
            return arrivalFuel;
        }

        public void SetServicePCA(bool enable)
        {
            Interface.SetProsimVariable("groundservice.preconditionedAir", enable);
        }

        public void SetServiceChocks(bool enable)
        {
            Interface.SetProsimVariable("efb.chocks", enable);
        }

        public void SetServiceGPU(bool enable)
        {
            Interface.SetProsimVariable("groundservice.groundpower", enable);
        }

        public void TriggerFinal()
        {
            if (Model.FlightPlanType == "MCDU")
            {

            }
            else
            {
                //Interface.TriggerFinalOnEFB();
                //Interface.ProsimPost(ProsimInterface.MsgMutation("bool", "doors.entry.left.fwd", false));

            }
        }

        public void SetInitialFuel()
        {
            if (useZeroFuel)
            {
                Logger.Log(LogLevel.Information, "ProsimController:SetInitialFuel", $"Start at Zero Fuel amount - Resetting to 0kg (0lbs)");
                Interface.SetProsimVariable("aircraft.fuel.total.amount.kg", 0.0D);
                fuelCurrent = 0D;
            }
            else if (Model.SetSaveFuel)
            {
                Logger.Log(LogLevel.Information, "ProsimController:SetInitialFuel", $"Using saved fuel value - Resetting to {Model.SavedFuelAmount}");
                Interface.SetProsimVariable("aircraft.fuel.total.amount.kg", Model.SavedFuelAmount);
                fuelCurrent = Model.SavedFuelAmount;
            }
            else if (fuelCurrent > fuelPlanned)
            {
                Logger.Log(LogLevel.Information, "ProsimController:SetInitialFuel", $"Current Fuel higher than planned - Resetting to 1500kg (3307lbs)");
                Interface.SetProsimVariable("aircraft.fuel.total.amount.kg", 1500.0D);
                fuelCurrent = 1500D;
            }

        }

        public void SetInitialFluids()
        {
            Interface.SetProsimVariable("aircraft.hydraulics.blue.quantity", Model.HydaulicsBlueAmount);
            Interface.SetProsimVariable("aircraft.hydraulics.green.quantity", Model.HydaulicsGreenAmount);
            Interface.SetProsimVariable("aircraft.hydraulics.yellow.quantity", Model.HydaulicsYellowAmount);
        }

        public (double, double, double) GetHydraulicFluidValues()
        {
            return (Model.HydaulicsBlueAmount = Interface.GetProsimVariable("aircraft.hydraulics.blue.quantity"), Model.HydaulicsGreenAmount = Interface.GetProsimVariable("aircraft.hydraulics.green.quantity"), Model.HydaulicsYellowAmount = Interface.GetProsimVariable("aircraft.hydraulics.yellow.quantity"));
        }
        public void RefuelStart()
        {
            // Initialize refueling with power off initially
            Interface.SetProsimVariable("aircraft.refuel.refuelingRate", 0.0D);
            Interface.SetProsimVariable("aircraft.refuel.refuelingPower", false);

            // Round up planned fuel to the nearest 100
            fuelTarget = Math.Ceiling(fuelPlanned / 100.0) * 100.0;
            Logger.Log(LogLevel.Debug, "ProsimController:RefuelStart",
                $"Rounding fuel from {fuelPlanned} to {fuelTarget}");

            if (fuelUnits == "KG")
                Interface.SetProsimVariable("aircraft.refuel.fuelTarget", fuelTarget);
            else
                Interface.SetProsimVariable("aircraft.refuel.fuelTarget", fuelTarget * weightConversion);

            Logger.Log(LogLevel.Debug, "ProsimController:RefuelStart",
                $"Fuel target set to {fuelTarget} kg. Current fuel: {fuelCurrent} kg");
        }

        public bool Refuel()
        {
            float step = Model.GetFuelRateKGS();

            Logger.Log(LogLevel.Debug, "ProsimController:Refuel",
                $"Refueling step: Current={fuelCurrent}, Target={fuelTarget}, Step={step}");

            if (fuelCurrent + step < fuelTarget)
            {
                fuelCurrent += step;
                Logger.Log(LogLevel.Debug, "ProsimController:Refuel",
                    $"Refueling in progress: {fuelCurrent}/{fuelTarget} kg");
            }
            else
            {
                fuelCurrent = fuelTarget;
                Logger.Log(LogLevel.Information, "ProsimController:Refuel",
                    $"Refueling complete: {fuelCurrent}/{fuelTarget} kg");
            }

            Interface.SetProsimVariable("aircraft.fuel.total.amount.kg", fuelCurrent);

            return Math.Abs(fuelCurrent - fuelTarget) < 1.0;
        }

        public void RefuelStop()
        {
            Logger.Log(LogLevel.Information, "ProsimController:RefuelStop", $"RefuelStop Requested");
            Interface.SetProsimVariable("aircraft.refuel.refuelingPower", false);
        }

        public void RefuelPause()
        {
            Logger.Log(LogLevel.Information, "ProsimController:RefuelPause", $"Refueling paused - hose disconnected");
            // Turn off power when hose disconnected
            Interface.SetProsimVariable("aircraft.refuel.refuelingPower", false);
            Interface.SetProsimVariable("aircraft.refuel.refuelingRate", 0.0D);
        }

        public void RefuelResume()
        {
            Logger.Log(LogLevel.Information, "ProsimController:RefuelResume", $"Refueling resumed - hose connected");
            // Turn on power when hose connected
            Interface.SetProsimVariable("aircraft.refuel.refuelingPower", true);
            Interface.SetProsimVariable("aircraft.refuel.refuelingRate", Model.GetFuelRateKGS());

        }

        public bool[] RandomizePaxSeating(int trueCount)
        {
            if (trueCount < 0 || trueCount > 132)
            {
                throw new ArgumentException("The number of 'true' values must be between 0 and 132.");
            }
            bool[] result = new bool[132];
            // Initialize all to false
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = false;
            }
            // Fill the array with 'true' values at random positions
            Random rand = new Random();
            int count = 0;
            while (count < trueCount)
            {
                int index = rand.Next(132);
                if (!result[index])
                {
                    result[index] = true;
                    count++;
                }
            }
            return result;
        }

        public void BoardingStart()
        {
            paxLast = 0;
            cargoLast = 0;
            paxSeats = new int[GetPaxPlanned()];
            int n = 0;
            for (int i = 0; i < paxPlanned.Length; i++)
            {
                if (paxPlanned[i])
                {
                    paxSeats[n] = i;
                    n++;
                }
            }
        }

        public bool Boarding(int paxCurrent, int cargoCurrent)
        {
            BoardPassengers(paxCurrent - paxLast);
            paxLast = paxCurrent;

            ChangeCargo(cargoCurrent);
            cargoLast = cargoCurrent;

            return paxCurrent == GetPaxPlanned() && cargoCurrent == 100;
        }

        private void BoardPassengers(int num)
        {
            if (num < 0)
            {
                Logger.Log(LogLevel.Debug, "ProsimController:BoardPassengers", $"Passenger Num was below 0!");
                return;
            }
            else if (num > 15)
            {
                Logger.Log(LogLevel.Debug, "ProsimController:BoardPassengers", $"Passenger Num was above 15!");
                return;
            }
            else
                Logger.Log(LogLevel.Debug, "ProsimController:BoardPassengers", $"(num {num}) (current {GetPaxCurrent()}) (planned ({GetPaxPlanned()}))");

            int n = 0;
            for (int i = paxLast; i < paxLast + num && i < GetPaxPlanned(); i++)
            {
                paxCurrent[paxSeats[i]] = true;
                n++;
            }

            if (n > 0)
                SendSeatString();
        }

        private void SendSeatString(bool force = false)
        {
            string seatString = "";
            bool first = true;

            if (GetPaxCurrent() == 0 && !force)
                return;

            foreach (var pax in paxCurrent)
            {
                if (first)
                {
                    if (pax)
                        seatString = "true";
                    else
                        seatString = "false";
                    first = false;
                }
                else
                {
                    if (pax)
                        seatString += ",true";
                    else
                        seatString += ",false";
                }
            }
            Logger.Log(LogLevel.Debug, "ProsimController:SendSeatString", seatString);
            Interface.SetProsimVariable("aircraft.passengers.seatOccupation.string", seatString);
        }

        private void ChangeCargo(int cargoCurrent)
        {
            if (cargoCurrent == cargoLast)
                return;

            float cargo = (float)cargoPlanned * (float)(cargoCurrent / 100.0f);
            Interface.SetProsimVariable("aircraft.cargo.forward.amount", (float)cargo * cargoDistMain);
            Interface.SetProsimVariable("aircraft.cargo.aft.amount", (float)cargo * cargoDistMain);
        }

        public void BoardingStop()
        {
            paxSeats = null;
            if (Model.FlightPlanType == "EFB")
            {
                Interface.SetProsimVariable("efb.efb.boardingStatus", "ended");
            }

        }

        public void DeboardingStart()
        {
            Logger.Log(LogLevel.Debug, "ProsimController:DeboardingStart", $"(planned {GetPaxPlanned()}) (current {GetPaxCurrent()})");
            paxLast = GetPaxPlanned();
            if (GetPaxCurrent() != GetPaxPlanned())
                paxCurrent = paxPlanned;
            cargoLast = 100;
        }

        private void DeboardPassengers(int num)
        {
            if (num < 0)
            {
                Logger.Log(LogLevel.Debug, "ProsimController:DeboardPassengers", $"Passenger Num was below 0!");
                return;
            }
            else if (num > 15)
            {
                Logger.Log(LogLevel.Debug, "ProsimController:DeboardPassengers", $"Passenger Num was above 15!");
                return;
            }
            else
                Logger.Log(LogLevel.Debug, "ProsimController:DeboardPassengers", $"(num {num}) (current {GetPaxCurrent()}) (planned ({GetPaxPlanned()}))");

            int n = 0;
            for (int i = 0; i < paxCurrent.Length && n < num; i++)
            {
                if (paxCurrent[i])
                {
                    paxCurrent[i] = false;
                    n++;
                }
            }

            if (n > 0)
                SendSeatString();
        }

        public bool Deboarding(int paxCurrent, int cargoCurrent)
        {
            DeboardPassengers(paxLast - paxCurrent);
            paxLast = paxCurrent;

            cargoCurrent = 100 - cargoCurrent;
            ChangeCargo(cargoCurrent);
            cargoLast = cargoCurrent;

            return paxCurrent == 0 && cargoCurrent == 0;
        }

        public void DeboardingStop()
        {
            ChangeCargo(0);
            for (int i = 0; i < paxCurrent.Length; i++)
                paxCurrent[i] = false;
            Logger.Log(LogLevel.Debug, "ProsimController:DeboardingStop", "Sending SeatString");
            SendSeatString(true);
            paxCurrent = new bool[162];
            paxSeats = null;
        }

        public void SetAftRightDoor(bool open)
        {
            Interface.SetProsimVariable("doors.entry.right.aft", open);
            Logger.Log(LogLevel.Information, "ProsimController:SetAftRightDoor", $"Aft right door {(open ? "opened" : "closed")}");
        }

        public string GetAftRightDoor()
        {
            bool doorStatus = Interface.GetProsimVariable("doors.entry.right.aft");
            string status = doorStatus ? "open" : "closed";
            Logger.Log(LogLevel.Debug, "ProsimController:GetForwardRightDoorStatus", $"Forward right door {status}");
            return status;
        }

        public void SetForwardRightDoor(bool open)
        {
            Interface.SetProsimVariable("doors.entry.right.fwd", open);
            Logger.Log(LogLevel.Information, "ProsimController:SetForwardRightDoor", $"Forward right door {(open ? "opened" : "closed")}");
        }

        public string GetForwardRightDoor()
        {
            bool doorStatus = Interface.GetProsimVariable("doors.entry.right.fwd");
            string status = doorStatus ? "open" : "closed";
            Logger.Log(LogLevel.Debug, "ProsimController:GetForwardRightDoorStatus", $"Forward right door {status}");
            return status;
        }

        public void SetForwardCargoDoor(bool open)
        {
            Interface.SetProsimVariable("doors.cargo.forward", open);
            Logger.Log(LogLevel.Information, "ProsimController:SetForwardCargoDoor", $"Forward cargo door {(open ? "opened" : "closed")}");
        }

        public string GetForwardCargoDoor()
        {
            bool doorStatus = Interface.GetProsimVariable("doors.cargo.forward");
            string status = doorStatus ? "open" : "closed";
            return status;
        }

        public void SetAftCargoDoor(bool open)
        {
            Interface.SetProsimVariable("doors.cargo.aft", open);
            Logger.Log(LogLevel.Information, "ProsimController:SetAftCargoDoor", $"Aft cargo door {(open ? "opened" : "closed")}");
        }

        public string GetAftCargoDoor()
        {
            bool doorStatus = Interface.GetProsimVariable("doors.cargo.aft");
            string status = doorStatus ? "open" : "closed";
            return status;
        }

        public int GetStatusFunction(string dataRef)
        {
            var value = Interface.GetProsimVariable(dataRef);

            // Convert boolean values to integers
            if (value is bool boolValue)
            {
                return boolValue ? 1 : 0;
            }

            // Try to convert to integer if it's not already
            if (value is int intValue)
            {
                return intValue;
            }

            // For other numeric types, convert to int
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                // Log the issue and return 0 as a fallback
                Logger.Log(LogLevel.Warning, "ProsimController:GetStatusFunction",
                    $"Could not convert value of type {value?.GetType().Name ?? "null"} to int for dataRef {dataRef}");
                return 0;
            }
        }

        // Add to ProsimController class if not already present
        public (double, double, double) GetMaxWeights()
        {
            // Return max ZFW, max TOW, max LAW
            return (Interface.GetProsimVariable("aircraft.weight.zfwMax"), Interface.GetProsimVariable("aircraft.weight.grossMax"), 66000);  // A320 typical values
        }

        public double GetPlannedTripFuel()
        {
            // Calculate trip fuel from flight plan
            double tripFuel = FlightPlan.Fuel - FlightPlan.FuelLanding;
            return tripFuel > 0 ? tripFuel : 5000; // Default 5000kg if not set
        }

        public void SetFlightPlan(FlightPlan flightPlan)
        {
            this.FlightPlan = flightPlan;
        }
    }
}
