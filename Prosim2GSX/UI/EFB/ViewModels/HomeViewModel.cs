using System;
using System.Windows.Input;
using Prosim2GSX.Models;
using Prosim2GSX.UI.EFB.Controls;
using Prosim2GSX.UI.EFB.Navigation;

namespace Prosim2GSX.UI.EFB.ViewModels
{
    /// <summary>
    /// View model for the Home page.
    /// </summary>
    public class HomeViewModel : BaseViewModel
    {
        private readonly EFBDataBindingService _dataBindingService;
        private readonly EFBNavigationService _navigationService;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
        /// </summary>
        /// <param name="dataBindingService">The data binding service.</param>
        /// <param name="navigationService">The navigation service.</param>
        public HomeViewModel(EFBDataBindingService dataBindingService, EFBNavigationService navigationService)
        {
            _dataBindingService = dataBindingService ?? throw new ArgumentNullException(nameof(dataBindingService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            // Initialize commands
            NavigateToAircraftCommand = new RelayCommand(NavigateToAircraft);
            NavigateToFuelCommand = new RelayCommand(NavigateToFuel);
            NavigateToDoorsCommand = new RelayCommand(NavigateToDoors);
            NavigateToPassengersCommand = new RelayCommand(NavigateToPassengers);
            NavigateToCargoCommand = new RelayCommand(NavigateToCargo);
            NavigateToEquipmentCommand = new RelayCommand(NavigateToEquipment);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettings);
            
            // Initialize properties
            CurrentFlightPhase = FlightPhaseIndicator.FlightPhase.Preflight;
            
            // Subscribe to property changes
            SubscribeToPropertyChanges();
        }
        
        /// <summary>
        /// Gets the command to navigate to the Aircraft page.
        /// </summary>
        public ICommand NavigateToAircraftCommand { get; }
        
        /// <summary>
        /// Gets the command to navigate to the Fuel page.
        /// </summary>
        public ICommand NavigateToFuelCommand { get; }
        
        /// <summary>
        /// Gets the command to navigate to the Doors page.
        /// </summary>
        public ICommand NavigateToDoorsCommand { get; }
        
        /// <summary>
        /// Gets the command to navigate to the Passengers page.
        /// </summary>
        public ICommand NavigateToPassengersCommand { get; }
        
        /// <summary>
        /// Gets the command to navigate to the Cargo page.
        /// </summary>
        public ICommand NavigateToCargoCommand { get; }
        
        /// <summary>
        /// Gets the command to navigate to the Equipment page.
        /// </summary>
        public ICommand NavigateToEquipmentCommand { get; }
        
        /// <summary>
        /// Gets the command to navigate to the Settings page.
        /// </summary>
        public ICommand NavigateToSettingsCommand { get; }
        
        /// <summary>
        /// Gets or sets the current flight phase.
        /// </summary>
        public FlightPhaseIndicator.FlightPhase CurrentFlightPhase
        {
            get => GetProperty<FlightPhaseIndicator.FlightPhase>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the ProsimA320 connection is active.
        /// </summary>
        public bool IsProsimConnected
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the MSFS2020 connection is active.
        /// </summary>
        public bool IsSimConnected
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the GSX service is active.
        /// </summary>
        public bool IsGSXActive
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets the fuel quantity in the left tank.
        /// </summary>
        public double LeftTankFuel
        {
            get => GetProperty<double>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets the fuel quantity in the center tank.
        /// </summary>
        public double CenterTankFuel
        {
            get => GetProperty<double>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets the fuel quantity in the right tank.
        /// </summary>
        public double RightTankFuel
        {
            get => GetProperty<double>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets the total fuel quantity.
        /// </summary>
        public double TotalFuel
        {
            get => GetProperty<double>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets the passenger count.
        /// </summary>
        public int PassengerCount
        {
            get => GetProperty<int>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets the cargo weight.
        /// </summary>
        public double CargoWeight
        {
            get => GetProperty<double>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets the refueling progress.
        /// </summary>
        public double RefuelingProgress
        {
            get => GetProperty<double>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets the boarding progress.
        /// </summary>
        public double BoardingProgress
        {
            get => GetProperty<double>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets the deboarding progress.
        /// </summary>
        public double DeboardingProgress
        {
            get => GetProperty<double>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets the cargo loading progress.
        /// </summary>
        public double CargoLoadingProgress
        {
            get => GetProperty<double>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Gets or sets the cargo unloading progress.
        /// </summary>
        public double CargoUnloadingProgress
        {
            get => GetProperty<double>();
            set => SetProperty(value);
        }
        
        /// <summary>
        /// Initializes the view model.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            
            // Update properties from service model
            UpdateProperties();
        }
        
        /// <summary>
        /// Cleans up the view model.
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
            
            // Unsubscribe from property changes
            UnsubscribeFromPropertyChanges();
        }
        
        private void SubscribeToPropertyChanges()
        {
            // Subscribe to property changes from service model with null-safe conversions
            _dataBindingService.Subscribe("IsProsimRunning", value => IsProsimConnected = value as bool? ?? false);
            _dataBindingService.Subscribe("IsSimRunning", value => IsSimConnected = value as bool? ?? false);
            _dataBindingService.Subscribe("IsGSXActive", value => IsGSXActive = value as bool? ?? false);
            _dataBindingService.Subscribe("LeftTankFuel", value => LeftTankFuel = value as double? ?? 0.0);
            _dataBindingService.Subscribe("CenterTankFuel", value => CenterTankFuel = value as double? ?? 0.0);
            _dataBindingService.Subscribe("RightTankFuel", value => RightTankFuel = value as double? ?? 0.0);
            _dataBindingService.Subscribe("TotalFuel", value => TotalFuel = value as double? ?? 0.0);
            _dataBindingService.Subscribe("PassengerCount", value => PassengerCount = value as int? ?? 0);
            _dataBindingService.Subscribe("CargoWeight", value => CargoWeight = value as double? ?? 0.0);
            _dataBindingService.Subscribe("RefuelingProgress", value => RefuelingProgress = value as double? ?? 0.0);
            _dataBindingService.Subscribe("BoardingProgress", value => BoardingProgress = value as double? ?? 0.0);
            _dataBindingService.Subscribe("DeboardingProgress", value => DeboardingProgress = value as double? ?? 0.0);
            _dataBindingService.Subscribe("CargoLoadingProgress", value => CargoLoadingProgress = value as double? ?? 0.0);
            _dataBindingService.Subscribe("CargoUnloadingProgress", value => CargoUnloadingProgress = value as double? ?? 0.0);
            _dataBindingService.Subscribe("CurrentFlightPhase", value => CurrentFlightPhase = value != null ? 
                (FlightPhaseIndicator.FlightPhase)value : FlightPhaseIndicator.FlightPhase.Preflight);
        }
        
        private void UnsubscribeFromPropertyChanges()
        {
            // This will be implemented in Phase 3
        }
        
        private void UpdateProperties()
        {
            // Update properties from service model with null-safe conversions
            // GetValue<T> already returns default(T) for missing properties, but we'll add extra safety
            IsProsimConnected = _dataBindingService.GetValue<bool>("IsProsimRunning");
            IsSimConnected = _dataBindingService.GetValue<bool>("IsSimRunning");
            IsGSXActive = _dataBindingService.GetValue<bool>("IsGSXActive");
            LeftTankFuel = _dataBindingService.GetValue<double>("LeftTankFuel");
            CenterTankFuel = _dataBindingService.GetValue<double>("CenterTankFuel");
            RightTankFuel = _dataBindingService.GetValue<double>("RightTankFuel");
            TotalFuel = _dataBindingService.GetValue<double>("TotalFuel");
            PassengerCount = _dataBindingService.GetValue<int>("PassengerCount");
            CargoWeight = _dataBindingService.GetValue<double>("CargoWeight");
            RefuelingProgress = _dataBindingService.GetValue<double>("RefuelingProgress");
            BoardingProgress = _dataBindingService.GetValue<double>("BoardingProgress");
            DeboardingProgress = _dataBindingService.GetValue<double>("DeboardingProgress");
            CargoLoadingProgress = _dataBindingService.GetValue<double>("CargoLoadingProgress");
            CargoUnloadingProgress = _dataBindingService.GetValue<double>("CargoUnloadingProgress");
            
            // For enum properties, we need to handle the case where the default(T) is 0
            var flightPhase = _dataBindingService.GetValue<FlightPhaseIndicator.FlightPhase>("CurrentFlightPhase");
            CurrentFlightPhase = flightPhase; // This is safe because enums default to their first value (0)
        }
        
        private void NavigateToAircraft()
        {
            _navigationService.NavigateTo("Aircraft");
        }
        
        private void NavigateToFuel()
        {
            _navigationService.NavigateTo("Fuel");
        }
        
        private void NavigateToDoors()
        {
            _navigationService.NavigateTo("Doors");
        }
        
        private void NavigateToPassengers()
        {
            _navigationService.NavigateTo("Passengers");
        }
        
        private void NavigateToCargo()
        {
            _navigationService.NavigateTo("Cargo");
        }
        
        private void NavigateToEquipment()
        {
            _navigationService.NavigateTo("Equipment");
        }
        
        private void NavigateToSettings()
        {
            _navigationService.NavigateTo("Settings");
        }
    }
    
    /// <summary>
    /// Implementation of the ICommand interface.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute action.</param>
        /// <param name="canExecute">The can execute function.</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        /// <summary>
        /// Event raised when the can execute state changes.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        
        /// <summary>
        /// Determines whether the command can execute.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        /// <returns>True if the command can execute, false otherwise.</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }
        
        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
