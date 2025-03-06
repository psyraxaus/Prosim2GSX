# System Patterns: Prosim2GSX

## Architecture Overview

Prosim2GSX follows a modular architecture with clear separation of concerns. The application is structured around several key components that work together to provide seamless integration between ProsimA320 and GSX in Microsoft Flight Simulator 2020.

```mermaid
graph TD
    A[Main Application] --> B[IPCManager]
    A --> C[ServiceController]
    A --> D[NotifyIconViewModel]
    B --> E[MobiSimConnect]
    B --> F[GsxController]
    B --> G[ProsimController]
    F --> H[AcarsService]
    G --> I[ProsimInterface]
    G --> J[FlightPlan]
    C --> K[ServiceModel]
    F --> K
    G --> K
    E --> L[SimConnectService]
    I --> M[ProsimService]
    F --> N[GSXStateManager]
    F --> O[GSXAudioService]
    F --> P[GSXServiceCoordinator]
    F --> Q[GSXDoorManager]
    F --> R[GSXLoadsheetManager]
    F --> S[GSXMenuService]
    G --> T[ProsimDoorService]
    G --> U[ProsimEquipmentService]
    G --> V[ProsimPassengerService]
    G --> W[ProsimCargoService]
    G --> X[ProsimFuelService]
    G --> Y[ProsimFlightDataService]
    G --> Z[ProsimFluidService]
    J --> AA[FlightPlanService]
```

## Component Relationships

### Core Components

1. **Main Application (MainWindow)**
   - Entry point for the application
   - Manages the UI and system tray icon
   - Coordinates between controllers and view models

2. **IPCManager**
   - Central hub for inter-process communication
   - Manages connections to external systems (SimConnect, ProSim)
   - Provides access to controllers for other components

3. **ServiceController**
   - Manages the application's service lifecycle
   - Coordinates startup and shutdown sequences
   - Monitors system state and handles reconnection

4. **ServiceModel**
   - Stores application configuration and state
   - Provides settings for all components
   - Persists configuration through ConfigurationFile

### Integration Components

1. **MobiSimConnect**
   - Interfaces with Microsoft Flight Simulator via SimConnect
   - Subscribes to simulator variables and events
   - Provides methods to read/write simulator data

2. **GsxController**
   - Manages interaction with GSX in MSFS2020
   - Controls GSX services (boarding, deboarding, refueling, etc.)
   - Handles flight state transitions and service timing
   - Manages audio control for GSX and other applications

3. **ProsimController**
   - Interfaces with ProsimA320
   - Synchronizes flight plan data
   - Manages passenger, cargo, and fuel data
   - Controls ground equipment in ProSim

4. **ProsimInterface**
   - Low-level interface to ProSim SDK
   - Handles data conversion between systems
   - Provides abstraction for ProSim API calls

5. **FlightPlan**
   - Manages flight plan data
   - Loads and parses flight plans from ProsimA320
   - Provides structured access to flight information

6. **AcarsService**
   - Handles ACARS communication
   - Sends and receives messages via ACARS networks
   - Formats loadsheets and other flight information

7. **GSXStateManager**
   - Manages flight state transitions
   - Provides state query methods
   - Raises events when state changes
   - Centralizes state management logic

8. **GSXAudioService**
   - Controls audio for GSX and other applications
   - Adjusts volume based on cockpit controls
   - Provides audio reset functionality
   - Manages audio device detection and control

9. **GSXServiceCoordinator**
   - Coordinates GSX services (boarding, refueling, etc.)
   - Manages service timing and sequencing
   - Raises events for service status changes
   - Centralizes service operation logic

10. **GSXDoorManager**
    - Manages aircraft door operations
    - Controls door opening/closing based on service needs
    - Raises events for door state changes
    - Handles door toggle requests from GSX

11. **GSXLoadsheetManager**
    - Generates and sends loadsheets
    - Formats loadsheet data for ACARS transmission
    - Calculates weight and balance information
    - Raises events when loadsheets are generated

12. **GSXMenuService**
    - Interacts with GSX menu system
    - Selects menu items and operators
    - Manages menu navigation
    - Provides abstraction for GSX menu interaction

13. **ProsimDoorService**
    - Controls aircraft doors in ProSim
    - Provides door state information
    - Raises events for door state changes
    - Centralizes door management logic

14. **ProsimEquipmentService**
    - Manages ground equipment in ProSim
    - Controls GPU, PCA, and chocks
    - Provides equipment state information
    - Raises events for equipment state changes

15. **ProsimPassengerService**
    - Manages passenger data in ProSim
    - Controls boarding and deboarding
    - Provides passenger count information
    - Raises events for passenger state changes

16. **ProsimCargoService**
    - Manages cargo data in ProSim
    - Controls cargo loading and unloading
    - Provides cargo weight information
    - Raises events for cargo state changes

