# Prosim2GSX Data Flow Diagram

This document describes the data flow within the Prosim2GSX application, showing how information moves between components and external systems.

## Main Data Flow Diagram

The following diagram shows the high-level data flow in Prosim2GSX:

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
    
    %% GSX Controller Facade and Coordinators
    G --> GCF[GSXControllerFacade]
    GCF --> GSO[GSXServiceOrchestrator]
    GCF --> GDC[GSXDoorCoordinator]
    GCF --> GEC[GSXEquipmentCoordinator]
    GCF --> GPC[GSXPassengerCoordinator]
    GCF --> GCC[GSXCargoCoordinator]
    GCF --> GFC[GSXFuelCoordinator]
    
    %% GSXFuelCoordinator components
    GFC --> RSM[RefuelingStateManager]
    GFC --> RPT[RefuelingProgressTracker]
    GFC --> FHM[FuelHoseConnectionMonitor]
    GFC --> RCF[RefuelingCommandFactory]
    GFC <--> PS5
```

## Key Data Flows

### 1. Configuration Flow

Configuration data flows from the user interface to the ServiceModel and then to the various components:

```mermaid
flowchart LR
    UI[User Interface] --> SM[ServiceModel]
    SM --> CF[ConfigurationFile]
    SM --> GC[GsxController]
    SM --> PC[ProsimController]
    SM --> SC[ServiceController]
    SM --> MSC[MobiSimConnect]
```

**Description**:
- User configures settings via the UI
- Settings are stored in the ServiceModel
- ServiceModel persists settings to ConfigurationFile
- Components read settings from ServiceModel
- Components adjust behavior based on settings

### 2. Flight Plan Flow

Flight plan data flows from ProsimA320 to the application and then to GSX:

```mermaid
flowchart LR
    PA[ProsimA320] --> PS[ProsimService]
    PS --> PFDS[ProsimFlightDataService]
    PFDS --> FP[FlightPlan]
    FP --> FPS[FlightPlanService]
    FPS --> GSM[GSXStateManager]
    GSM --> GSO[GSXServiceOrchestrator]
    GSO --> GSC[GSXServiceCoordinator]
    FPS --> GLM[GSXLoadsheetManager]
    GLM --> AS[AcarsService]
    AS --> AN[ACARS Network]
```

**Description**:
- Flight plan is loaded in ProsimA320
- ProsimFlightDataService detects new flight plan
- FlightPlanService parses and validates flight plan
- GSXStateManager transitions to appropriate state
- GSXServiceOrchestrator coordinates services based on flight plan
- GSXLoadsheetManager generates loadsheet
- AcarsService transmits loadsheet to ACARS network

### 3. Service Flow

Service data flows from GSX to ProSim and back:

```mermaid
flowchart LR
    GSM[GSXStateManager] --> GSO[GSXServiceOrchestrator]
    GSO --> GDC[GSXDoorCoordinator]
    GSO --> GPC[GSXPassengerCoordinator]
    GSO --> GCC[GSXCargoCoordinator]
    GSO --> GFC[GSXFuelCoordinator]
    GSO --> GEC[GSXEquipmentCoordinator]
    
    GDC <--> PDS[ProsimDoorService]
    GPC <--> PPS[ProsimPassengerService]
    GCC <--> PCS[ProsimCargoService]
    GFC <--> PFS[ProsimFuelService]
    GEC <--> PES[ProsimEquipmentService]
    
    PDS <--> PA[ProsimA320]
    PPS <--> PA
    PCS <--> PA
    PFS <--> PA
    PES <--> PA
```

**Description**:
- GSXStateManager determines current flight state
- GSXServiceOrchestrator selects appropriate services
- Coordinators manage specific service areas
- ProSim services interact with ProsimA320
- Data flows bidirectionally between GSX and ProSim

### 4. Passenger Flow

Passenger data flows between ProSim and GSX:

```mermaid
flowchart LR
    PA[ProsimA320] <--> PPS[ProsimPassengerService]
    PPS <--> GPC[GSXPassengerCoordinator]
    GPC <--> GSO[GSXServiceOrchestrator]
    GSO --> GSC[GSXServiceCoordinator]
    PPS --> GLM[GSXLoadsheetManager]
    GLM --> AS[AcarsService]
