# Project Brief: Prosim2GSX

## Overview
Prosim2GSX is an integration tool that provides full automation and synchronization between Prosim A320 and GSX Pro (Ground Services for X-Plane/MSFS). It creates a seamless connection between the flight simulator's ground services and the Prosim A320 cockpit simulation.

## Core Purpose
To eliminate the manual synchronization between Prosim A320 and GSX Pro, allowing pilots to focus on realistic flight operations without worrying about discrepancies between the two systems.

## Key Features
- **Refueling Synchronization**: Synchronizes fuel levels between GSX and Prosim
- **Passenger & Cargo Synchronization**: Coordinates boarding and deboarding between systems
- **Ground Equipment Automation**: Automatically manages GPU, Chocks, and PCA
- **Service Call Automation**: Automates most service calls except Push-Back, De-Ice, and Gate-Selection
- **Audio Control Integration**: Controls GSX audio via the INT-Knob from the cockpit
- **ATC Volume Control**: Controls ATC volume via the VHF1-Knob from the cockpit

## Target Users
- Flight simulation enthusiasts using Prosim A320 cockpit simulation
- Virtual pilots who want a more realistic and integrated ground operations experience
- Users of both Prosim A320 and GSX Pro who want to eliminate manual synchronization

## Success Criteria
1. Seamless integration between Prosim A320 and GSX Pro
2. Reliable automation of ground services
3. Intuitive configuration through a simple UI
4. Minimal user intervention required during normal operations
5. Compatibility with standard flight simulation workflows

## Constraints
- Requires Windows 10/11
- Depends on .NET 7 x64 Runtime
- Requires MobiFlight WASM Module
- Must work with MSFS, Prosim, and GSX Pro
- Should not interfere with other simulation components

## Project Scope
The project focuses specifically on the integration between Prosim A320 and GSX Pro. It does not aim to replace either system but to create a bridge between them that enhances the overall simulation experience.
