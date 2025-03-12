using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using Prosim2GSX.Models;
using ProSimSDK;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Implementation of the ProSim controller facade that coordinates ProSim integration with GSX
    /// </summary>
    public class ProsimControllerFacade : BaseController, IProsimController
    {
        private readonly ProsimInterface _interface;
        private readonly IProsimService _prosimService;
        private readonly IProsimDoorService _doorService;
        private readonly IProsimEquipmentService _equipmentService;
        private readonly IProsimPassengerService _passengerService;
        private readonly IProsimCargoService _cargoService;
        private readonly IProsimFuelService _fuelService;
        private readonly IProsimFluidService _fluidService;
        private readonly IFlightPlanService _flightPlanService;
        
        private IProsimFlightDataService _flightDataService;
        private FlightPlan _flightPlan;
        
        /// <summary>
        /// Gets a value indicating whether the controller is connected to ProSim
        /// </summary>
        public bool IsConnected => _interface?.IsProsimReady() ?? false;
        
        /// <summary>
        /// Gets a value indicating whether engines are running
        /// </summary>
        public bool EnginesRunning { get; private set; }
        
        /// <summary>
        /// Gets the current flight plan ID
        /// </summary>
        public string FlightPlanID { get; private set; } = "0";
        
        /// <summary>
        /// Gets the current flight number
        /// </summary>
        public string FlightNumber { get; private set; } = "0";
        
        /// <summary>
        /// Event raised when the connection state changes
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        
        /// <summary>
        /// Event raised when a flight plan is loaded
        /// </summary>
        public event EventHandler<FlightPlanLoadedEventArgs> FlightPlanLoaded;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProsimControllerFacade"/> class
        /// </summary>
        /// <param name="model">The service model</param>
        /// <param name="logger">The logger</param>
        /// <param name="eventAggregator">The event aggregator</param>
        /// <param name="prosimService">The ProSim service</param>
        /// <param name="doorService">The door service</param>
        /// <param name="equipmentService">The equipment service</param>
        /// <param name="passengerService">The passenger service</param>
        /// <param name="cargoService">The cargo service</param>
        /// <param name="fuelService">The fuel service</param>
        /// <param name="fluidService">The fluid service</param>
        /// <param name="flightPlanService">The flight plan service</param>
        public ProsimControllerFacade(
            ServiceModel model,
            ILogger logger,
            IEventAggregator eventAggregator,
            IProsimService prosimService,
            IProsimDoorService doorService,
            IProsimEquipmentService equipmentService,
            IProsimPassengerService passengerService,
            IProsimCargoService cargoService,
            IProsimFuelService fuelService,
            IProsimFluidService fluidService,
            IFlightPlanService flightPlanService)
            : base(model, logger, eventAggregator)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _doorService = doorService ?? throw new ArgumentNullException(nameof(doorService));
            _equipmentService = equipmentService ?? throw new ArgumentNullException(nameof(equipmentService));
            _passengerService = passengerService ?? throw new ArgumentNullException(nameof(passengerService));
            _cargoService = cargoService ?? throw new ArgumentNullException(nameof(cargoService));
            _fuelService = fuelService ?? throw new ArgumentNullException(nameof(fuelService));
            _fluidService = fluidService ?? throw new ArgumentNullException(nameof(fluidService));
            _flightPlanService = flightPlanService ?? throw new ArgumentNullException(nameof(flightPlanService));
            
            _interface = new ProsimInterface(model, (ProSimSDK.ProSimConnect)_prosimService.Connection);
            
            // Subscribe to service events
            _doorService.DoorStateChanged += (sender, args) => {
                Logger.Log(LogLevel.Debug, "ProsimControllerFacade:DoorStateChanged", 
                    $"{args.DoorName} is now {(args.IsOpen ? "open" : "closed")}");
            };
            
            _equipmentService.EquipmentStateChanged += (sender, args) => {
                Logger.Log(LogLevel.Debug, "ProsimControllerFacade:EquipmentStateChanged", 
                    $"{args.EquipmentName} is now {(args.IsEnabled ? "enabled" : "disabled")}");
            };
            
            _passengerService.PassengerStateChanged += (sender, args) => {
                Logger.Log(LogLevel.Debug, "ProsimControllerFacade:PassengerStateChanged", 
                    $"{args.OperationType}: Current: {args.CurrentCount}, Planned: {args.PlannedCount}");
            };
            
            _cargoService.CargoStateChanged += (sender, args) => {
                Logger.Log(LogLevel.Debug, "ProsimControllerFacade:CargoStateChanged", 
                    $"{args.OperationType}: Current: {args.CurrentPercentage}%, Planned: {args.PlannedAmount}");
            };
            
            _fuelService.FuelStateChanged += (sender, args) => {
                Logger.Log(LogLevel.Debug, "ProsimControllerFacade:FuelStateChanged", 
                    $"{args.OperationType}: Current: {args.CurrentAmount} {args.FuelUnits}, Planned: {args.PlannedAmount} {args.FuelUnits}");
            };
            
            _fluidService.FluidStateChanged += (sender, args) => {
                Logger.Log(LogLevel.Debug, "ProsimControllerFacade:FluidStateChanged", 
                    $"{args.OperationType}: Blue: {args.BlueAmount}, Green: {args.GreenAmount}, Yellow: {args.YellowAmount}");
            };
            
            Logger.Log(LogLevel.Information, "ProsimControllerFacade:Constructor", "ProSim controller facade initialized");
        }
        
        /// <summary>
        /// Connects to ProSim
        /// </summary>
        /// <param name="model">The service model</param>
        /// <returns>True if connection was successful, false otherwise</returns>
        public bool Connect(ServiceModel model)
        {
            return ExecuteSafely(() => {
                Logger.Log(LogLevel.Information, "ProsimControllerFacade:Connect", "Connecting to ProSim");
                
                Thread.Sleep(250);
                _interface.ConnectProsimSDK();
                Thread.Sleep(5000);
                
                bool isConnected = _interface.IsProsimReady();
                Logger.Log(LogLevel.Debug, "ProsimControllerFacade:Connect", $"ProSim Available: {isConnected}");
                
                while (Model.IsSimRunning && !isConnected && !model.CancellationRequested)
                {
                    Logger.Log(LogLevel.Information, "ProsimControllerFacade:Connect", 
                        $"Is ProSim available? {isConnected} - waiting {ProsimController.waitDuration / 1000}s for Retry");
                    _interface.ConnectProsimSDK();
                    Thread.Sleep(ProsimController.waitDuration);
                    isConnected = _interface.IsProsimReady();
                }
                
                if (isConnected)
                {
                    Logger.Log(LogLevel.Information, "ProsimControllerFacade:Connect", "Connected to ProSim");
                    ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(true));
                    
                    // Set SimBrief ID
                    model.SimBriefID = (string)_interface.ReadDataRef("efb.simbrief.id");
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ProsimControllerFacade:Connect", "Failed to connect to ProSim");
                }
                
                return isConnected;
            }, "Connect");
        }
        
        /// <summary>
        /// Disconnects from ProSim
        /// </summary>
        public void Disconnect()
        {
            ExecuteSafely(() => {
                Logger.Log(LogLevel.Information, "ProsimControllerFacade:Disconnect", "Disconnecting from ProSim");
                
                // No explicit disconnect method in ProsimInterface, but we can notify listeners
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(false));
            }, "Disconnect");
        }
        
        /// <summary>
        /// Updates the controller state
        /// </summary>
        /// <param name="forceCurrent">Whether to force current values to match planned values</param>
        public void Update(bool forceCurrent)
        {
            ExecuteSafely(() => {
                try
                {
                    double engine1 = _interface.ReadDataRef("aircraft.engine1.raw");
                    double engine2 = _interface.ReadDataRef("aircraft.engine2.raw");
                    EnginesRunning = engine1 > 18.0D || engine2 > 18.0D;

                    if (Model.FlightPlanType == "MCDU")
                    {
                        // Update fuel data from flight plan
                        _fuelService.UpdateFromFlightPlan(_flightPlan.Fuel, forceCurrent);

                        if (!_passengerService.HasRandomizedSeating())
                        {
                            // Update passenger data from flight plan
                            _passengerService.UpdateFromFlightPlan(_flightPlan.Passenger, forceCurrent);
                            
                            // Update cargo data from flight plan
                            _cargoService.UpdateFromFlightPlan(_flightPlan.CargoTotal, forceCurrent);
                        }

                        if (FlightPlanID != _flightPlan.FlightPlanID)
                        {
                            Logger.Log(LogLevel.Information, "ProsimControllerFacade:Update", $"New FlightPlan with ID {_flightPlan.FlightPlanID} detected!");
                            FlightPlanID = _flightPlan.FlightPlanID;
                            FlightNumber = _flightPlan.Flight;
                            
                            // Raise event
                            FlightPlanLoaded?.Invoke(this, new FlightPlanLoadedEventArgs(
                                FlightPlanID, 
                                FlightNumber, 
                                _flightPlan.Origin, 
                                _flightPlan.Destination));
                        }
                    }
                    else
                    {
                        // Update fuel data from EFB
                        _fuelService.UpdateFromFlightPlan(_interface.ReadDataRef("aircraft.refuel.fuelTarget"), forceCurrent);

                        string str = (string)_interface.ReadDataRef("efb.loading");
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            if (int.TryParse(str[1..], out int cargoAmount))
                            {
                                _cargoService.UpdateFromFlightPlan(cargoAmount, forceCurrent);
                            }
                        }

                        // Update passenger data from EFB
                        bool[] paxPlanned = _interface.ReadDataRef("efb.passengers.booked");
                        _passengerService.UpdateFromEFB(paxPlanned, forceCurrent);

                        // Check for new flight plan
                        JObject result = JObject.Parse((string)_interface.ReadDataRef("efb.flightTimestampJSON"));
                        string innerJson = result.ToString();
                        result = JObject.Parse(innerJson);
                        innerJson = result["ProsimTimes"]["PRELIM_EDNO"].ToString();

                        if (FlightPlanID != innerJson)
                        {
                            Logger.Log(LogLevel.Information, "ProsimControllerFacade:Update", $"New FlightPlan with ID {innerJson} detected!");
                            FlightPlanID = innerJson;
                            
                            // Get flight number from EFB
                            FlightNumber = GetFMSFlightNumber();
                            
                            // Get departure and destination from EFB
                            string departure = (string)_interface.ReadDataRef("aircraft.fms.origin");
                            string destination = (string)_interface.ReadDataRef("aircraft.fms.destination");
                            
                            // Raise event
                            FlightPlanLoaded?.Invoke(this, new FlightPlanLoadedEventArgs(
                                FlightPlanID, 
                                FlightNumber, 
                                departure, 
                                destination));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "ProsimControllerFacade:Update", $"Exception during Update {ex.Message}");
                }
            }, "Update");
        }
        
        /// <summary>
        /// Initializes the flight plan
        /// </summary>
        /// <param name="flightPlan">The flight plan</param>
        public void InitializeFlightPlan(FlightPlan flightPlan)
        {
            ExecuteSafely(() => {
                Logger.Log(LogLevel.Information, "ProsimControllerFacade:InitializeFlightPlan", "Initializing flight plan");
                
                _flightPlan = flightPlan ?? throw new ArgumentNullException(nameof(flightPlan));
                
                // Initialize flight data service with the ProsimService from Interface and the FlightPlan
                _flightDataService = new ProsimFlightDataService(_prosimService, _flightPlan);
                
                // Subscribe to flight data change events
                _flightDataService.FlightDataChanged += (sender, args) => {
                    Logger.Log(LogLevel.Debug, "ProsimControllerFacade:FlightDataChanged", 
                        $"{args.DataType} changed to {args.CurrentValue}");
                };
                
                Logger.Log(LogLevel.Information, "ProsimControllerFacade:InitializeFlightPlan", "Flight plan and flight data service initialized");
            }, "InitializeFlightPlan");
        }
        
        /// <summary>
        /// Checks if a flight plan is loaded
        /// </summary>
        /// <returns>True if a flight plan is loaded, false otherwise</returns>
        public bool IsFlightplanLoaded()
        {
            return ExecuteSafely(() => {
                if (Model.FlightPlanType == "MCDU")
                {
                    return !string.IsNullOrEmpty((string)_interface.ReadDataRef("aircraft.fms.destination"));
                }
                else
                {
                    return !string.IsNullOrEmpty((string)_interface.ReadDataRef("efb.prelimloadsheet"));
                }
            }, "IsFlightplanLoaded");
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
        /// Gets the equipment service
        /// </summary>
        /// <returns>The equipment service</returns>
        public IProsimEquipmentService GetEquipmentService()
        {
            return _equipmentService;
        }
        
        /// <summary>
        /// Gets the passenger service
        /// </summary>
        /// <returns>The passenger service</returns>
        public IProsimPassengerService GetPassengerService()
        {
            return _passengerService;
        }
        
        /// <summary>
        /// Gets the cargo service
        /// </summary>
        /// <returns>The cargo service</returns>
        public IProsimCargoService GetCargoService()
        {
            return _cargoService;
        }
        
        /// <summary>
        /// Gets the fuel service
        /// </summary>
        /// <returns>The fuel service</returns>
        public IProsimFuelService GetFuelService()
        {
            return _fuelService;
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
        /// Gets the fluid service
        /// </summary>
        /// <returns>The fluid service</returns>
        public IProsimFluidService GetFluidService()
        {
            return _fluidService;
        }
        
        /// <summary>
        /// Gets the planned passenger count
        /// </summary>
        /// <returns>The planned passenger count</returns>
        public int GetPaxPlanned()
        {
            return ExecuteSafely(() => _passengerService.GetPaxPlanned(), "GetPaxPlanned");
        }
        
        /// <summary>
        /// Gets the current passenger count
        /// </summary>
        /// <returns>The current passenger count</returns>
        public int GetPaxCurrent()
        {
            return ExecuteSafely(() => _passengerService.GetPaxCurrent(), "GetPaxCurrent");
        }
        
        /// <summary>
        /// Gets the planned fuel amount
        /// </summary>
        /// <returns>The planned fuel amount</returns>
        public double GetFuelPlanned()
        {
            return ExecuteSafely(() => _fuelService.GetFuelPlanned(), "GetFuelPlanned");
        }
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        /// <returns>The current fuel amount</returns>
        public double GetFuelCurrent()
        {
            return ExecuteSafely(() => _fuelService.GetFuelCurrent(), "GetFuelCurrent");
        }
        
        /// <summary>
        /// Gets the Zero Fuel Weight Center of Gravity (MACZFW)
        /// </summary>
        /// <returns>The MACZFW value as a percentage</returns>
        public double GetZfwCG()
        {
            return ExecuteSafely(() => _flightDataService.GetZfwCG(), "GetZfwCG");
        }
        
        /// <summary>
        /// Gets the Take Off Weight Center of Gravity (MACTOW)
        /// </summary>
        /// <returns>The MACTOW value as a percentage</returns>
        public double GetTowCG()
        {
            return ExecuteSafely(() => _flightDataService.GetTowCG(), "GetTowCG");
        }
        
        /// <summary>
        /// Gets loaded data for a loadsheet
        /// </summary>
        /// <param name="loadsheetType">The loadsheet type</param>
        /// <returns>A tuple containing the loaded data</returns>
        public (string, string, string, string, string, string, string, double, double, double, double, double, double, int, int, double, double, int, int, int, double) GetLoadedData(string loadsheetType)
        {
            return ExecuteSafely(() => _flightDataService.GetLoadedData(loadsheetType), "GetLoadedData");
        }
        
        /// <summary>
        /// Gets the FMS flight number
        /// </summary>
        /// <returns>The FMS flight number</returns>
        public string GetFMSFlightNumber()
        {
            return ExecuteSafely(() => _flightDataService.GetFMSFlightNumber(), "GetFMSFlightNumber");
        }
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        /// <returns>The current fuel amount</returns>
        public double GetFuelAmount()
        {
            return ExecuteSafely(() => _fuelService.GetFuelAmount(), "GetFuelAmount");
        }
        
        /// <summary>
        /// Sets the PCA service state
        /// </summary>
        /// <param name="enable">Whether to enable or disable the service</param>
        public void SetServicePCA(bool enable)
        {
            ExecuteSafely(() => _equipmentService.SetServicePCA(enable), "SetServicePCA");
        }
        
        /// <summary>
        /// Sets the chocks service state
        /// </summary>
        /// <param name="enable">Whether to enable or disable the service</param>
        public void SetServiceChocks(bool enable)
        {
            ExecuteSafely(() => _equipmentService.SetServiceChocks(enable), "SetServiceChocks");
        }
        
        /// <summary>
        /// Sets the GPU service state
        /// </summary>
        /// <param name="enable">Whether to enable or disable the service</param>
        public void SetServiceGPU(bool enable)
        {
            ExecuteSafely(() => _equipmentService.SetServiceGPU(enable), "SetServiceGPU");
        }
        
        /// <summary>
        /// Triggers the final loadsheet
        /// </summary>
        public void TriggerFinal()
        {
            ExecuteSafely(() => {
                if (Model.FlightPlanType == "MCDU")
                {
                    // No implementation for MCDU
                    Logger.Log(LogLevel.Information, "ProsimControllerFacade:TriggerFinal", "TriggerFinal not implemented for MCDU");
                }
                else
                {
                    Logger.Log(LogLevel.Information, "ProsimControllerFacade:TriggerFinal", "Triggering final loadsheet");
                    //Interface.TriggerFinalOnEFB();
                    //Interface.ProsimPost(ProsimInterface.MsgMutation("bool", "doors.entry.left.fwd", false));
                }
            }, "TriggerFinal");
        }
        
        /// <summary>
        /// Sets the initial fuel
        /// </summary>
        public void SetInitialFuel()
        {
            ExecuteSafely(() => _fuelService.SetInitialFuel(), "SetInitialFuel");
        }
        
        /// <summary>
        /// Sets the initial fluids
        /// </summary>
        public void SetInitialFluids()
        {
            ExecuteSafely(() => _fluidService.SetInitialFluids(), "SetInitialFluids");
        }
        
        /// <summary>
        /// Gets the hydraulic fluid values
        /// </summary>
        /// <returns>A tuple containing the hydraulic fluid values</returns>
        public (double, double, double) GetHydraulicFluidValues()
        {
            return ExecuteSafely(() => _fluidService.GetHydraulicFluidValues(), "GetHydraulicFluidValues");
        }
        
        /// <summary>
        /// Starts refueling
        /// </summary>
        public void RefuelStart()
        {
            ExecuteSafely(() => _fuelService.RefuelStart(), "RefuelStart");
        }
        
        /// <summary>
        /// Performs refueling
        /// </summary>
        /// <returns>True if refueling is complete, false otherwise</returns>
        public bool Refuel()
        {
            return ExecuteSafely(() => _fuelService.Refuel(), "Refuel");
        }
        
        /// <summary>
        /// Stops refueling
        /// </summary>
        public void RefuelStop()
        {
            ExecuteSafely(() => _fuelService.RefuelStop(), "RefuelStop");
        }
        
        /// <summary>
        /// Randomizes passenger seating
        /// </summary>
        /// <param name="trueCount">The number of passengers</param>
        /// <returns>An array of boolean values representing passenger seating</returns>
        public bool[] RandomizePaxSeating(int trueCount)
        {
            return ExecuteSafely(() => _passengerService.RandomizePaxSeating(trueCount), "RandomizePaxSeating");
        }
        
        /// <summary>
        /// Starts boarding
        /// </summary>
        public void BoardingStart()
        {
            ExecuteSafely(() => _passengerService.BoardingStart(), "BoardingStart");
        }
        
        /// <summary>
        /// Performs boarding
        /// </summary>
        /// <param name="paxCurrent">The current passenger count</param>
        /// <param name="cargoCurrent">The current cargo amount</param>
        /// <returns>True if boarding is complete, false otherwise</returns>
        public bool Boarding(int paxCurrent, int cargoCurrent)
        {
            return ExecuteSafely(() => _passengerService.Boarding(paxCurrent, cargoCurrent, (cargoValue) => {
                _cargoService.ChangeCargo(cargoValue);
            }), "Boarding");
        }
        
        /// <summary>
        /// Stops boarding
        /// </summary>
        public void BoardingStop()
        {
            ExecuteSafely(() => _passengerService.BoardingStop(), "BoardingStop");
        }
        
        /// <summary>
        /// Starts deboarding
        /// </summary>
        public void DeboardingStart()
        {
            ExecuteSafely(() => _passengerService.DeboardingStart(), "DeboardingStart");
        }
        
        /// <summary>
        /// Performs deboarding
        /// </summary>
        /// <param name="paxCurrent">The current passenger count</param>
        /// <param name="cargoCurrent">The current cargo amount</param>
        /// <returns>True if deboarding is complete, false otherwise</returns>
        public bool Deboarding(int paxCurrent, int cargoCurrent)
        {
            return ExecuteSafely(() => _passengerService.Deboarding(paxCurrent, cargoCurrent, (cargoValue) => {
                _cargoService.ChangeCargo(cargoValue);
            }), "Deboarding");
        }
        
        /// <summary>
        /// Stops deboarding
        /// </summary>
        public void DeboardingStop()
        {
            ExecuteSafely(() => {
                // Reset cargo
                _cargoService.ChangeCargo(0);
                
                // Delegate to passenger service
                _passengerService.DeboardingStop();
            }, "DeboardingStop");
        }
        
        /// <summary>
        /// Sets the aft right door state
        /// </summary>
        /// <param name="open">Whether to open or close the door</param>
        public void SetAftRightDoor(bool open)
        {
            ExecuteSafely(() => _doorService.SetAftRightDoor(open), "SetAftRightDoor");
        }
        
        /// <summary>
        /// Sets the forward right door state
        /// </summary>
        /// <param name="open">Whether to open or close the door</param>
        public void SetForwardRightDoor(bool open)
        {
            ExecuteSafely(() => _doorService.SetForwardRightDoor(open), "SetForwardRightDoor");
        }
        
        /// <summary>
        /// Sets the forward cargo door state
        /// </summary>
        /// <param name="open">Whether to open or close the door</param>
        public void SetForwardCargoDoor(bool open)
        {
            ExecuteSafely(() => _doorService.SetForwardCargoDoor(open), "SetForwardCargoDoor");
        }
        
        /// <summary>
        /// Sets the aft cargo door state
        /// </summary>
        /// <param name="open">Whether to open or close the door</param>
        public void SetAftCargoDoor(bool open)
        {
            ExecuteSafely(() => _doorService.SetAftCargoDoor(open), "SetAftCargoDoor");
        }
        
        /// <summary>
        /// Gets a value from ProSim using a data reference
        /// </summary>
        /// <param name="dataRef">The data reference</param>
        /// <returns>The value</returns>
        public dynamic GetStatusFunction(string dataRef)
        {
            return ExecuteSafely(() => _interface.ReadDataRef(dataRef), "GetStatusFunction");
        }
        
        /// <summary>
        /// Disposes resources used by the controller
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
                return;
                
            try
            {
                // Unsubscribe from events
                
                // Dispose services if they implement IDisposable
                if (_prosimService is IDisposable disposableProsimService)
                    disposableProsimService.Dispose();
                    
                if (_doorService is IDisposable disposableDoorService)
                    disposableDoorService.Dispose();
                    
                if (_equipmentService is IDisposable disposableEquipmentService)
                    disposableEquipmentService.Dispose();
                    
                if (_passengerService is IDisposable disposablePassengerService)
                    disposablePassengerService.Dispose();
                    
                if (_cargoService is IDisposable disposableCargoService)
                    disposableCargoService.Dispose();
                    
                if (_fuelService is IDisposable disposableFuelService)
                    disposableFuelService.Dispose();
                    
                if (_fluidService is IDisposable disposableFluidService)
                    disposableFluidService.Dispose();
                    
                if (_flightDataService is IDisposable disposableFlightDataService)
                    disposableFlightDataService.Dispose();
                    
                if (_flightPlanService is IDisposable disposableFlightPlanService)
                    disposableFlightPlanService.Dispose();
                
                Logger.Log(LogLevel.Information, "ProsimControllerFacade:Dispose", "ProSim controller facade disposed");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimControllerFacade:Dispose", $"Error disposing ProSim controller facade: {ex.Message}");
            }
            
            base.Dispose();
        }
    }
}
