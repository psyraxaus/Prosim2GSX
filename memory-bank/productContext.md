# Product Context: Prosim2GSX

## Why This Project Exists

Prosim2GSX exists to bridge the gap between two powerful but separate flight simulation tools:

1. **ProsimA320** - A high-fidelity Airbus A320 cockpit simulation suite that provides realistic aircraft systems and flight dynamics.
2. **GSX Pro** - A ground services extension for Microsoft Flight Simulator 2020 that provides realistic ground operations including boarding, deboarding, refueling, and other airport services.

Without integration, users must manually coordinate between these two environments, switching contexts and managing inconsistencies in passenger counts, fuel levels, and other operational parameters. This creates a disjointed experience that breaks immersion and adds unnecessary complexity to the simulation.

## Problems It Solves

### 1. Fragmented Simulation Experience

- **Before**: Users must manually coordinate between ProsimA320 and GSX, switching between interfaces and managing inconsistencies.
- **After**: Seamless integration allows users to stay within the ProsimA320 environment while GSX services are automatically coordinated.

### 2. Data Synchronization Challenges

- **Before**: Manual synchronization of passenger counts, cargo loads, and fuel levels between systems leads to inconsistencies.
- **After**: Automatic synchronization ensures that both systems operate with the same data, maintaining simulation integrity.

### 3. Workflow Disruption

- **Before**: Users must interrupt their workflow to manage ground services, breaking immersion.
- **After**: Ground services are automatically triggered at appropriate times based on flight phase, maintaining immersion.

### 4. Complex Configuration

- **Before**: Users must configure both systems separately and ensure compatibility.
- **After**: Centralized configuration through Prosim2GSX simplifies setup and ensures compatibility.

### 5. Audio Management

- **Before**: Users must manually adjust audio levels for GSX and ATC applications.
- **After**: Audio levels can be controlled directly from the cockpit controls, enhancing immersion.

## How It Should Work

### User Experience Goals

1. **Transparency**: The integration should be nearly invisible to the user, with services happening automatically at appropriate times.
2. **Configurability**: Users should be able to customize which services are automated and how they behave.
3. **Reliability**: The integration should work consistently without requiring user intervention.
4. **Immersion**: The integration should enhance the simulation experience by removing technical distractions.

### Workflow Integration

#### Pre-Flight Phase
1. User starts ProsimA320 and MSFS2020
2. Prosim2GSX automatically connects to both systems
3. When the user loads a flight plan in ProsimA320, Prosim2GSX:
   - Synchronizes the flight plan with GSX
   - Automatically positions the aircraft at the gate if configured
   - Connects jetway/stairs if configured
   - Places ground equipment (GPU, chocks, PCA) as appropriate

#### Departure Phase
1. When the flight plan is loaded, Prosim2GSX:
   - Calls refueling service to match the planned fuel
   - Calls catering service if configured
   - Calls boarding service after refueling and catering are complete
2. When boarding and refueling are complete:
   - Sends final loadsheet to ProsimA320
   - Prepares for pushback
3. When the user sets parking brake, disconnects external power, and turns on beacon:
   - Removes all ground equipment
   - Disconnects jetway/stairs if still connected

#### Flight Phase
1. Prosim2GSX monitors flight state
2. Minimal interaction during this phase

#### Arrival Phase
1. When the aircraft is on the ground with engines off and parking brake set:
   - Connects jetway/stairs if configured
2. When the beacon is turned off:
   - Places ground equipment (GPU, PCA, chocks)
   - Calls deboarding service if configured
3. For turnarounds, the cycle restarts when a new flight plan is loaded

### Audio Control Integration

1. GSX audio volume is controlled via the INT knob in the cockpit
2. ATC application volume is controlled via the VHF1 knob in the cockpit
3. Audio settings are automatically reset at the end of a session

## Target Users

1. **Flight Simulation Enthusiasts**: Users who want a high-fidelity, immersive Airbus A320 simulation experience.
2. **Virtual Airlines Pilots**: Users who fly for virtual airlines and need realistic ground operations.
3. **Home Cockpit Builders**: Users who have built physical cockpit setups and want integrated ground services.
4. **Training Environment Users**: Users who use the simulation for procedural training and want realistic ground operations.

## Value Proposition

Prosim2GSX transforms the flight simulation experience by seamlessly integrating ProsimA320 and GSX Pro, allowing users to focus on flying rather than managing separate systems. It enhances immersion, improves workflow, and ensures data consistency across platforms.