17. **ProsimFuelService**
    - Manages fuel data in ProSim
    - Controls refueling operations
    - Provides fuel quantity information
    - Raises events for fuel state changes

18. **ProsimFlightDataService**
    - Manages flight data in ProSim
    - Provides access to flight parameters
    - Formats flight data for other components
    - Raises events for flight data changes

19. **ProsimFluidService**
    - Manages hydraulic fluid data in ProSim
    - Controls fluid levels and servicing
    - Provides fluid quantity information
    - Raises events for fluid state changes

20. **FlightPlanService**
    - Loads and parses flight plans
    - Provides structured access to flight plan data
    - Handles flight plan file operations
    - Raises events when flight plans change

### UI Components

1. **NotifyIconViewModel**
   - Manages the system tray icon and context menu
   - Provides commands for user interaction
   - Controls application visibility

## State Management

The application uses a state machine pattern to manage the flight lifecycle:

```mermaid
stateDiagram-v2
    [*] --> PREFLIGHT
    PREFLIGHT --> DEPARTURE: Flight Plan Loaded
    DEPARTURE --> TAXIOUT: Equipment Removed
    TAXIOUT --> FLIGHT: Aircraft Airborne
    FLIGHT --> TAXIIN: Aircraft on Ground
    TAXIIN --> ARRIVAL: Engines Off & Parking Brake Set
    ARRIVAL --> TURNAROUND: Deboarding Complete
    TURNAROUND --> DEPARTURE: New Flight Plan Loaded
```

Each state has specific behaviors and triggers specific actions:

1. **PREFLIGHT**
   - Connect to ProsimA320 and MSFS
   - Position aircraft at gate (if configured)
   - Connect jetway/stairs (if configured)
   - Place ground equipment

2. **DEPARTURE**
   - Call refueling service
   - Call catering service
   - Call boarding service
   - Send final loadsheet
   - Remove ground equipment

3. **TAXIOUT**
   - Monitor aircraft state
   - Prepare for takeoff

4. **FLIGHT**
   - Minimal interaction
   - Monitor flight progress

5. **TAXIIN**
   - Monitor aircraft state
   - Prepare for arrival services

6. **ARRIVAL**
   - Connect jetway/stairs
   - Place ground equipment
   - Call deboarding service

7. **TURNAROUND**
   - Wait for new flight plan
   - Reset for next departure

## Design Patterns

### 1. Model-View-ViewModel (MVVM)
The UI follows the MVVM pattern:
- **Model**: ServiceModel and other data classes
- **View**: MainWindow and NotifyIconResources
- **ViewModel**: NotifyIconViewModel

### 2. Dependency Injection
Components receive their dependencies through constructors, promoting loose coupling and testability:
- Services are injected into controllers
- Controllers are injected into managers
- All dependencies are explicit and can be mocked for testing

### 3. Observer Pattern
The application uses event-based communication for state changes:
- SimConnect subscribes to simulator variables
- Controllers observe model changes
- UI updates based on property change notifications
- Services raise events for state changes
- Components subscribe to events from other components

### 4. State Machine
The GSXStateManager implements a state machine to manage the flight lifecycle:
- Clear states representing flight phases
- Well-defined transitions between states
- State-specific behaviors
- Event-based notification of state changes

### 5. Facade Pattern
The GsxController acts as a facade for the GSX subsystem:
- Provides a simplified interface to complex subsystems
- Delegates to specialized services
- Coordinates between services
- Hides implementation details from clients

### 6. Strategy Pattern
The application uses the strategy pattern for service operations:
- Different strategies for different flight phases
- Interchangeable service implementations
- Common interfaces for similar operations
- Runtime selection of appropriate strategy

### 7. Command Pattern
The GSXMenuService implements the command pattern for menu interactions:
- Encapsulates menu operations as objects
- Decouples menu selection from execution
- Allows for parameterization of menu operations
- Supports undo/redo operations

### 8. Adapter Pattern
The ProsimInterface and MobiSimConnect classes act as adapters:
- Convert external API calls to internal format
- Provide consistent interface to different subsystems
- Hide implementation details of external systems
- Allow for easy replacement of external dependencies

### 9. Repository Pattern
The FlightPlanService implements the repository pattern:
- Centralizes data access logic
- Provides abstraction over data sources
- Handles data persistence and retrieval
- Encapsulates data access operations

### 10. Service Locator Pattern
The IPCManager acts as a service locator:
- Provides access to services throughout the application
- Centralizes service registration and retrieval
- Decouples service consumers from service providers
- Simplifies service discovery

## Data Flow

