# Prosim2GSX Modularization Architecture - Phase 4

## Architecture Overview

This document provides a visual representation of the proposed architecture for Phase 4 of the Prosim2GSX modularization strategy. The diagrams illustrate the relationships between the different components and how they interact with each other.

## Component Diagram

```mermaid
graph TD
    A[ServiceController] --> B[GSXControllerFacade]
    B --> C[GSXStateMachine]
    B --> D[GSXServiceOrchestrator]
    B --> E[GSXDoorCoordinator]
    B --> F[GSXEquipmentCoordinator]
    B --> G[GSXPassengerCoordinator]
    B --> H[GSXCargoCoordinator]
    B --> I[GSXFuelCoordinator]
    B --> J[GSXAudioService]
    B --> K[GSXMenuService]
    B --> L[GSXLoadsheetManager]
    
    C -.-> |State Events| B
    D -.-> |Service Events| B
    E -.-> |Door Events| B
    F -.-> |Equipment Events| B
    G -.-> |Passenger Events| B
    H -.-> |Cargo Events| B
    I -.-> |Fuel Events| B
    J -.-> |Audio Events| B
    K -.-> |Menu Events| B
    L -.-> |Loadsheet Events| B
    
    D --> C
    E --> C
    F --> C
    G --> C
    H --> C
    I --> C
    
    E --> D
    F --> D
    G --> D
    H --> D
    I --> D
    
    G --> E
    H --> E
    
    M[SimConnectService] --> B
    N[ProsimService] --> B
    O[AcarsService] --> B
    P[FlightPlanService] --> B
```

## Interaction Diagram

```mermaid
sequenceDiagram
    participant SC as ServiceController
    participant GCF as GSXControllerFacade
    participant GSM as GSXStateMachine
    participant GSO as GSXServiceOrchestrator
    participant GDC as GSXDoorCoordinator
    participant GEC as GSXEquipmentCoordinator
    participant GPC as GSXPassengerCoordinator
    participant GCC as GSXCargoCoordinator
    participant GFC as GSXFuelCoordinator
    participant GAS as GSXAudioService
    participant GMS as GSXMenuService
    participant GLM as GSXLoadsheetManager
    
    SC->>GCF: Initialize
    GCF->>GSM: Initialize
    GCF->>GSO: Initialize
    GCF->>GDC: Initialize
    GCF->>GEC: Initialize
    GCF->>GPC: Initialize
    GCF->>GCC: Initialize
    GCF->>GFC: Initialize
    GCF->>GAS: Initialize
    GCF->>GMS: Initialize
    GCF->>GLM: Initialize
    
    SC->>GCF: RunServices
    GCF->>GSM: GetCurrentState
    GSM-->>GCF: CurrentState
    
    GCF->>GSO: CoordinateServices(CurrentState)
    GSO->>GSM: ValidateStateTransition
    GSM-->>GSO: TransitionValid
    
    GSO->>GDC: ManageDoors(CurrentState)
    GDC-->>GSO: DoorsManaged
    
    GSO->>GEC: ManageEquipment(CurrentState)
    GEC-->>GSO: EquipmentManaged
    
    GSO->>GPC: ManagePassengers(CurrentState)
    GPC->>GDC: CheckDoorStatus
    GDC-->>GPC: DoorStatus
    GPC-->>GSO: PassengersManaged
    
    GSO->>GCC: ManageCargo(CurrentState)
    GCC->>GDC: CheckDoorStatus
    GDC-->>GCC: DoorStatus
    GCC-->>GSO: CargoManaged
    
    GSO->>GFC: ManageFuel(CurrentState)
    GFC-->>GSO: FuelManaged
    
    GSO->>GAS: ManageAudio(CurrentState)
    GAS-->>GSO: AudioManaged
    
    GSO->>GMS: ManageMenus(CurrentState)
    GMS-->>GSO: MenusManaged
    
    GSO->>GLM: ManageLoadsheets(CurrentState)
    GLM-->>GSO: LoadsheetsManaged
    
    GSO-->>GCF: ServicesCoordinated
    
    GCF->>GSM: UpdateState
    GSM-->>GCF: StateUpdated
    
    GCF-->>SC: ServicesCompleted
```

## State Transition Diagram

