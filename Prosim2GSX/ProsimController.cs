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

namespace Prosim2GSX
{
    public class ProsimController
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

        private bool[] paxPlanned;
        private int[] paxSeats;
        private bool[] paxCurrent;
        private int paxLast;
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
                double engine1 = Interface.ReadDataRef("aircraft.engine1.raw");
                double engine2 = Interface.ReadDataRef("aircraft.engine2.raw");
                enginesRunning = engine1 > 18.0D || engine2 > 18.0D;

                fuelCurrent = Interface.ReadDataRef("aircraft.fuel.total.amount.kg");

                useZeroFuel = Model.SetZeroFuel;

                fuelUnits = Interface.ReadDataRef("system.config.Units.Weight");
                if (fuelUnits == "LBS")
                    fuelPlanned /= weightConversion;

                if (Model.FlightPlanType == "MCDU")
                {
                    cargoPlanned = FlightPlan.CargoTotal;
                    fuelPlanned = FlightPlan.Fuel;

                    if (!randomizePaxSeat)
                    {
                        paxPlanned = RandomizePaxSeating(FlightPlan.Passenger);
                        Logger.Log(LogLevel.Information, "ProsimController:Update", $"seatOccupation bool: {string.Join(", ", paxPlanned)}");
                        Interface.SetProsimVariable("aircraft.passengers.seatOccupation", paxPlanned); // string.Join(", ", paxPlanned));
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
                    fuelPlanned = Interface.ReadDataRef("aircraft.refuel.fuelTarget");

                    string str = (string)Interface.ReadDataRef("efb.loading");
                    if (!string.IsNullOrWhiteSpace(str))
                        int.TryParse(str[1..], out cargoPlanned);

                    paxPlanned = Interface.ReadDataRef("efb.passengers.booked");
                    if (forceCurrent)
                        paxCurrent = paxPlanned;


                    JObject result = JObject.Parse((string)Interface.ReadDataRef("efb.flightTimestampJSON"));
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
            model.SimBriefID = (string)Interface.ReadDataRef("efb.simbrief.id");
        }
        public bool IsProsimConnectionAvailable(ServiceModel model)
        {
            Thread.Sleep(250);
            Interface.ConnectProsimSDK();
            Thread.Sleep(5000);

            bool isProsimReady = Interface.IsProsimReady();
            Logger.Log(LogLevel.Debug, "ProsimController:IsProsimConnectionAvailable", $"Prosim Available: {isProsimReady}");

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
            SetSimBriefID(model);
            FlightPlan = new(Model);

            if (!FlightPlan.Load())
            {
                Logger.Log(LogLevel.Error, "ProsimController:IsProsimConnectionAvailable", "Could not load Flightplan");
                Thread.Sleep(5000);
            }
            return true;
        }

        public bool IsFlightplanLoaded()
        {
            if (Model.FlightPlanType == "MCDU")
            {
                return !string.IsNullOrEmpty((string)Interface.ReadDataRef("aircraft.fms.destination"));
            }
            else
            {
                return !string.IsNullOrEmpty((string)Interface.ReadDataRef("efb.prelimloadsheet"));
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

        public double GetFuelPlanned()
        {
            return fuelPlanned;
        }

        public double GetFuelCurrent()
        {
            return fuelCurrent;
        }

        public double GetZfwCG()
        {
            var macZfwCG = 00.0d;
            var act1TankFuelCurrent = Interface.ReadDataRef("aircraft.fuel.ACT1.amount.kg");
            var act2TankFuelCurrent = Interface.ReadDataRef("aircraft.fuel.ACT2.amount.kg");
            var centerTankFuelCurrent = Interface.ReadDataRef("aircraft.fuel.center.amount.kg");
            var leftTankFuelCurrent = Interface.ReadDataRef("aircraft.fuel.left.amount.kg");
            var rightTankFuelCurrent = Interface.ReadDataRef("aircraft.fuel.right.amount.kg");
            Interface.SetProsimVariable("aircraft.fuel.ACT1.amount.kg", 0);
            Interface.SetProsimVariable("aircraft.fuel.ACT2.amount.kg", 0);
            Interface.SetProsimVariable("aircraft.fuel.center.amount.kg", 0);
            Interface.SetProsimVariable("aircraft.fuel.left.amount.kg", 0);
            Interface.SetProsimVariable("aircraft.fuel.right.amount.kg", 0);
            macZfwCG = Interface.ReadDataRef("aircraft.cg");
            Interface.SetProsimVariable("aircraft.fuel.ACT1.amount.kg", act1TankFuelCurrent);
            Interface.SetProsimVariable("aircraft.fuel.ACT2.amount.kg", act2TankFuelCurrent);
            Interface.SetProsimVariable("aircraft.fuel.center.amount.kg", centerTankFuelCurrent);
            Interface.SetProsimVariable("aircraft.fuel.left.amount.kg", leftTankFuelCurrent);
            Interface.SetProsimVariable("aircraft.fuel.right.amount.kg", rightTankFuelCurrent);
            return macZfwCG;
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
            else if (fuelCurrent > fuelPlanned)
            {
                Logger.Log(LogLevel.Information, "ProsimController:SetInitialFuel", $"Current Fuel higher than planned - Resetting to 1500kg (3307lbs)");
                Interface.SetProsimVariable("aircraft.fuel.total.amount.kg", 1500.0D);
                fuelCurrent = 1500D;
            }

        }
        public void RefuelStart()
        {
            Interface.SetProsimVariable("aircraft.refuel.refuelingRate", 0.0D);
            Interface.SetProsimVariable("aircraft.refuel.refuelingPower", true);

            if (fuelUnits == "KG")
                Interface.SetProsimVariable("aircraft.refuel.fuelTarget", fuelPlanned);
            else
                Interface.SetProsimVariable("aircraft.refuel.fuelTarget", fuelPlanned * weightConversion);

        }

        public bool Refuel()
        {
            float step = Model.GetFuelRateKGS();

            if (fuelCurrent + step < fuelPlanned)
                fuelCurrent += step;
            else
                fuelCurrent = fuelPlanned;

            Interface.SetProsimVariable("aircraft.fuel.total.amount.kg", fuelCurrent);
            //Interface.SetProsimVariable("aircraft.fuel.total.amount", fuelCurrent);

            return fuelCurrent == fuelPlanned;
        }

        public void RefuelStop()
        {
            Logger.Log(LogLevel.Information, "ProsimController:RefuelStop", $"RefuelStop Requested");

            Interface.SetProsimVariable("aircraft.refuel.refuelingPower", false);

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
    }
}
