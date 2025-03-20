# Product Context: Prosim2GSX

## Problem Statement
Flight simulation enthusiasts using Prosim A320 and GSX Pro face several challenges:

1. **Manual Synchronization**: Without integration, users must manually ensure that fuel levels, passenger counts, and cargo weights match between Prosim and GSX.
2. **Workflow Disruption**: Switching between different interfaces disrupts the immersive simulation experience.
3. **Inconsistent States**: Discrepancies between the two systems can lead to unrealistic situations (e.g., GSX shows refueling complete but Prosim shows tanks still empty).
4. **Complex Ground Operations**: Managing ground services requires attention to multiple systems simultaneously.
5. **Audio Management**: Controlling simulation audio requires leaving the cockpit environment.

## Solution Overview
Prosim2GSX bridges these systems by:

1. **Automatic Synchronization**: Ensuring that changes in one system are reflected in the other.
2. **Workflow Integration**: Allowing pilots to manage ground services directly from the Prosim cockpit.
3. **State Consistency**: Maintaining a single source of truth for the simulation state.
4. **Simplified Operations**: Automating routine ground service tasks.
5. **Cockpit-Centric Control**: Providing audio controls through existing cockpit interfaces.

## User Experience Goals

### Seamlessness
- Users should feel like they're interacting with a single, unified system
- Transitions between flight phases should happen naturally without manual intervention
- The tool should "disappear" during normal operation, only requiring attention for exceptions

### Realism
- Ground operations should follow realistic procedures and timelines
- Service behaviors should match real-world expectations
- Audio cues should enhance immersion rather than break it

### Control
- Users should have configuration options to determine automation levels
- Critical decisions (like pushback direction) remain under user control
- The system should be predictable and consistent in its behavior

### Reliability
- Services should execute consistently without requiring restarts or workarounds
- The tool should gracefully handle edge cases and unexpected situations
- Users should be able to trust the automation to work correctly

## User Workflow

### Pre-Flight
1. User starts MSFS and Prosim2GSX
2. Tool automatically positions aircraft and connects jetway/stairs if configured
3. User powers up the aircraft and imports flight plan via EFB
4. Tool automatically calls for refueling and catering based on configuration
5. Boarding process is synchronized between GSX and Prosim
6. Final loadsheet is delivered when services are complete

### Departure
1. User prepares for departure
2. When parking brake is set, external power disconnected, and beacon light on
3. Tool automatically removes ground equipment and jetway/stairs
4. User proceeds with normal departure procedures

### Arrival
1. User selects arrival gate
2. After engines off and parking brake set, jetway/stairs connect automatically
3. Ground equipment (GPU, PCA, chocks) is placed automatically
4. Deboarding process begins automatically if configured
5. System is ready for turnaround when deboarding completes

## Key Interactions

### Configuration Interface
- System tray icon provides access to configuration
- Settings are persistent between sessions
- All options have tooltips for explanation
- Settings can be changed dynamically during operation

### Audio Control
- INT-Knob controls GSX audio volume
- VHF1-Knob controls ATC application volume
- Audio control activates when aircraft is powered

### Service Flow
- Automatic service calls based on configuration
- Synchronization of passenger, cargo, and fuel data
- Automatic ground equipment management
- Jetway/stairs operation based on flight phase

## Success Indicators
- Users can complete full flights without manual synchronization
- Ground operations proceed realistically without intervention
- Configuration remains stable between sessions
- Users report enhanced immersion and reduced workload