```mermaid
stateDiagram-v2
    [*] --> PREFLIGHT
    
    PREFLIGHT --> DEPARTURE: FlightPlanLoaded
    DEPARTURE --> TAXIOUT: EquipmentRemoved
    TAXIOUT --> FLIGHT: AircraftAirborne
    FLIGHT --> TAXIIN: AircraftOnGround
    TAXIIN --> ARRIVAL: EnginesOff & ParkingBrakeSet
    ARRIVAL --> TURNAROUND: DeboardingComplete
    TURNAROUND --> DEPARTURE: NewFlightPlanLoaded
    
    state PREFLIGHT {
        [*] --> ConnectingToSystems
        ConnectingToSystems --> PositioningAircraft
        PositioningAircraft --> ConnectingJetwayStairs
        ConnectingJetwayStairs --> PlacingGroundEquipment
        PlacingGroundEquipment --> WaitingForFlightPlan
        WaitingForFlightPlan --> [*]
    }
    
    state DEPARTURE {
        [*] --> InitiatingRefueling
        InitiatingRefueling --> InitiatingCatering
        InitiatingCatering --> InitiatingBoarding
        InitiatingBoarding --> MonitoringRefueling
        MonitoringRefueling --> MonitoringCatering
        MonitoringCatering --> MonitoringBoarding
        MonitoringBoarding --> SendingFinalLoadsheet
        SendingFinalLoadsheet --> RemovingGroundEquipment
        RemovingGroundEquipment --> [*]
    }
    
    state ARRIVAL {
        [*] --> ConnectingJetwayStairs
        ConnectingJetwayStairs --> PlacingGroundEquipment
        PlacingGroundEquipment --> InitiatingDeboarding
        InitiatingDeboarding --> MonitoringDeboarding
        MonitoringDeboarding --> [*]
    }
```

## Component Responsibilities

### GSXControllerFacade
- Initializes and manages all GSX services
- Delegates operations to appropriate services
- Handles high-level error recovery
- Provides a simplified interface to the rest of the application

### GSXStateMachine
- Manages flight state transitions
- Enforces valid state transitions
- Notifies other components of state changes
- Provides state-specific behavior

### GSXServiceOrchestrator
- Coordinates service execution based on current state
- Manages service timing and sequencing
- Handles service dependencies
- Provides feedback on service execution

### GSXDoorCoordinator
- Manages aircraft door operations
- Coordinates door operations with services
- Handles door state tracking
- Provides door-related events

### GSXEquipmentCoordinator
- Manages ground equipment operations
- Coordinates equipment operations with services
- Handles equipment state tracking
- Provides equipment-related events

### GSXPassengerCoordinator
- Manages passenger boarding and deboarding
- Coordinates passenger operations with services
- Handles passenger count tracking
- Provides passenger-related events

### GSXCargoCoordinator
- Manages cargo loading and unloading
- Coordinates cargo operations with services
- Handles cargo state tracking
- Provides cargo-related events

### GSXFuelCoordinator
- Manages refueling operations
- Coordinates fuel operations with services
- Handles fuel state tracking
- Provides fuel-related events

### GSXAudioService
- Controls audio for GSX and other applications
- Adjusts volume based on cockpit controls
- Provides audio reset functionality
- Manages audio device detection and control

### GSXMenuService
- Interacts with GSX menu system
- Selects menu items and operators
- Manages menu navigation
- Provides abstraction for GSX menu interaction

### GSXLoadsheetManager
- Generates and sends loadsheets
- Formats loadsheet data for ACARS transmission
- Calculates weight and balance information
- Provides loadsheet-related events

## Data Flow

```mermaid
flowchart LR
    A[ProsimA320] <--> B[ProsimService]
    B <--> C[GSXControllerFacade]
    D[MSFS2020] <--> E[SimConnectService]
    E <--> C
    
    C --> F[GSXStateMachine]
    F --> C
    
    C --> G[GSXServiceOrchestrator]
    G --> C
    
    G --> H[GSXDoorCoordinator]
    H --> G
    
    G --> I[GSXEquipmentCoordinator]
    I --> G
    
    G --> J[GSXPassengerCoordinator]
    J --> G
    
    G --> K[GSXCargoCoordinator]
    K --> G
    
    G --> L[GSXFuelCoordinator]
    L --> G
    
    C --> M[GSXAudioService]
    M --> C
    
    C --> N[GSXMenuService]
    N --> C
    
    C --> O[GSXLoadsheetManager]
    O --> C
    
    O <--> P[AcarsService]
    P <--> Q[ACARS Network]
    
    R[ServiceModel] <--> C
    R <--> S[User Interface]
```

## Benefits of the New Architecture

1. **Improved Separation of Concerns**
   - Each component has a single responsibility
   - Components are focused on specific aspects of the system
   - GsxController is replaced with a thin facade

2. **Enhanced Testability**
   - Components can be tested in isolation
   - Dependencies are explicit and can be mocked
   - Unit tests can be written for each component

3. **Better Maintainability**
   - Changes to one component don't affect other components
   - New features can be added without modifying existing code
   - Code is more modular and easier to maintain

4. **Event-Based Communication**
   - Components communicate through events
   - Reduces tight coupling between components
   - Makes the system more extensible

5. **Clearer Responsibility Boundaries**
   - Each component has a clear responsibility
   - GSXControllerFacade orchestrates the components
   - Components don't need to know about each other

## Conclusion

The proposed architecture for Phase 4 of the Prosim2GSX modularization strategy provides a clear visualization of the relationships between the different components and how they interact with each other. This architecture will significantly improve the codebase's maintainability, testability, and extensibility by breaking down the GsxController into smaller, more focused components that follow the Single Responsibility Principle.