```

**Description**:
- ProsimPassengerService provides passenger data
- GSXPassengerCoordinator coordinates passenger operations
- GSXServiceOrchestrator manages service timing
- GSXLoadsheetManager generates loadsheet with passenger data
- AcarsService transmits loadsheet to ACARS network

### 5. Cargo Flow

Cargo data flows between ProSim and GSX:

```mermaid
flowchart LR
    PA[ProsimA320] <--> PCS[ProsimCargoService]
    PCS <--> GCC[GSXCargoCoordinator]
    GCC <--> GSO[GSXServiceOrchestrator]
    GSO --> GSC[GSXServiceCoordinator]
    PCS --> GLM[GSXLoadsheetManager]
    GLM --> AS[AcarsService]
```

**Description**:
- ProsimCargoService provides cargo data
- GSXCargoCoordinator coordinates cargo operations
- GSXServiceOrchestrator manages service timing
- GSXLoadsheetManager includes cargo data in loadsheet
- AcarsService transmits loadsheet to ACARS network

### 6. Fuel Flow

Fuel data flows between ProSim and GSX:

```mermaid
flowchart LR
    PA[ProsimA320] <--> PFS[ProsimFuelService]
    PFS <--> GFC[GSXFuelCoordinator]
    GFC --> RSM[RefuelingStateManager]
    GFC --> RPT[RefuelingProgressTracker]
    GFC --> FHM[FuelHoseConnectionMonitor]
    GFC --> RCF[RefuelingCommandFactory]
    RCF --> SRC[StartRefuelingCommand]
    RCF --> STRC[StopRefuelingCommand]
    RCF --> UFC[UpdateFuelAmountCommand]
    GFC <--> GSO[GSXServiceOrchestrator]
    GSO --> GSC[GSXServiceCoordinator]
    PFS --> GLM[GSXLoadsheetManager]
    GLM --> AS[AcarsService]
```

**Description**:
- ProsimFuelService provides fuel data
- GSXFuelCoordinator coordinates fuel operations
- RefuelingStateManager tracks refueling state
- RefuelingProgressTracker monitors refueling progress
- FuelHoseConnectionMonitor detects fuel hose connections
- RefuelingCommandFactory creates commands for fuel operations
- GSXServiceOrchestrator manages service timing
- GSXLoadsheetManager includes fuel data in loadsheet
- AcarsService transmits loadsheet to ACARS network

### 7. Door Flow

Door data flows between ProSim and GSX:

```mermaid
flowchart LR
    PA[ProsimA320] <--> PDS[ProsimDoorService]
    PDS <--> GDC[GSXDoorCoordinator]
    GDC <--> GDM[GSXDoorManager]
    GDC <--> GSO[GSXServiceOrchestrator]
    GSO --> GSC[GSXServiceCoordinator]
```

**Description**:
- ProsimDoorService provides door state information
- GSXDoorCoordinator coordinates door operations
- GSXDoorManager controls doors based on service needs
- GSXServiceOrchestrator monitors door toggle LVARs
- GSXServiceCoordinator manages service timing

### 8. Audio Control Flow

Audio control data flows from ProSim to GSX:

```mermaid
flowchart LR
    PA[ProsimA320] --> PS[ProsimService]
    PS --> PC[ProsimController]
    PC --> GAS[GSXAudioService]
    GAS --> CASM[CoreAudioSessionManager]
    CASM --> WA[Windows Audio]
```

**Description**:
- ProsimController detects changes in cockpit controls
- GSXAudioService adjusts audio levels
- CoreAudioSessionManager interacts with Windows Audio
- Audio levels are adjusted for GSX and other applications

## Data Transformation

### Flight Plan Data

```mermaid
flowchart LR
    RFP[Raw Flight Plan] --> FPS[FlightPlanService]
    FPS --> PFP[Parsed Flight Plan]
    PFP --> SM[ServiceModel]
    SM --> GSM[GSXStateManager]
    SM --> GSO[GSXServiceOrchestrator]
    SM --> GLM[GSXLoadsheetManager]
```

**Transformations**:
1. Raw flight plan data from ProsimA320
2. Parsed and validated flight plan data
3. Flight plan data stored in ServiceModel
4. Flight plan data used for state transitions
5. Flight plan data used for service coordination
6. Flight plan data used for loadsheet generation

### Passenger Data

```mermaid
flowchart LR
    RPD[Raw Passenger Data] --> PPS[ProsimPassengerService]
    PPS --> PPD[Processed Passenger Data]
    PPD --> GPC[GSXPassengerCoordinator]
    PPD --> GLM[GSXLoadsheetManager]
    GLM --> LS[Loadsheet]
    LS --> AS[AcarsService]
    AS --> AM[ACARS Message]
