using System;
using System.Collections.Generic;
using System.Linq;
using Prosim2GSX.Services;
using Prosim2GSX.Services.EventArgs;
using Prosim2GSX.UI.EFB.Controls;

namespace Prosim2GSX.UI.EFB.Phase
{
    /// <summary>
    /// Service for managing phase contexts.
    /// </summary>
    public class PhaseContextService : IPhaseContextService
    {
        private readonly IFlightPhaseService _flightPhaseService;
        private readonly IEventAggregator _eventAggregator;
        
        private readonly Dictionary<FlightPhaseIndicator.FlightPhase, PhaseContext> _phaseContexts = 
            new Dictionary<FlightPhaseIndicator.FlightPhase, PhaseContext>();
        
        private PhaseContext _currentContext;
        private FlightPhaseIndicator.FlightPhase _currentPhase;
        
        /// <summary>
        /// Gets the current phase context.
        /// </summary>
        public PhaseContext CurrentContext => _currentContext;
        
        /// <summary>
        /// Gets the current flight phase.
        /// </summary>
        public FlightPhaseIndicator.FlightPhase CurrentPhase => _currentPhase;
        
        /// <summary>
        /// Event raised when the phase context changes.
        /// </summary>
        public event EventHandler<PhaseContextChangedEventArgs> ContextChanged;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PhaseContextService"/> class.
        /// </summary>
        /// <param name="flightPhaseService">The flight phase service.</param>
        /// <param name="eventAggregator">The event aggregator.</param>
        public PhaseContextService(IFlightPhaseService flightPhaseService, IEventAggregator eventAggregator)
        {
            _flightPhaseService = flightPhaseService ?? throw new ArgumentNullException(nameof(flightPhaseService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            
            // Initialize with default contexts for all phases
            InitializeDefaultContexts();
        }
        
        /// <summary>
        /// Initializes the phase context service.
        /// </summary>
        public void Initialize()
        {
            // Subscribe to flight phase changes
            _flightPhaseService.PhaseChanged += OnFlightPhaseChanged;
            _eventAggregator.Subscribe<FlightPhaseChangedEventArgs>(OnFlightPhaseChangedEvent);
            
            // Initialize current phase and context
            _currentPhase = _flightPhaseService.CurrentPhase;
            _currentContext = GetContextForPhase(_currentPhase);
            
            Logger.Log(LogLevel.Information, "PhaseContextService:Initialize", 
                $"Phase context service initialized with current phase: {_currentPhase}");
        }
        
        /// <summary>
        /// Registers a phase context for a specific flight phase.
        /// </summary>
        /// <param name="phase">The flight phase.</param>
        /// <param name="context">The phase context.</param>
        public void RegisterPhaseContext(FlightPhaseIndicator.FlightPhase phase, PhaseContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
                
            _phaseContexts[phase] = context;
            
            // If this is the current phase, update the current context
            if (phase == _currentPhase)
            {
                var previousContext = _currentContext;
                _currentContext = context;
                
                // Raise event
                OnContextChanged(_currentPhase, _currentPhase, previousContext, _currentContext);
            }
            
            Logger.Log(LogLevel.Information, "PhaseContextService:RegisterPhaseContext", 
                $"Registered phase context for phase: {phase}");
        }
        
        /// <summary>
        /// Gets the phase context for a specific flight phase.
        /// </summary>
        /// <param name="phase">The flight phase.</param>
        /// <returns>The phase context for the specified flight phase.</returns>
        public PhaseContext GetContextForPhase(FlightPhaseIndicator.FlightPhase phase)
        {
            if (_phaseContexts.TryGetValue(phase, out var context))
                return context;
                
            // If no context is registered for the phase, create a default one
            var defaultContext = CreateDefaultContext(phase);
            _phaseContexts[phase] = defaultContext;
            
            return defaultContext;
        }
        
        /// <summary>
        /// Updates the current phase context based on the current flight phase.
        /// </summary>
        public void UpdateCurrentContext()
        {
            var currentPhase = _flightPhaseService.CurrentPhase;
            
            if (currentPhase != _currentPhase)
            {
                var previousPhase = _currentPhase;
                var previousContext = _currentContext;
                
                _currentPhase = currentPhase;
                _currentContext = GetContextForPhase(currentPhase);
                
                // Raise event
                OnContextChanged(previousPhase, _currentPhase, previousContext, _currentContext);
            }
        }
        
        /// <summary>
        /// Checks if a control is visible in the current phase context.
        /// </summary>
        /// <param name="controlName">The name of the control.</param>
        /// <returns>True if the control is visible, false otherwise.</returns>
        public bool IsControlVisible(string controlName)
        {
            if (string.IsNullOrEmpty(controlName))
                throw new ArgumentNullException(nameof(controlName));
                
            if (_currentContext.ControlVisibility.TryGetValue(controlName, out var isVisible))
                return isVisible;
                
            // If not specified, default to visible
            return true;
        }
        
        /// <summary>
        /// Checks if a control is enabled in the current phase context.
        /// </summary>
        /// <param name="controlName">The name of the control.</param>
        /// <returns>True if the control is enabled, false otherwise.</returns>
        public bool IsControlEnabled(string controlName)
        {
            if (string.IsNullOrEmpty(controlName))
                throw new ArgumentNullException(nameof(controlName));
                
            if (_currentContext.ControlEnabled.TryGetValue(controlName, out var isEnabled))
                return isEnabled;
                
            // If not specified, default to enabled
            return true;
        }
        
        /// <summary>
        /// Gets the recommended actions for the current phase context.
        /// </summary>
        /// <returns>The list of recommended actions.</returns>
        public string[] GetRecommendedActions()
        {
            return _currentContext.RecommendedActions.ToArray();
        }
        
        /// <summary>
        /// Gets the available services for the current phase context.
        /// </summary>
        /// <returns>The list of available services.</returns>
        public string[] GetAvailableServices()
        {
            return _currentContext.AvailableServices.ToArray();
        }
        
        /// <summary>
        /// Gets the notifications for the current phase context.
        /// </summary>
        /// <returns>The list of notifications.</returns>
        public PhaseNotification[] GetNotifications()
        {
            return _currentContext.Notifications.ToArray();
        }
        
        /// <summary>
        /// Gets the checklists for the current phase context.
        /// </summary>
        /// <returns>The list of checklists.</returns>
        public PhaseChecklist[] GetChecklists()
        {
            return _currentContext.Checklists.ToArray();
        }
        
        /// <summary>
        /// Gets the actions for the current phase context.
        /// </summary>
        /// <returns>The list of actions.</returns>
        public PhaseAction[] GetActions()
        {
            return _currentContext.Actions.ToArray();
        }
        
        /// <summary>
        /// Gets the recommended actions for the current phase context.
        /// </summary>
        /// <returns>The list of recommended actions.</returns>
        public PhaseAction[] GetRecommendedPhaseActions()
        {
            return _currentContext.Actions.Where(a => a.IsRecommended).ToArray();
        }
        
        private void InitializeDefaultContexts()
        {
            // Create default contexts for all phases
            foreach (FlightPhaseIndicator.FlightPhase phase in Enum.GetValues(typeof(FlightPhaseIndicator.FlightPhase)))
            {
                if (!_phaseContexts.ContainsKey(phase))
                {
                    _phaseContexts[phase] = CreateDefaultContext(phase);
                }
            }
        }
        
        private PhaseContext CreateDefaultContext(FlightPhaseIndicator.FlightPhase phase)
        {
            switch (phase)
            {
                case FlightPhaseIndicator.FlightPhase.Preflight:
                    return CreatePreflightContext();
                case FlightPhaseIndicator.FlightPhase.Departure:
                    return CreateDepartureContext();
                case FlightPhaseIndicator.FlightPhase.TaxiOut:
                    return CreateTaxiOutContext();
                case FlightPhaseIndicator.FlightPhase.Flight:
                    return CreateFlightContext();
                case FlightPhaseIndicator.FlightPhase.TaxiIn:
                    return CreateTaxiInContext();
                case FlightPhaseIndicator.FlightPhase.Arrival:
                    return CreateArrivalContext();
                case FlightPhaseIndicator.FlightPhase.Turnaround:
                    return CreateTurnaroundContext();
                default:
                    return new PhaseContext($"Unknown Phase: {phase}", "No description available.");
            }
        }
        
        private PhaseContext CreatePreflightContext()
        {
            var context = new PhaseContext("Preflight", "Prepare the aircraft for departure.");
            
            // Add recommended actions
            context.AddRecommendedAction("Load flight plan");
            context.AddRecommendedAction("Configure aircraft");
            context.AddRecommendedAction("Request ground services");
            
            // Add available services
            context.AddAvailableService("Ground Power");
            context.AddAvailableService("Pre-Conditioned Air");
            context.AddAvailableService("Chocks");
            context.AddAvailableService("Jetway");
            
            // Set control visibility and enabled state
            context.SetControlVisibility("RefuelingPanel", true);
            context.SetControlVisibility("BoardingPanel", true);
            context.SetControlVisibility("CateringPanel", true);
            context.SetControlVisibility("CargoPanel", true);
            
            context.SetControlEnabled("RefuelingPanel", true);
            context.SetControlEnabled("BoardingPanel", false);
            context.SetControlEnabled("CateringPanel", true);
            context.SetControlEnabled("CargoPanel", true);
            
            // Add notifications
            context.AddNotification(new PhaseNotification(
                "preflight-welcome",
                NotificationType.Info,
                "Welcome to the preflight phase. Please load your flight plan to begin."));
                
            // Add checklists
            var preflightChecklist = new PhaseChecklist("preflight-checklist", "Preflight Checklist");
            preflightChecklist.AddItem(new PhaseChecklistItem("load-flight-plan", "Load flight plan"));
            preflightChecklist.AddItem(new PhaseChecklistItem("configure-aircraft", "Configure aircraft"));
            preflightChecklist.AddItem(new PhaseChecklistItem("request-services", "Request ground services"));
            context.AddChecklist(preflightChecklist);
            
            // Set layout configuration
            context.LayoutConfiguration.LayoutTemplate = "PreflightLayout";
            context.LayoutConfiguration.SetPanelConfiguration("ServicesPanel", new PanelConfiguration
            {
                IsVisible = true,
                Size = PanelSize.Large,
                Position = PanelPosition.Right,
                ContentTemplate = "ServicesTemplate"
            });
            
            return context;
        }
        
        private PhaseContext CreateDepartureContext()
        {
            var context = new PhaseContext("Departure", "Prepare for pushback and engine start.");
            
            // Add recommended actions
            context.AddRecommendedAction("Complete refueling");
            context.AddRecommendedAction("Complete boarding");
            context.AddRecommendedAction("Complete cargo loading");
            context.AddRecommendedAction("Request pushback");
            
            // Add available services
            context.AddAvailableService("Refueling");
            context.AddAvailableService("Boarding");
            context.AddAvailableService("Catering");
            context.AddAvailableService("Cargo Loading");
            
            // Set control visibility and enabled state
            context.SetControlVisibility("RefuelingPanel", true);
            context.SetControlVisibility("BoardingPanel", true);
            context.SetControlVisibility("CateringPanel", true);
            context.SetControlVisibility("CargoPanel", true);
            context.SetControlVisibility("PushbackPanel", true);
            
            context.SetControlEnabled("RefuelingPanel", true);
            context.SetControlEnabled("BoardingPanel", true);
            context.SetControlEnabled("CateringPanel", true);
            context.SetControlEnabled("CargoPanel", true);
            context.SetControlEnabled("PushbackPanel", false);
            
            // Add notifications
            context.AddNotification(new PhaseNotification(
                "departure-services",
                NotificationType.Info,
                "Complete all services before requesting pushback."));
                
            // Add checklists
            var departureChecklist = new PhaseChecklist("departure-checklist", "Departure Checklist");
            departureChecklist.AddItem(new PhaseChecklistItem("complete-refueling", "Complete refueling"));
            departureChecklist.AddItem(new PhaseChecklistItem("complete-boarding", "Complete boarding"));
            departureChecklist.AddItem(new PhaseChecklistItem("complete-cargo", "Complete cargo loading"));
            departureChecklist.AddItem(new PhaseChecklistItem("doors-closed", "Ensure all doors are closed"));
            departureChecklist.AddItem(new PhaseChecklistItem("request-pushback", "Request pushback"));
            context.AddChecklist(departureChecklist);
            
            // Set layout configuration
            context.LayoutConfiguration.LayoutTemplate = "DepartureLayout";
            context.LayoutConfiguration.SetPanelConfiguration("ServicesPanel", new PanelConfiguration
            {
                IsVisible = true,
                Size = PanelSize.Large,
                Position = PanelPosition.Right,
                ContentTemplate = "ServicesTemplate"
            });
            
            return context;
        }
        
        private PhaseContext CreateTaxiOutContext()
        {
            var context = new PhaseContext("Taxi Out", "Taxi to the runway for takeoff.");
            
            // Add recommended actions
            context.AddRecommendedAction("Complete before takeoff checklist");
            context.AddRecommendedAction("Monitor ground traffic");
            
            // Add available services
            // No services available during taxi
            
            // Set control visibility and enabled state
            context.SetControlVisibility("RefuelingPanel", false);
            context.SetControlVisibility("BoardingPanel", false);
            context.SetControlVisibility("CateringPanel", false);
            context.SetControlVisibility("CargoPanel", false);
            context.SetControlVisibility("PushbackPanel", false);
            context.SetControlVisibility("TaxiPanel", true);
            
            context.SetControlEnabled("TaxiPanel", true);
            
            // Add notifications
            context.AddNotification(new PhaseNotification(
                "taxiout-checklist",
                NotificationType.Info,
                "Complete before takeoff checklist before reaching the runway."));
                
            // Add checklists
            var taxiOutChecklist = new PhaseChecklist("taxiout-checklist", "Taxi Out Checklist");
            taxiOutChecklist.AddItem(new PhaseChecklistItem("flaps-set", "Set flaps for takeoff"));
            taxiOutChecklist.AddItem(new PhaseChecklistItem("transponder-on", "Transponder on"));
            taxiOutChecklist.AddItem(new PhaseChecklistItem("takeoff-briefing", "Complete takeoff briefing"));
            context.AddChecklist(taxiOutChecklist);
            
            // Set layout configuration
            context.LayoutConfiguration.LayoutTemplate = "TaxiOutLayout";
            context.LayoutConfiguration.SetPanelConfiguration("ChecklistPanel", new PanelConfiguration
            {
                IsVisible = true,
                Size = PanelSize.Normal,
                Position = PanelPosition.Right,
                ContentTemplate = "ChecklistTemplate"
            });
            
            return context;
        }
        
        private PhaseContext CreateFlightContext()
        {
            var context = new PhaseContext("Flight", "In-flight operations.");
            
            // Add recommended actions
            context.AddRecommendedAction("Monitor flight parameters");
            context.AddRecommendedAction("Prepare for arrival");
            
            // Add available services
            // No services available during flight
            
            // Set control visibility and enabled state
            context.SetControlVisibility("RefuelingPanel", false);
            context.SetControlVisibility("BoardingPanel", false);
            context.SetControlVisibility("CateringPanel", false);
            context.SetControlVisibility("CargoPanel", false);
            context.SetControlVisibility("PushbackPanel", false);
            context.SetControlVisibility("TaxiPanel", false);
            context.SetControlVisibility("FlightPanel", true);
            
            context.SetControlEnabled("FlightPanel", true);
            
            // Add notifications
            context.AddNotification(new PhaseNotification(
                "flight-monitor",
                NotificationType.Info,
                "Monitor flight parameters and prepare for arrival."));
                
            // Add checklists
            var flightChecklist = new PhaseChecklist("flight-checklist", "Flight Checklist");
            flightChecklist.AddItem(new PhaseChecklistItem("cruise-altitude", "Reach cruise altitude"));
            flightChecklist.AddItem(new PhaseChecklistItem("monitor-systems", "Monitor aircraft systems"));
            flightChecklist.AddItem(new PhaseChecklistItem("prepare-arrival", "Prepare for arrival"));
            context.AddChecklist(flightChecklist);
            
            // Set layout configuration
            context.LayoutConfiguration.LayoutTemplate = "FlightLayout";
            context.LayoutConfiguration.SetPanelConfiguration("FlightPanel", new PanelConfiguration
            {
                IsVisible = true,
                Size = PanelSize.Large,
                Position = PanelPosition.Center,
                ContentTemplate = "FlightTemplate"
            });
            
            return context;
        }
        
        private PhaseContext CreateTaxiInContext()
        {
            var context = new PhaseContext("Taxi In", "Taxi to the gate after landing.");
            
            // Add recommended actions
            context.AddRecommendedAction("Complete after landing checklist");
            context.AddRecommendedAction("Monitor ground traffic");
            
            // Add available services
            // No services available during taxi
            
            // Set control visibility and enabled state
            context.SetControlVisibility("RefuelingPanel", false);
            context.SetControlVisibility("BoardingPanel", false);
            context.SetControlVisibility("CateringPanel", false);
            context.SetControlVisibility("CargoPanel", false);
            context.SetControlVisibility("PushbackPanel", false);
            context.SetControlVisibility("TaxiPanel", true);
            
            context.SetControlEnabled("TaxiPanel", true);
            
            // Add notifications
            context.AddNotification(new PhaseNotification(
                "taxiin-checklist",
                NotificationType.Info,
                "Complete after landing checklist before reaching the gate."));
                
            // Add checklists
            var taxiInChecklist = new PhaseChecklist("taxiin-checklist", "Taxi In Checklist");
            taxiInChecklist.AddItem(new PhaseChecklistItem("flaps-up", "Flaps up"));
            taxiInChecklist.AddItem(new PhaseChecklistItem("apu-start", "Start APU"));
            taxiInChecklist.AddItem(new PhaseChecklistItem("prepare-shutdown", "Prepare for engine shutdown"));
            context.AddChecklist(taxiInChecklist);
            
            // Set layout configuration
            context.LayoutConfiguration.LayoutTemplate = "TaxiInLayout";
            context.LayoutConfiguration.SetPanelConfiguration("ChecklistPanel", new PanelConfiguration
            {
                IsVisible = true,
                Size = PanelSize.Normal,
                Position = PanelPosition.Right,
                ContentTemplate = "ChecklistTemplate"
            });
            
            return context;
        }
        
        private PhaseContext CreateArrivalContext()
        {
            var context = new PhaseContext("Arrival", "Arrival at the gate.");
            
            // Add recommended actions
            context.AddRecommendedAction("Connect jetway/stairs");
            context.AddRecommendedAction("Connect ground power");
            context.AddRecommendedAction("Request deboarding");
            
            // Add available services
            context.AddAvailableService("Jetway/Stairs");
            context.AddAvailableService("Ground Power");
            context.AddAvailableService("Deboarding");
            context.AddAvailableService("Cargo Unloading");
            
            // Set control visibility and enabled state
            context.SetControlVisibility("RefuelingPanel", false);
            context.SetControlVisibility("BoardingPanel", false);
            context.SetControlVisibility("CateringPanel", false);
            context.SetControlVisibility("CargoPanel", true);
            context.SetControlVisibility("DeboardingPanel", true);
            context.SetControlVisibility("EquipmentPanel", true);
            
            context.SetControlEnabled("CargoPanel", true);
            context.SetControlEnabled("DeboardingPanel", true);
            context.SetControlEnabled("EquipmentPanel", true);
            
            // Add notifications
            context.AddNotification(new PhaseNotification(
                "arrival-services",
                NotificationType.Info,
                "Connect ground services and request deboarding."));
                
            // Add checklists
            var arrivalChecklist = new PhaseChecklist("arrival-checklist", "Arrival Checklist");
            arrivalChecklist.AddItem(new PhaseChecklistItem("engines-off", "Engines off"));
            arrivalChecklist.AddItem(new PhaseChecklistItem("connect-jetway", "Connect jetway/stairs"));
            arrivalChecklist.AddItem(new PhaseChecklistItem("connect-power", "Connect ground power"));
            arrivalChecklist.AddItem(new PhaseChecklistItem("request-deboarding", "Request deboarding"));
            context.AddChecklist(arrivalChecklist);
            
            // Set layout configuration
            context.LayoutConfiguration.LayoutTemplate = "ArrivalLayout";
            context.LayoutConfiguration.SetPanelConfiguration("ServicesPanel", new PanelConfiguration
            {
                IsVisible = true,
                Size = PanelSize.Large,
                Position = PanelPosition.Right,
                ContentTemplate = "ServicesTemplate"
            });
            
            return context;
        }
        
        private PhaseContext CreateTurnaroundContext()
        {
            var context = new PhaseContext("Turnaround", "Prepare for the next flight.");
            
            // Add recommended actions
            context.AddRecommendedAction("Load new flight plan");
            context.AddRecommendedAction("Request refueling");
            context.AddRecommendedAction("Request catering");
            
            // Add available services
            context.AddAvailableService("Refueling");
            context.AddAvailableService("Catering");
            context.AddAvailableService("Cargo Loading");
            context.AddAvailableService("Boarding");
            
            // Set control visibility and enabled state
            context.SetControlVisibility("RefuelingPanel", true);
            context.SetControlVisibility("BoardingPanel", true);
            context.SetControlVisibility("CateringPanel", true);
            context.SetControlVisibility("CargoPanel", true);
            context.SetControlVisibility("DeboardingPanel", false);
            context.SetControlVisibility("EquipmentPanel", true);
            
            context.SetControlEnabled("RefuelingPanel", true);
            context.SetControlEnabled("BoardingPanel", true);
            context.SetControlEnabled("CateringPanel", true);
            context.SetControlEnabled("CargoPanel", true);
            context.SetControlEnabled("EquipmentPanel", true);
            
            // Add notifications
            context.AddNotification(new PhaseNotification(
                "turnaround-flightplan",
                NotificationType.Info,
                "Load a new flight plan to begin the next flight."));
                
            // Add checklists
            var turnaroundChecklist = new PhaseChecklist("turnaround-checklist", "Turnaround Checklist");
            turnaroundChecklist.AddItem(new PhaseChecklistItem("load-flightplan", "Load new flight plan"));
            turnaroundChecklist.AddItem(new PhaseChecklistItem("request-refueling", "Request refueling"));
            turnaroundChecklist.AddItem(new PhaseChecklistItem("request-catering", "Request catering"));
            turnaroundChecklist.AddItem(new PhaseChecklistItem("request-cargo", "Request cargo loading"));
            context.AddChecklist(turnaroundChecklist);
            
            // Set layout configuration
            context.LayoutConfiguration.LayoutTemplate = "TurnaroundLayout";
            context.LayoutConfiguration.SetPanelConfiguration("ServicesPanel", new PanelConfiguration
            {
                IsVisible = true,
                Size = PanelSize.Large,
                Position = PanelPosition.Right,
                ContentTemplate = "ServicesTemplate"
            });
            
            return context;
        }
        
        private void OnFlightPhaseChanged(object sender, FlightPhaseChangedEventArgs e)
        {
            var previousPhase = e.PreviousPhase;
            var newPhase = e.NewPhase;
            
            var previousContext = _currentContext;
            _currentPhase = newPhase;
            _currentContext = GetContextForPhase(newPhase);
            
            // Raise event
            OnContextChanged(previousPhase, newPhase, previousContext, _currentContext);
            
            Logger.Log(LogLevel.Information, "PhaseContextService:OnFlightPhaseChanged", 
                $"Phase context changed from {previousPhase} to {newPhase}");
        }
        
        private void OnFlightPhaseChangedEvent(FlightPhaseChangedEventArgs e)
        {
            // This method is called when the event is published through the event aggregator
            OnFlightPhaseChanged(this, e);
        }
        
        protected virtual void OnContextChanged(
            FlightPhaseIndicator.FlightPhase previousPhase,
            FlightPhaseIndicator.FlightPhase newPhase,
            PhaseContext previousContext,
            PhaseContext newContext)
        {
            var args = new PhaseContextChangedEventArgs(
                previousPhase,
                newPhase,
                previousContext,
                newContext,
                DateTime.Now);
                
            ContextChanged?.Invoke(this, args);
            
            // Also publish through event aggregator
            _eventAggregator.Publish(args);
        }
    }
}
