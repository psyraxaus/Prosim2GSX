# Prosim2GSX
<img src="img/icon.png" width="196"><br/>
Full and proper GSX Integration and Automation for the Prosim A320! <br/>

> **Note:** Version 0.4.0 has been updated to .NET 8. Please ensure you have the .NET 8 Runtime installed before updating.

- The Refuel Service fill's the Tanks as planned (or more correctly GSX and Prosim are "synched")
- Calling Boarding load's Passengers and Cargo, as does Deboarding for unloading (or more correctly GSX and Prosim are "synched")
- Ground Equipment (GPU, Chocks, PCA) is automatically set or removed
- All Service Calls except Push-Back, De-Ice and Gate-Selection can be automated
- GSX Audio can be controlled via the INT-Knob from the Cockpit
- ATC Volume can be controlled via the VHF1-Knob from the Cockpit (ATC Application configurable)
- VoiceMeeter integration for advanced audio control of all radio panel channels (INT, VHF1, VHF2, VHF3, CAB, PA)

<br/><br/>

## Requirements
- Windows 10/11
- [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) x64 Runtime (.NET Runtime and .NET Desktop Runtime. Do not confuse it with arm64!) installed & updated. Reboot when installing the Runtimes for the first Time.
- MobiFlight [WASM Module](https://github.com/MobiFlight/MobiFlight-WASM-Module/releases) installed in your Community Folder
- MSFS, Prosim, GSX Pro :wink:

<br/><br/>
## Installation
Extract it anywhere you want, but do not use Application-Folders, User-Folders or even C:\\ <br/>
Please remove the old Version completely before updating.<br/>
It may be blocked by Windows Security or your AV-Scanner, try if unblocking and/or setting an Exception helps.<br/>
You can check if the .NET Runtimes are correctly installed by running the Command `dotnet --list-runtimes` - .NET 8 should show up in the list.<br/><br/>

If you own a registered Copy of FSUIPC, you can start it automatically through that. Add this to your FSUIPC7.ini:
```
[Programs]
RunIf1=READY,KILL,X:\PATH\YOU\USED\Prosim2GSX.exe
```
The ini-File is in the Folder where FSUIPC was installed to, remember to change the Path to the Binary. If there are multiple RunIf-Entries, make sure they are numbered uniquely and that the [Programs] Section only exists once.<br/>
When starting it manually (or by other means), either start it before MSFS or when MSFS is in the Main Menu.

<br/><br/>
## Configuration
**Prosim**:<br/>
Disable **Auto-Door** and **Auto-Jetway** Simulation in the EFB!<br/><br/>

**Prosim2GSX**:<br/>
The Configuration is done through the UI, open it by clicking on the System-Tray/Notification-Icon. They are stored persistently in the *Prosim2GSX.dll.config* File - so set them once to your Preference and you should be fine :smiley:<br/>
All Options have ToolTips which explains them further.
<br/><br/>
<img src="img/ui.png" width="400"><br/><br/>
All Settings can be changed dynamically on the Fly if needed. But do that before a Service/Feature starts or after it has ended. For example, don't disable "Automatic Jetway/Stair Operation" while the Jetway is connected. Do it before the Tool calls the Jetway or after it was disconnected by the Tool.<br/><br/>
In general, it is up to your Preference how much Automation you want. I you want to keep Control of when Services are Called and/or the Jetway is connected, you can still enjoy the (De-)Boarding and Refueling Syncronization when the Automation-Options are disabled. The only Automation which can not be disabled: The Removal of the Ground-Equipment and Jetway-Disconnection (if still connected) is always active on Depature.<br/><br/>
A Note on the Audio-Control: The Tool does not control Audio until the Plane is powered (=FCU is On). Be aware, that the Prosim defaults to 50% Volume on INT and VHF1 when loaded. When you end your Session, Prosim2GSX will try to reset the Application-Audio to unmuted and 100% Volume. But that does not really work on GSX because it is resetting at the same Time. So GSX can stay muted when switching to another Plane (if it was muted) - keep that in Mind.

## Audio Control with VoiceMeeter

Prosim2GSX now supports controlling VoiceMeeter strips and buses directly from the Prosim A320 cockpit. This allows for more advanced audio control beyond what's possible with Windows Core Audio.

### Features
- Control VoiceMeeter strips and buses using the radio panel knobs in Prosim
- Map each audio channel (INT, VHF1, VHF2, VHF3, CAB, PA) to a specific VoiceMeeter strip or bus
- Control volume and mute state for each channel
- Synchronize Prosim radio panel state with VoiceMeeter parameters

### Setup
1. Install VoiceMeeter (Standard, Banana, or Potato) from [VB-Audio](https://vb-audio.com/Voicemeeter/)
2. Start VoiceMeeter before launching Prosim2GSX
3. In Prosim2GSX settings, select "VoiceMeeter" as the Audio API
4. For each channel you want to control:
   - Enable the channel
   - Select whether it's a strip or bus
   - Select the specific strip or bus from the dropdown
   - Configure the latch mute option if desired
5. Click "Refresh" if you don't see your strips or buses in the dropdown

### Notes
- VoiceMeeter must be running before Prosim2GSX starts
- You don't need to specify a process name when using VoiceMeeter
- The radio panel knobs in Prosim directly control the corresponding VoiceMeeter strips/buses
- Volume range is mapped from Prosim's range (176-1020) to VoiceMeeter's range (-60dB to +12dB)
- Mute state is inverted between Prosim and VoiceMeeter (REC button on = unmuted in VoiceMeeter)


<br/><br/>

**GSX Pro**:
- Make sure you do not have a customized Aircraft Config (GSX In-Game Menu -> Customize Aircraft -> should show only "Internal GSX Database"). If you want to keep your customized Config for whatever Reason, make sure the Option **"Show MSFS Fuel and Cargo during refueling"** is disabled!
- If using any Automation Option from Prosim2GSX, make sure **"Assistance services Auto Mode"** is disabled in the GSX Settings (GSX In-Game Menu -> GSX Settings -> Simulation)
- If you have troubles with Refueling, try if disabling "Always refuel progressively" and "Detect custom aircraft system refueling" in the GSX Settings helps. (Though it should work with these Settings)
- The De-/Boarding Speed of Passengers is dependant on the Passenger Density Setting (GSX In-Game Menu -> GSX Settings -> Timings). Higher Density => faster Boarding. *BUT*: The Setting **Extreme** is too extreme! Boarding does not work with this Setting.
- Ensure the other two Settings under Timings are on their Default (15s, 1x).

<br/><br/>

## General Service Flow
There might be Issues when used together with FS2Crew! (that is "FS2Crew: Prosim A320 Edition", the RAAS Tool is fine!)

1) Create your SB Flightplan and start MSFS as you normally would. Depending on your Configuration, start the Tool before MSFS or when MSFS is in the Main Menu.
2) When your Session is loaded (Ready to Fly was pressed), wait for the Repositioning and Jetway/Stair Call to happen (if configured).
3) Import your Flightplan on the EFB (wherever you're using it from, does not need to be the EFB in the VC). Refueling and Catering will be called (if configured). Always import a Flightplan on the EFB, regardless of Configuration. Power up the Plane from Cold & Dark before importing the Flightplan.
4) When Refueling and Boarding are finished (whoever called it), you will receive your Final Loadsheet after 90-150s. The left Forward Door will be closed when this happens (if not already closed by GSX). Also when both Services are finished and the APU is Avail and the APU Bleed is switched ON: the PCA will be removed (if configured to connect)
5) When the Parking Brake is set, External Power is disconnected (on the Overhead) and Beacon Light is On, the Tool will remove all Ground-Equipment: Jetway/Stairs (if not already removed) and GPU, PCA & Chocks (always, to be safe).
6) Happy Flight!
7) When you arrive (pre-select your Gate), the Jetway/Stairs will automatically connect as soon as the Engines are Off and the Parking Brake is set (if configured).
8) When the Beacon Light is off, the other Ground-Equipment will placed: GPU, PCA (if configured) and Chocks. If configured, Deboarding will be called. Calling Deboarding in the EFB is not required, it is best to dismiss that. Only generate a new Flightplan in SimBrief until Deboarding has actively started!
9) It works with Turn-Arounds! As soon as you (re)import a new Flightplan the Cycle starts over (after Deboarding has completely finished).


If you set every Option for automatic Service Calls, I'd recommend to disable the GSX Menu in the Toolbar (Icon not white). The Services are still called, but you won't see the Menu popping-up. So Push-Back, De-Ice and Gate-Selection are the only Situations where you need to open it.<br/>
Be aware that Prosim2GSX automatically selectes the first Operator in the List if GSX asks for a Selection (Ground Handling, Catering). If you're picky about which Operator should appear, you have to disable the Automatic Jetway Operation and the Automatic Catering Call!<br/><br/>
Be cautious on the Mass and Balance Page in the EFB: Don't change the planned Numbers, use *"Load Aircraft"* or *"Reset All"* - they likely break the Integration. *"Resend Loadsheet"* should not hurt though if needed! (In Case the Prelim-LS wasn't send automatically by the Prosim due to EOBT or because the Plane had no Power yet).<br/><br/>
Tip for VATSIM / IVAO: Disable the automatic Jetway Operation before loading the Session in MSFS, in Case you need to move to another Gate. If the Gate is free (or you have moved to a free one) you can renable Auto-Connect and the Jetway/Stairs will still connect then (unless the Flightplan was already loaded in the EFB).<br/><br/>
