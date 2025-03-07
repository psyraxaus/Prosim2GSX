﻿﻿﻿﻿﻿using Newtonsoft.Json.Linq;
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
using Prosim2GSX.Services;

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
        private IProsimDoorService _doorService;
        private IProsimEquipmentService _equipmentService;
        private IProsimPassengerService _passengerService;
        private IProsimCargoService _cargoService;
        private IProsimFuelService _fuelService;
        private IProsimFluidService _fluidService;
        private IFlightPlanService _flightPlanService;
        private IProsimFlightDataService _flightDataService;

        public static readonly int waitDuration = 30000;

        private bool[] paxPlanned;
        private int[] paxSeats;
        private bool[] paxCurrent;
        private int paxLast;
        public int paxZone1;
        public int paxZone2;
        public int paxZone3;
        public int paxZone4;
        private bool randomizePaxSeat = false;

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
            
            // Services will be initialized after connection is established
        }
        
        /// <summary>
        /// Initializes all ProSim services after connection is established
        /// </summary>
        private void InitializeServices()
        {
            Logger.Log(LogLevel.Information, "ProsimController:InitializeServices", "Initializing ProSim services");
            
            // Initialize door service with the ProsimService from Interface
            _doorService = new ProsimDoorService(Interface.ProsimService);
            
            // Optionally subscribe to door state change events
            _doorService.DoorStateChanged += (sender, args) => {
                // Handle door state changes if needed
                Logger.Log(LogLevel.Debug, "ProsimController:DoorStateChanged", 
                    $"{args.DoorName} is now {(args.IsOpen ? "open" : "closed")}");
            };
            
            // Initialize equipment service with the ProsimService from Interface
            _equipmentService = new ProsimEquipmentService(Interface.ProsimService);
            
            // Optionally subscribe to equipment state change events
            _equipmentService.EquipmentStateChanged += (sender, args) => {
                // Handle equipment state changes if needed
                Logger.Log(LogLevel.Debug, "ProsimController:EquipmentStateChanged", 
                    $"{args.EquipmentName} is now {(args.IsEnabled ? "enabled" : "disabled")}");
            };
            
            // Initialize passenger service with the ProsimService from Interface
            _passengerService = new ProsimPassengerService(Interface.ProsimService);
            
            // Optionally subscribe to passenger state change events
            _passengerService.PassengerStateChanged += (sender, args) => {
                // Handle passenger state changes if needed
                Logger.Log(LogLevel.Debug, "ProsimController:PassengerStateChanged", 
                    $"{args.OperationType}: Current: {args.CurrentCount}, Planned: {args.PlannedCount}");
            };
            
            // Initialize cargo service with the ProsimService from Interface
            _cargoService = new ProsimCargoService(Interface.ProsimService);
            
            // Optionally subscribe to cargo state change events
            _cargoService.CargoStateChanged += (sender, args) => {
                // Handle cargo state changes if needed
                Logger.Log(LogLevel.Debug, "ProsimController:CargoStateChanged", 
                    $"{args.OperationType}: Current: {args.CurrentPercentage}%, Planned: {args.PlannedAmount}");
            };
            
            // Initialize fuel service with the ProsimService from Interface and the model
            _fuelService = new ProsimFuelService(Interface.ProsimService, Model);
            
            // Optionally subscribe to fuel state change events
            _fuelService.FuelStateChanged += (sender, args) => {
                // Handle fuel state changes if needed
                Logger.Log(LogLevel.Debug, "ProsimController:FuelStateChanged", 
                    $"{args.OperationType}: Current: {args.CurrentAmount} {args.FuelUnits}, Planned: {args.PlannedAmount} {args.FuelUnits}");
            };
            
            // Initialize fluid service with the ProsimService from Interface and the model
            _fluidService = new ProsimFluidService(Interface.ProsimService, Model);
            
            // Optionally subscribe to fluid state change events
            _fluidService.FluidStateChanged += (sender, args) => {
                // Handle fluid state changes if needed
                Logger.Log(LogLevel.Debug, "ProsimController:FluidStateChanged", 
                    $"{args.OperationType}: Blue: {args.BlueAmount}, Green: {args.GreenAmount}, Yellow: {args.YellowAmount}");
            };
            
            // Initialize flight plan service
            _flightPlanService = new FlightPlanService(Model);
            
            Logger.Log(LogLevel.Information, "ProsimController:InitializeServices", "ProSim services initialized successfully");
        }

        public void Update(bool forceCurrent)
        {
            try
            {
                double engine1 = Interface.ReadDataRef("aircraft.engine1.raw");
                double engine2 = Interface.ReadDataRef("aircraft.engine2.raw");
                enginesRunning = engine1 > 18.0D || engine2 > 18.0D;

                useZeroFuel = Model.SetZeroFuel;

                if (Model.FlightPlanType == "MCDU")
                {
                    // Update fuel data from flight plan
                    _fuelService.UpdateFromFlightPlan(FlightPlan.Fuel, forceCurrent);

                    if (!_passengerService.HasRandomizedSeating())
                    {
                        // Update passenger data from flight plan
                        _passengerService.UpdateFromFlightPlan(FlightPlan.Passenger, forceCurrent);
                        
                        // Update local variables for zone counts
                        paxZone1 = _passengerService.PaxZone1;
                        paxZone2 = _passengerService.PaxZone2;
                        paxZone3 = _passengerService.PaxZone3;
                        paxZone4 = _passengerService.PaxZone4;
                        
                        // Update local paxPlanned variable for backward compatibility
                        paxPlanned = _passengerService.RandomizePaxSeating(FlightPlan.Passenger);

                        _cargoService.UpdateFromFlightPlan(FlightPlan.CargoTotal, forceCurrent);

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
                    // Update fuel data from EFB
                    _fuelService.UpdateFromFlightPlan(Interface.ReadDataRef("aircraft.refuel.fuelTarget"), forceCurrent);

                    string str = (string)Interface.ReadDataRef("efb.loading");
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        int cargoAmount;
                        if (int.TryParse(str[1..], out cargoAmount))
                        {
                            _cargoService.UpdateFromFlightPlan(cargoAmount, forceCurrent);
                        }
                    }

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
            
            // Initialize services after connection is established
            InitializeServices();
            
            SetSimBriefID(model);
            
            // We'll let the ServiceController create and initialize the FlightPlan
            // This is to ensure proper initialization order
            
            return true;
        }
        
        /// <summary>
        /// Initializes the FlightPlan and FlightDataService
        /// This should be called by ServiceController after FlightPlan is created
        /// </summary>
        public void InitializeFlightPlan(FlightPlan flightPlan)
        {
            FlightPlan = flightPlan;
            
            // Initialize flight data service with the ProsimService from Interface and the FlightPlan
            _flightDataService = new ProsimFlightDataService(Interface.ProsimService, FlightPlan);
            
            // Optionally subscribe to flight data change events
            _flightDataService.FlightDataChanged += (sender, args) => {
                // Handle flight data changes if needed
                Logger.Log(LogLevel.Debug, "ProsimController:FlightDataChanged", 
                    $"{args.DataType} changed to {args.CurrentValue}");
            };
            
            Logger.Log(LogLevel.Information, "ProsimController:InitializeFlightPlan", "FlightPlan and FlightDataService initialized");
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
            return _passengerService.GetPaxPlanned();
        }

        public int GetPaxCurrent()
        {
            return _passengerService.GetPaxCurrent();
        }

        public double GetFuelPlanned()
        {
            return _fuelService.GetFuelPlanned();
        }

        public double GetFuelCurrent()
        {
            return _fuelService.GetFuelCurrent();
        }

        /// <summary>
        /// Gets the Zero Fuel Weight Center of Gravity (MACZFW)
        /// This is the aircraft center of gravity with passengers and cargo but without fuel
        /// </summary>
        /// <returns>The MACZFW value as a percentage</returns>
        public double GetZfwCG()
        {
            return _flightDataService.GetZfwCG();
        }
        
        /// <summary>
        /// Gets the flight data service
        /// </summary>
        /// <returns>The flight data service</returns>
        public IProsimFlightDataService GetFlightDataService()
        {
            return _flightDataService;
        }
        
        /// <summary>
        /// Gets the door service
        /// </summary>
        /// <returns>The door service</returns>
        public IProsimDoorService GetDoorService()
        {
            return _doorService;
        }
        
        /// <summary>
        /// Gets the Take Off Weight Center of Gravity (MACTOW)
        /// This is the aircraft center of gravity with fuel, passengers, and cargo
        /// </summary>
        /// <returns>The MACTOW value as a percentage</returns>
        public double GetTowCG()
        {
            return _flightDataService.GetTowCG();
        }

        public (string, string, string, string, string, string, string, double, double, double, double, double, double, int, int, double, double, int, int, int, double) GetLoadedData(string loadsheetType)
        {
            return _flightDataService.GetLoadedData(loadsheetType);
        }

        public string GetFMSFlightNumber()
        {
            return _flightDataService.GetFMSFlightNumber();
        }

        public double GetFuelAmount()
        {
            return _fuelService.GetFuelAmount();
        }

        public void SetServicePCA(bool enable)
        {
            _equipmentService.SetServicePCA(enable);
        }

        public void SetServiceChocks(bool enable)
        {
            _equipmentService.SetServiceChocks(enable);
        }

        public void SetServiceGPU(bool enable)
        {
            _equipmentService.SetServiceGPU(enable);
        }

        public void TriggerFinal()
        {
            if (Model.FlightPlanType == "MCDU")
            {
                // No implementation for MCDU
            }
            else
            {
                //Interface.TriggerFinalOnEFB();
                //Interface.ProsimPost(ProsimInterface.MsgMutation("bool", "doors.entry.left.fwd", false));
            }
        }

        public void SetInitialFuel()
        {
            _fuelService.SetInitialFuel();
        }

        public void SetInitialFluids()
        {
            _fluidService.SetInitialFluids();
        }

        public (double, double, double) GetHydraulicFluidValues()
        {
            return _fluidService.GetHydraulicFluidValues();
        }
        
        public void RefuelStart()
        {
            _fuelService.RefuelStart();
        }

        public bool Refuel()
        {
            return _fuelService.Refuel();
        }

        public void RefuelStop()
        {
            _fuelService.RefuelStop();
        }

        public bool[] RandomizePaxSeating(int trueCount)
        {
            return _passengerService.RandomizePaxSeating(trueCount);
        }

        public void BoardingStart()
        {
            // Delegate to passenger service
            _passengerService.BoardingStart();
        }

        public bool Boarding(int paxCurrent, int cargoCurrent)
        {
            return _passengerService.Boarding(paxCurrent, cargoCurrent, (cargoValue) => {
                _cargoService.ChangeCargo(cargoValue);
            });
        }

        public void BoardingStop()
        {
            _passengerService.BoardingStop();
        }

        public void DeboardingStart()
        {
            // Delegate to passenger service
            _passengerService.DeboardingStart();
        }

        public bool Deboarding(int paxCurrent, int cargoCurrent)
        {
            return _passengerService.Deboarding(paxCurrent, cargoCurrent, (cargoValue) => {
                _cargoService.ChangeCargo(cargoValue);
            });
        }

        public void DeboardingStop()
        {
            // Reset cargo
            _cargoService.ChangeCargo(0);
            
            // Delegate to passenger service
            _passengerService.DeboardingStop();
        }

        public void SetAftRightDoor(bool open)
        {
            _doorService.SetAftRightDoor(open);
        }
        
        public void SetForwardRightDoor(bool open)
        {
            _doorService.SetForwardRightDoor(open);
        }

        public void SetForwardCargoDoor(bool open)
        {
            _doorService.SetForwardCargoDoor(open);
        }

        public void SetAftCargoDoor(bool open)
        {
            _doorService.SetAftCargoDoor(open);
        }

        public dynamic GetStatusFunction(string dataRef)
        {
            var value = Interface.ReadDataRef(dataRef);
            return value;
        }
    }
}