```mermaid
flowchart LR
    A[ProsimA320] <--> B[ProsimService]
    B <--> C[ProsimController]
    D[MSFS2020] <--> E[SimConnectService]
    E <--> F[MobiSimConnect]
    F <--> G[GsxController]
    
    C --> PS1[ProsimDoorService]
    C --> PS2[ProsimEquipmentService]
    C --> PS3[ProsimPassengerService]
    C --> PS4[ProsimCargoService]
    C --> PS5[ProsimFuelService]
    C --> PS6[ProsimFlightDataService]
    C --> PS7[ProsimFluidService]
    
    G --> GS1[GSXStateManager]
    G --> GS2[GSXAudioService]
    G --> GS3[GSXServiceCoordinator]
    G --> GS4[GSXDoorManager]
    G --> GS5[GSXLoadsheetManager]
    G --> GS6[GSXMenuService]
    
    H[FlightPlan] <--> FPS[FlightPlanService]
    FPS <--> PS6
    
    GS5 <--> AC[AcarsService] <--> ACN[ACARS Network]
    
    SM[ServiceModel] <--> G
    SM <--> C
    SM <--> UI[User Interface]
```

### Key Data Flows

1. **Configuration Flow**:
   - User configures settings via UI
   - Settings stored in ServiceModel
   - ServiceModel provides configuration to all components
   - Components read settings and adjust behavior accordingly

2. **Flight Plan Flow**:
   - Flight plan loaded in ProsimA320
   - ProsimFlightDataService detects new flight plan
   - FlightPlanService parses and validates flight plan
   - FlightPlanService raises event for new flight plan
   - GSXStateManager transitions to appropriate state
   - GSXServiceCoordinator uses flight plan data to coordinate services

3. **Service Flow**:
   - GSXStateManager determines current flight state
   - GSXServiceCoordinator selects appropriate services based on state
   - GSXServiceCoordinator calls service methods on GSX
   - GSXServiceCoordinator raises events for service status changes
   - GsxController coordinates between GSX services and ProSim services

4. **Passenger Flow**:
   - ProsimPassengerService provides passenger data
   - GSXServiceCoordinator uses passenger data for boarding/deboarding
   - GSXLoadsheetManager generates loadsheet with passenger data
   - AcarsService transmits loadsheet to ACARS network

5. **Cargo Flow**:
   - ProsimCargoService provides cargo data
   - GSXServiceCoordinator uses cargo data for loading/unloading
   - GSXLoadsheetManager includes cargo data in loadsheet
   - AcarsService transmits loadsheet to ACARS network

6. **Fuel Flow**:
   - ProsimFuelService provides fuel data
   - GSXServiceCoordinator uses fuel data for refueling
   - GSXLoadsheetManager includes fuel data in loadsheet
   - AcarsService transmits loadsheet to ACARS network

7. **Door Flow**:
   - ProsimDoorService provides door state information
   - GSXDoorManager controls doors based on service needs
   - GSXServiceCoordinator coordinates door operations with services
   - GSXDoorManager raises events for door state changes

8. **Audio Control Flow**:
   - ProsimController detects changes in cockpit controls
   - GSXAudioService adjusts audio levels for GSX and other applications
   - GSXAudioService provides audio reset functionality
   - GSXAudioService raises events for audio state changes

## Technical Decisions

### 1. Modular Architecture
- Separation of concerns through specialized services
- Interface-based design for testability
- Event-based communication for loose coupling
- Dependency injection for flexible component composition
- Reduced complexity in individual components

### 2. SimConnect for MSFS Integration
- Provides stable API for interacting with MSFS
- Allows reading/writing simulator variables
- Enables subscription to simulator events
- Abstracted through SimConnectService for testability

### 3. ProSim SDK for ProsimA320 Integration
- Official SDK provides reliable access to ProsimA320
- Allows reading/writing ProSim variables
- Enables monitoring of ProSim events
- Abstracted through ProsimService for testability

### 4. System Tray Application
- Minimizes UI footprint
- Allows application to run in background
- Provides easy access to configuration
- Uses MVVM pattern for separation of UI and logic

### 5. State-Based Service Management
- Clear state transitions based on flight phase
- Predictable behavior for service calls
- Easier to debug and maintain
- Centralized in GSXStateManager for consistency

### 6. Configurable Automation
- All automation features can be enabled/disabled
- Allows users to customize their experience
- Accommodates different workflows and preferences
- Configuration stored in ServiceModel for centralized access

### 7. Event-Based Communication
- Components communicate through events
- Reduces direct dependencies between components
- Allows for flexible component composition
- Simplifies adding new features and services

### 8. Interface-Based Design
- All services implement interfaces
- Enables mock implementations for testing
- Allows for alternative implementations
- Facilitates future platform extensions

### 9. Secure XML Processing
- XML processing uses secure settings
- DtdProcessing.Prohibit for security
- Null XmlResolver to prevent external entity resolution
- Proper indentation and formatting for XML output

### 10. Comprehensive Logging
- Serilog used for structured logging
- Appropriate log levels based on context
- Relevant context included in log messages
- Centralized logging configuration