```

**Transformations**:
1. Raw passenger data from ProsimA320
2. Processed passenger data with counts and status
3. Passenger data used for boarding/deboarding coordination
4. Passenger data included in loadsheet
5. Loadsheet formatted for ACARS transmission
6. ACARS message sent to network

### Fuel Data

```mermaid
flowchart LR
    RFD[Raw Fuel Data] --> PFS[ProsimFuelService]
    PFS --> PFD[Processed Fuel Data]
    PFD --> GFC[GSXFuelCoordinator]
    GFC --> RSM[RefuelingStateManager]
    GFC --> RPT[RefuelingProgressTracker]
    PFD --> GLM[GSXLoadsheetManager]
    GLM --> LS[Loadsheet]
```

**Transformations**:
1. Raw fuel data from ProsimA320
2. Processed fuel data with quantities and status
3. Fuel data used for refueling coordination
4. Fuel state tracked by RefuelingStateManager
5. Refueling progress tracked by RefuelingProgressTracker
6. Fuel data included in loadsheet

## Data Storage

### Persistent Storage

1. **Configuration File**
   - Format: XML
   - Location: Application directory
   - Content: User settings and preferences
   - Access: Read/write by ConfigurationFile class

2. **State File**
   - Format: JSON
   - Location: Application directory
   - Content: Current state and state history
   - Access: Read/write by GSXStateManager

### In-Memory Storage

1. **ServiceModel**
   - Content: Application configuration and state
   - Access: Read/write by all components
   - Lifetime: Application session

2. **State History**
   - Content: History of state transitions
   - Access: Read/write by GSXStateManager
   - Lifetime: Application session (with optional persistence)

3. **Flight Plan Data**
   - Content: Current flight plan information
   - Access: Read by various components
   - Lifetime: Until new flight plan is loaded

## Event-Based Communication

The application uses event-based communication for data flow between components:

```mermaid
flowchart LR
    PS[ProsimService] -- "ConnectionStateChanged" --> PC[ProsimController]
    PDS[ProsimDoorService] -- "DoorStateChanged" --> GDC[GSXDoorCoordinator]
    PPS[ProsimPassengerService] -- "PassengerStateChanged" --> GPC[GSXPassengerCoordinator]
    PCS[ProsimCargoService] -- "CargoStateChanged" --> GCC[GSXCargoCoordinator]
    PFS[ProsimFuelService] -- "FuelStateChanged" --> GFC[GSXFuelCoordinator]
    
    GSM[GSXStateManager] -- "StateChanged" --> GSO[GSXServiceOrchestrator]
    GSO -- "ServiceStatusChanged" --> GCF[GSXControllerFacade]
    GFC -- "RefuelingProgressChanged" --> GCF
    
    EA[EventAggregator] -- "Publish/Subscribe" --> ALL[All Components]
```

**Event Types**:
1. **State Events**: Notify of state changes in the system
2. **Service Events**: Notify of service status changes
3. **Data Events**: Notify of data changes (fuel, passengers, cargo, etc.)
4. **Connection Events**: Notify of connection status changes
5. **Progress Events**: Notify of progress updates for long-running operations

## Circular Dependency Resolution

The application uses a specific pattern to resolve circular dependencies in data flow:

```mermaid
sequenceDiagram
    participant SC as ServiceController
    participant CC as GSXCargoCoordinator
    participant SO as GSXServiceOrchestrator
    
    SC->>CC: new GSXCargoCoordinator(prosimCargoService, null, logger)
    Note right of CC: Created with null orchestrator
    SC->>SO: new GSXServiceOrchestrator(..., cargoCoordinator, ...)
    Note right of SO: Created with reference to coordinator
    SC->>CC: cargoCoordinator.SetServiceOrchestrator(serviceOrchestrator)
    Note right of CC: Circular dependency resolved
```

**Resolution Pattern**:
1. Create first component with null dependency
2. Create second component with reference to first component
3. Set the circular dependency on the first component
4. Both components now have references to each other
