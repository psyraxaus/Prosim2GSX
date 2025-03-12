using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.ViewModels.Aircraft
{
    /// <summary>
    /// ViewModel for the aircraft visualization
    /// </summary>
    public class AircraftViewModel : BaseViewModel
    {
        private readonly IProsimDoorService _doorService;
        private readonly IProsimEquipmentService _equipmentService;
        private readonly IGSXFuelCoordinator _fuelCoordinator;
        private readonly IGSXServiceOrchestrator _serviceOrchestrator;
        private readonly IEventAggregator _eventAggregator;

        #region Properties

        // Flight phase
        private string _currentFlightPhase;
        public string CurrentFlightPhase
        {
            get => _currentFlightPhase;
            set => SetProperty(ref _currentFlightPhase, value);
        }

        // Status information
        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _connectionStatus;
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        private string _connectionStatusText;
        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set => SetProperty(ref _connectionStatusText, value);
        }

        // Door states
        private bool _forwardLeftDoorOpen;
        public bool ForwardLeftDoorOpen
        {
            get => _forwardLeftDoorOpen;
            set => SetProperty(ref _forwardLeftDoorOpen, value);
        }

        private bool _forwardRightDoorOpen;
        public bool ForwardRightDoorOpen
        {
            get => _forwardRightDoorOpen;
            set => SetProperty(ref _forwardRightDoorOpen, value);
        }

        private bool _aftLeftDoorOpen;
        public bool AftLeftDoorOpen
        {
            get => _aftLeftDoorOpen;
            set => SetProperty(ref _aftLeftDoorOpen, value);
        }

        private bool _aftRightDoorOpen;
        public bool AftRightDoorOpen
        {
            get => _aftRightDoorOpen;
            set => SetProperty(ref _aftRightDoorOpen, value);
        }

        private bool _forwardCargoDoorOpen;
        public bool ForwardCargoDoorOpen
        {
            get => _forwardCargoDoorOpen;
            set => SetProperty(ref _forwardCargoDoorOpen, value);
        }

        private bool _aftCargoDoorOpen;
        public bool AftCargoDoorOpen
        {
            get => _aftCargoDoorOpen;
            set => SetProperty(ref _aftCargoDoorOpen, value);
        }

        // Equipment states
        private bool _jetwayConnected;
        public bool JetwayConnected
        {
            get => _jetwayConnected;
            set => SetProperty(ref _jetwayConnected, value);
        }

        private bool _stairsConnected;
        public bool StairsConnected
        {
            get => _stairsConnected;
            set => SetProperty(ref _stairsConnected, value);
        }

        private bool _gpuConnected;
        public bool GpuConnected
        {
            get => _gpuConnected;
            set => SetProperty(ref _gpuConnected, value);
        }

        private bool _pcaConnected;
        public bool PcaConnected
        {
            get => _pcaConnected;
            set => SetProperty(ref _pcaConnected, value);
        }

        private bool _chocksPlaced;
        public bool ChocksPlaced
        {
            get => _chocksPlaced;
            set => SetProperty(ref _chocksPlaced, value);
        }

        // Service states
        private bool _refuelingInProgress;
        public bool RefuelingInProgress
        {
            get => _refuelingInProgress;
            set => SetProperty(ref _refuelingInProgress, value);
        }

        private double _refuelingProgress;
        public double RefuelingProgress
        {
            get => _refuelingProgress;
            set => SetProperty(ref _refuelingProgress, value);
        }

        private bool _cateringInProgress;
        public bool CateringInProgress
        {
            get => _cateringInProgress;
            set => SetProperty(ref _cateringInProgress, value);
        }

        private bool _boardingInProgress;
        public bool BoardingInProgress
        {
            get => _boardingInProgress;
            set => SetProperty(ref _boardingInProgress, value);
        }

        private double _boardingProgress;
        public double BoardingProgress
        {
            get => _boardingProgress;
            set => SetProperty(ref _boardingProgress, value);
        }

        private bool _deBoardingInProgress;
        public bool DeBoardingInProgress
        {
            get => _deBoardingInProgress;
            set => SetProperty(ref _deBoardingInProgress, value);
        }

        private double _deBoardingProgress;
        public double DeBoardingProgress
        {
            get => _deBoardingProgress;
            set => SetProperty(ref _deBoardingProgress, value);
        }

        private bool _cargoLoadingInProgress;
        public bool CargoLoadingInProgress
        {
            get => _cargoLoadingInProgress;
            set => SetProperty(ref _cargoLoadingInProgress, value);
        }

        private double _cargoLoadingProgress;
        public double CargoLoadingProgress
        {
            get => _cargoLoadingProgress;
            set => SetProperty(ref _cargoLoadingProgress, value);
        }

        private bool _cargoUnloadingInProgress;
        public bool CargoUnloadingInProgress
        {
            get => _cargoUnloadingInProgress;
            set => SetProperty(ref _cargoUnloadingInProgress, value);
        }

        private double _cargoUnloadingProgress;
        public double CargoUnloadingProgress
        {
            get => _cargoUnloadingProgress;
            set => SetProperty(ref _cargoUnloadingProgress, value);
        }

        // Service vehicle states
        private bool _fuelTruckPresent;
        public bool FuelTruckPresent
        {
            get => _fuelTruckPresent;
            set => SetProperty(ref _fuelTruckPresent, value);
        }

        private bool _cateringTruckPresent;
        public bool CateringTruckPresent
        {
            get => _cateringTruckPresent;
            set => SetProperty(ref _cateringTruckPresent, value);
        }

        private bool _passengerBusPresent;
        public bool PassengerBusPresent
        {
            get => _passengerBusPresent;
            set => SetProperty(ref _passengerBusPresent, value);
        }

        private bool _baggageTruckPresent;
        public bool BaggageTruckPresent
        {
            get => _baggageTruckPresent;
            set => SetProperty(ref _baggageTruckPresent, value);
        }

        // Computed properties
        public bool AnyServiceInProgress => 
            RefuelingInProgress || 
            CateringInProgress || 
            BoardingInProgress || 
            DeBoardingInProgress || 
            CargoLoadingInProgress || 
            CargoUnloadingInProgress;

        #endregion

        #region Commands

        public ICommand ToggleDoorCommand { get; }
        public ICommand ToggleEquipmentCommand { get; }
        public ICommand RequestServiceCommand { get; }
        public ICommand CancelServiceCommand { get; }

        #endregion

        public AircraftViewModel(
            IProsimDoorService doorService,
            IProsimEquipmentService equipmentService,
            IGSXFuelCoordinator fuelCoordinator,
            IGSXServiceOrchestrator serviceOrchestrator,
            IEventAggregator eventAggregator)
        {
            _doorService = doorService ?? throw new ArgumentNullException(nameof(doorService));
            _equipmentService = equipmentService ?? throw new ArgumentNullException(nameof(equipmentService));
            _fuelCoordinator = fuelCoordinator ?? throw new ArgumentNullException(nameof(fuelCoordinator));
            _serviceOrchestrator = serviceOrchestrator ?? throw new ArgumentNullException(nameof(serviceOrchestrator));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // Initialize commands
            ToggleDoorCommand = new RelayCommand<string>(ToggleDoor);
            ToggleEquipmentCommand = new RelayCommand<string>(ToggleEquipment);
            RequestServiceCommand = new RelayCommand<string>(RequestService);
            CancelServiceCommand = new RelayCommand<string>(CancelService);

            // Subscribe to events
            SubscribeToEvents();

            // Initialize state
            InitializeState();
        }

        private void SubscribeToEvents()
        {
            // Subscribe to door state changes
            _eventAggregator.Subscribe<DoorStateChangedEventArgs>(OnDoorStateChanged);
            
            // Subscribe to equipment state changes
            _eventAggregator.Subscribe<EquipmentStateChangedEventArgs>(OnEquipmentStateChanged);
            
            // Subscribe to fuel state changes
            _eventAggregator.Subscribe<FuelStateChangedEventArgs>(OnFuelStateChanged);
            
            // Subscribe to refueling progress changes
            _eventAggregator.Subscribe<RefuelingProgressChangedEventArgs>(OnRefuelingProgressChanged);
            
            // Subscribe to passenger state changes
            _eventAggregator.Subscribe<PassengerStateChangedEventArgs>(OnPassengerStateChanged);
            
            // Subscribe to cargo state changes
            _eventAggregator.Subscribe<CargoStateChangedEventArgs>(OnCargoStateChanged);
        }

        public void InitializeState()
        {
            // Initialize door states
            ForwardLeftDoorOpen = _doorService.IsForwardLeftDoorOpen();
            ForwardRightDoorOpen = _doorService.IsForwardRightDoorOpen();
            AftLeftDoorOpen = _doorService.IsAftLeftDoorOpen();
            AftRightDoorOpen = _doorService.IsAftRightDoorOpen();
            ForwardCargoDoorOpen = _doorService.IsForwardCargoDoorOpen();
            AftCargoDoorOpen = _doorService.IsAftCargoDoorOpen();

            // Initialize equipment states
            JetwayConnected = _equipmentService.IsJetwayConnected();
            StairsConnected = _equipmentService.IsStairsConnected();
            GpuConnected = _equipmentService.IsGpuConnected();
            PcaConnected = _equipmentService.IsPcaConnected();
            ChocksPlaced = _equipmentService.AreChocksPlaced();

            // Initialize service states
            RefuelingInProgress = _fuelCoordinator.IsRefuelingInProgress();
            RefuelingProgress = _fuelCoordinator.GetRefuelingProgress();
            
            // Initialize service vehicle states based on service states
            FuelTruckPresent = RefuelingInProgress;
            CateringTruckPresent = CateringInProgress;
            PassengerBusPresent = BoardingInProgress || DeBoardingInProgress;
            BaggageTruckPresent = CargoLoadingInProgress || CargoUnloadingInProgress;

            // Initialize status information
            CurrentFlightPhase = "GROUND";
            StatusMessage = "Aircraft ready for services";
            ConnectionStatus = "Connected";
            ConnectionStatusText = "Connected to GSX and ProSim";
        }

        #region Event Handlers

        private void OnDoorStateChanged(DoorStateChangedEventArgs args)
        {
            switch (args.DoorType)
            {
                case DoorType.ForwardLeft:
                    ForwardLeftDoorOpen = args.IsOpen;
                    break;
                case DoorType.ForwardRight:
                    ForwardRightDoorOpen = args.IsOpen;
                    break;
                case DoorType.AftLeft:
                    AftLeftDoorOpen = args.IsOpen;
                    break;
                case DoorType.AftRight:
                    AftRightDoorOpen = args.IsOpen;
                    break;
                case DoorType.ForwardCargo:
                    ForwardCargoDoorOpen = args.IsOpen;
                    break;
                case DoorType.AftCargo:
                    AftCargoDoorOpen = args.IsOpen;
                    break;
            }
        }

        private void OnEquipmentStateChanged(EquipmentStateChangedEventArgs args)
        {
            switch (args.EquipmentType)
            {
                case EquipmentType.Jetway:
                    JetwayConnected = args.IsConnected;
                    break;
                case EquipmentType.Stairs:
                    StairsConnected = args.IsConnected;
                    break;
                case EquipmentType.Gpu:
                    GpuConnected = args.IsConnected;
                    break;
                case EquipmentType.Pca:
                    PcaConnected = args.IsConnected;
                    break;
                case EquipmentType.Chocks:
                    ChocksPlaced = args.IsConnected;
                    break;
            }
        }

        private void OnFuelStateChanged(FuelStateChangedEventArgs args)
        {
            RefuelingInProgress = args.IsRefueling;
            FuelTruckPresent = args.IsRefueling;
        }

        private void OnRefuelingProgressChanged(RefuelingProgressChangedEventArgs args)
        {
            RefuelingProgress = args.Progress;
        }

        private void OnPassengerStateChanged(PassengerStateChangedEventArgs args)
        {
            BoardingInProgress = args.IsBoardingInProgress;
            BoardingProgress = args.BoardingProgress;
            DeBoardingInProgress = args.IsDeBoardingInProgress;
            DeBoardingProgress = args.DeBoardingProgress;
            PassengerBusPresent = args.IsBoardingInProgress || args.IsDeBoardingInProgress;
        }

        private void OnCargoStateChanged(CargoStateChangedEventArgs args)
        {
            CargoLoadingInProgress = args.IsLoadingInProgress;
            CargoLoadingProgress = args.LoadingProgress;
            CargoUnloadingInProgress = args.IsUnloadingInProgress;
            CargoUnloadingProgress = args.UnloadingProgress;
            BaggageTruckPresent = args.IsLoadingInProgress || args.IsUnloadingInProgress;
        }

        #endregion

        #region Command Handlers

        private void ToggleDoor(string doorType)
        {
            if (string.IsNullOrEmpty(doorType))
                return;

            try
            {
                switch (doorType)
                {
                    case "ForwardLeft":
                        _doorService.ToggleForwardLeftDoor();
                        break;
                    case "ForwardRight":
                        _doorService.ToggleForwardRightDoor();
                        break;
                    case "AftLeft":
                        _doorService.ToggleAftLeftDoor();
                        break;
                    case "AftRight":
                        _doorService.ToggleAftRightDoor();
                        break;
                    case "ForwardCargo":
                        _doorService.ToggleForwardCargoDoor();
                        break;
                    case "AftCargo":
                        _doorService.ToggleAftCargoDoor();
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Logger.LogError($"Error toggling door {doorType}: {ex.Message}");
            }
        }

        private void ToggleEquipment(string equipmentType)
        {
            if (string.IsNullOrEmpty(equipmentType))
                return;

            try
            {
                switch (equipmentType)
                {
                    case "Jetway":
                        _equipmentService.ToggleJetway();
                        break;
                    case "Stairs":
                        _equipmentService.ToggleStairs();
                        break;
                    case "Gpu":
                        _equipmentService.ToggleGpu();
                        break;
                    case "Pca":
                        _equipmentService.TogglePca();
                        break;
                    case "Chocks":
                        _equipmentService.ToggleChocks();
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Logger.LogError($"Error toggling equipment {equipmentType}: {ex.Message}");
            }
        }

        private void RequestService(string serviceType)
        {
            if (string.IsNullOrEmpty(serviceType))
                return;

            try
            {
                switch (serviceType)
                {
                    case "Refueling":
                        _serviceOrchestrator.RequestRefueling();
                        break;
                    case "Catering":
                        _serviceOrchestrator.RequestCatering();
                        break;
                    case "Boarding":
                        _serviceOrchestrator.RequestBoarding();
                        break;
                    case "DeBoarding":
                        _serviceOrchestrator.RequestDeBoarding();
                        break;
                    case "CargoLoading":
                        _serviceOrchestrator.RequestCargoLoading();
                        break;
                    case "CargoUnloading":
                        _serviceOrchestrator.RequestCargoUnloading();
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Logger.LogError($"Error requesting service {serviceType}: {ex.Message}");
            }
        }

        private void CancelService(string serviceType)
        {
            if (string.IsNullOrEmpty(serviceType))
                return;

            try
            {
                switch (serviceType)
                {
                    case "Refueling":
                        _serviceOrchestrator.CancelRefueling();
                        break;
                    case "Catering":
                        _serviceOrchestrator.CancelCatering();
                        break;
                    case "Boarding":
                        _serviceOrchestrator.CancelBoarding();
                        break;
                    case "DeBoarding":
                        _serviceOrchestrator.CancelDeBoarding();
                        break;
                    case "CargoLoading":
                        _serviceOrchestrator.CancelCargoLoading();
                        break;
                    case "CargoUnloading":
                        _serviceOrchestrator.CancelCargoUnloading();
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Logger.LogError($"Error canceling service {serviceType}: {ex.Message}");
            }
        }

        #endregion

        public override void Cleanup()
        {
            // Unsubscribe from events
            _eventAggregator.Unsubscribe<DoorStateChangedEventArgs>(OnDoorStateChanged);
            _eventAggregator.Unsubscribe<EquipmentStateChangedEventArgs>(OnEquipmentStateChanged);
            _eventAggregator.Unsubscribe<FuelStateChangedEventArgs>(OnFuelStateChanged);
            _eventAggregator.Unsubscribe<RefuelingProgressChangedEventArgs>(OnRefuelingProgressChanged);
            _eventAggregator.Unsubscribe<PassengerStateChangedEventArgs>(OnPassengerStateChanged);
            _eventAggregator.Unsubscribe<CargoStateChangedEventArgs>(OnCargoStateChanged);

            base.Cleanup();
        }
    }
}
