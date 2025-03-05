# Project Brief: Prosim2GSX

## Project Overview

Prosim2GSX is a bridge application that connects ProsimA320 with GSX in Microsoft Flight Simulator 2020. It enables seamless integration between these two platforms, allowing users to control GSX services directly from the ProsimA320 environment.

## Core Requirements

1. **Automatic Detection and Connection**
   - Detect and connect to ProsimA320 and MSFS2020 automatically
   - Establish and maintain stable connections to both platforms
   - Handle connection failures gracefully

2. **GSX Service Integration**
   - Autonomous control of all GSX services from Prosim2GSX
   - Support for boarding, deboarding, refueling, catering, and other ground services
   - Automatic synchronization of passenger, cargo, and fuel data

3. **Flight Plan Synchronization**
   - Synchronize flight plans between ProsimA320 and GSX
   - Support for both MCDU and EFB flight plan types
   - ACARS integration for real-world flight plans

4. **Ground Equipment Management**
   - Automatic control of ground equipment (GPU, chocks, PCA)
   - Jetway/stairs connection and disconnection
   - Intelligent timing of equipment placement and removal based on flight phase

5. **User Interface**
   - Minimal, non-intrusive UI that runs in the system tray
   - Comprehensive configuration options
   - Clear status indicators for connections and services

6. **Audio Control**
   - GSX audio control via INT-Knob from the cockpit
   - ATC volume control via VHF1-Knob from the cockpit

## Project Scope

### In Scope

- Integration between ProsimA320 and GSX Pro for MSFS2020
- Automation of ground services and equipment
- Flight plan synchronization
- Audio control integration
- System tray application with configuration UI

### Out of Scope

- Modifications to ProsimA320 or GSX Pro software
- Support for aircraft other than the A320 (as ProsimA320 is A320-specific)
- Support for flight simulators other than MSFS2020
- Support for ground service add-ons other than GSX Pro

## Success Criteria

1. Seamless integration between ProsimA320 and GSX Pro
2. Reliable automation of ground services
3. Accurate synchronization of passenger, cargo, and fuel data
4. Intuitive user interface with comprehensive configuration options
5. Stable performance with minimal resource usage
