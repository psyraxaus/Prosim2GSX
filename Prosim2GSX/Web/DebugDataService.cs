using CFIT.AppLogger;
using Prosim2GSX.GSX;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Prosim2GSX.Web
{
    // Read-only diagnostic snapshot service. Walks the live service graph on
    // every GetSnapshot() call and returns variable-name → value strings,
    // grouped for display. Owned by AppService for the app's lifetime; gated
    // by Config.ShowDebugTab at the surfaces (WPF tab + /api/debug).
    //
    // Reads, never writes — and it never caches; callers are expected to poll
    // at Config.DebugRefreshMs cadence. SDK-degraded state is fine: every
    // dereference is null-coalesced to "N/A" so the snapshot stays useful
    // even before services come up.
    public class DebugDataService
    {
        private readonly AppService _app;
        private bool _datarefsRegistered;

        public DebugDataService(AppService app)
        {
            _app = app;
        }

        // Walk every dataref the Debug tab surfaces and enrol it in the SDK
        // poll-and-cache machinery. Idempotent — Subscribe and
        // RegisterPollDataref both no-op on duplicates. Called lazily on the
        // first GetSnapshot so the SDK has had a chance to connect.
        private void EnsureDatarefsRegistered()
        {
            if (_datarefsRegistered) return;
            var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
            if (sdk == null) return;
            foreach (var dataRef in DebugTabDatarefs)
            {
                try
                {
                    sdk.Subscribe(dataRef, global::ProsimInterface.SdkLvarBridgeService.PollOnlyHandler);
                    sdk.RegisterPollDataref(dataRef);
                }
                catch (Exception ex) { Logger.LogException(ex, $"DebugDataService.Subscribe failed: {dataRef}"); }
            }
            _datarefsRegistered = true;
        }

        // Single source of truth for which datarefs the Debug tab needs to be
        // polled. Updated alongside the BuildProsim*Datarefs methods below.
        private static readonly string[] DebugTabDatarefs = new[]
        {
            // Switches
            "system.switches.S_MIP_PARKING_BRAKE",
            "system.switches.S_MIP_GEAR",
            "system.switches.S_ENG_MASTER_1",
            "system.switches.S_ENG_MASTER_2",
            "system.switches.S_ENG_MODE",
            "system.switches.S_OH_ELEC_BAT1",
            "system.switches.S_OH_ELEC_BAT2",
            "system.switches.S_OH_ELEC_GEN1",
            "system.switches.S_OH_ELEC_GEN2",
            "system.switches.S_OH_ELEC_APU_GENERATOR",
            "system.switches.S_OH_ELEC_EXT_PWR",
            "system.switches.S_OH_PNEUMATIC_APU_BLEED",
            "system.switches.S_OH_PNEUMATIC_PACK_1",
            "system.switches.S_OH_PNEUMATIC_PACK_2",
            "system.switches.S_OH_PROBE_HEAT",
            "system.switches.S_OH_EXT_LT_BEACON",
            "system.switches.S_OH_EXT_LT_STROBE",
            "system.switches.S_OH_EXT_LT_LANDING_L",
            "system.switches.S_OH_EXT_LT_LANDING_R",
            "system.switches.S_OH_EXT_LT_NAV_LOGO",
            "system.switches.S_OH_EXT_LT_NOSE",
            "system.switches.S_OH_EXT_LT_WING",
            "system.switches.S_OH_INT_LT_EMER",
            "system.switches.S_OH_SIGNS",
            "system.switches.S_OH_SIGNS_SMOKING",
            "system.switches.S_XPDR_MODE",
            "system.switches.S_OH_FUEL_LEFT_1",
            "system.switches.S_OH_FUEL_LEFT_2",
            "system.switches.S_OH_FUEL_RIGHT_1",
            "system.switches.S_OH_FUEL_RIGHT_2",
            "system.switches.S_OH_FUEL_CENTER_1",
            "system.switches.S_OH_FUEL_CENTER_2",
            "system.switches.S_OH_NAV_IR1_MODE",
            "system.switches.S_OH_NAV_IR2_MODE",
            "system.switches.S_OH_NAV_IR3_MODE",
            "system.switches.S_FCU_EFIS1_BARO_STD",
            "system.switches.S_FCU_EFIS2_BARO_STD",
            // Gates / indicators
            "system.gates.B_APU_RUNNING",
            "system.gates.B_ELEC_POWERUP",
            "system.gates.B_HYD_PARKING_BRAKE_SET",
            "system.gates.B_ELEC_BATTERY_SWITCH_1",
            "system.gates.B_ELEC_EXTERNAL_CONNECT",
            "system.gates.B_ELEC_BUS_POWER_DC_ESS",
            "system.indicators.I_MIP_AUTOBRAKE_MAX_L",
            "system.indicators.I_MIP_AUTOBRAKE_MED_L",
            "system.indicators.I_MIP_AUTOBRAKE_LO_L",
            "system.indicators.I_OH_PNEUMATIC_APU_BLEED_U",
            "system.indicators.I_OH_PNEUMATIC_APU_BLEED_L",
            "system.indicators.I_OH_ELEC_EXT_PWR_L",
            "system.indicators.I_OH_ELEC_EXT_PWR_U",
            // Aircraft state
            "aircraft.flightControls.throttle.1.lever",
            "aircraft.flightControls.throttle.2.lever",
            "aircraft.flap.positionHandle",
            "aircraft.adiru.1.position_available",
            "aircraft.adiru.2.position_available",
            "aircraft.adiru.3.position_available",
            "aircraft.systems.pneumatic.valve.BLEED_VALVE",
            "groundservice.groundpower",
        };

        // Outer key = group, inner key = variable name, value = current value
        // formatted as a string. Group order is the insertion order of the
        // outer Dictionary (preserved on .NET).
        public virtual Dictionary<string, Dictionary<string, string>> GetSnapshot()
        {
            EnsureDatarefsRegistered();
            var snapshot = new Dictionary<string, Dictionary<string, string>>
            {
                ["Connection"] = BuildConnection(),
                ["Flight Phase"] = BuildFlightPhase(),
                ["GSX LVARs"] = BuildGsxLvars(),
                ["ProSim Flight Data"] = BuildProsimFlightData(),
                ["Pushback / Doors"] = BuildPushbackDoors(),
                ["ProSim Datarefs — Switches"] = BuildProsimSwitchDatarefs(),
                ["ProSim Datarefs — Gates / Indicators"] = BuildProsimGateAndIndicatorDatarefs(),
                ["ProSim Datarefs — Aircraft"] = BuildProsimAircraftDatarefs(),
                ["Audio"] = BuildAudio(),
                ["Web"] = BuildWeb(),
            };
            return snapshot;
        }

        // Curated list of cockpit-switch datarefs the app actively subscribes
        // to. Mirrors the Checklists evaluation set so the user can validate
        // expected vs. actual values from the Debug tab.
        private Dictionary<string, string> BuildProsimSwitchDatarefs()
        {
            var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
            var refs = new (string Label, string DataRef)[]
            {
                ("Parking brake (S_MIP_PARKING_BRAKE)", "system.switches.S_MIP_PARKING_BRAKE"),
                ("Gear lever (S_MIP_GEAR)", "system.switches.S_MIP_GEAR"),
                ("Engine master 1 (S_ENG_MASTER_1)", "system.switches.S_ENG_MASTER_1"),
                ("Engine master 2 (S_ENG_MASTER_2)", "system.switches.S_ENG_MASTER_2"),
                ("Mode selector (S_ENG_MODE)", "system.switches.S_ENG_MODE"),
                ("Battery 1 (S_OH_ELEC_BAT1)", "system.switches.S_OH_ELEC_BAT1"),
                ("Battery 2 (S_OH_ELEC_BAT2)", "system.switches.S_OH_ELEC_BAT2"),
                ("Generator 1 (S_OH_ELEC_GEN1)", "system.switches.S_OH_ELEC_GEN1"),
                ("Generator 2 (S_OH_ELEC_GEN2)", "system.switches.S_OH_ELEC_GEN2"),
                ("APU generator (S_OH_ELEC_APU_GENERATOR)", "system.switches.S_OH_ELEC_APU_GENERATOR"),
                ("External power (S_OH_ELEC_EXT_PWR)", "system.switches.S_OH_ELEC_EXT_PWR"),
                ("APU bleed (S_OH_PNEUMATIC_APU_BLEED)", "system.switches.S_OH_PNEUMATIC_APU_BLEED"),
                ("Pack 1 (S_OH_PNEUMATIC_PACK_1)", "system.switches.S_OH_PNEUMATIC_PACK_1"),
                ("Pack 2 (S_OH_PNEUMATIC_PACK_2)", "system.switches.S_OH_PNEUMATIC_PACK_2"),
                ("Probe heat (S_OH_PROBE_HEAT)", "system.switches.S_OH_PROBE_HEAT"),
                ("Beacon (S_OH_EXT_LT_BEACON)", "system.switches.S_OH_EXT_LT_BEACON"),
                ("Strobe (S_OH_EXT_LT_STROBE)", "system.switches.S_OH_EXT_LT_STROBE"),
                ("Landing L (S_OH_EXT_LT_LANDING_L)", "system.switches.S_OH_EXT_LT_LANDING_L"),
                ("Landing R (S_OH_EXT_LT_LANDING_R)", "system.switches.S_OH_EXT_LT_LANDING_R"),
                ("Nav/Logo (S_OH_EXT_LT_NAV_LOGO)", "system.switches.S_OH_EXT_LT_NAV_LOGO"),
                ("Nose lights (S_OH_EXT_LT_NOSE)", "system.switches.S_OH_EXT_LT_NOSE"),
                ("Wing lights (S_OH_EXT_LT_WING)", "system.switches.S_OH_EXT_LT_WING"),
                ("Emergency lights (S_OH_INT_LT_EMER)", "system.switches.S_OH_INT_LT_EMER"),
                ("Seat belts (S_OH_SIGNS)", "system.switches.S_OH_SIGNS"),
                ("No-smoking (S_OH_SIGNS_SMOKING)", "system.switches.S_OH_SIGNS_SMOKING"),
                ("XPDR mode (S_XPDR_MODE)", "system.switches.S_XPDR_MODE"),
                ("Fuel pump L1 (S_OH_FUEL_LEFT_1)", "system.switches.S_OH_FUEL_LEFT_1"),
                ("Fuel pump L2 (S_OH_FUEL_LEFT_2)", "system.switches.S_OH_FUEL_LEFT_2"),
                ("Fuel pump R1 (S_OH_FUEL_RIGHT_1)", "system.switches.S_OH_FUEL_RIGHT_1"),
                ("Fuel pump R2 (S_OH_FUEL_RIGHT_2)", "system.switches.S_OH_FUEL_RIGHT_2"),
                ("Fuel pump CTR1 (S_OH_FUEL_CENTER_1)", "system.switches.S_OH_FUEL_CENTER_1"),
                ("Fuel pump CTR2 (S_OH_FUEL_CENTER_2)", "system.switches.S_OH_FUEL_CENTER_2"),
                ("ADIRS IR1 mode (S_OH_NAV_IR1_MODE)", "system.switches.S_OH_NAV_IR1_MODE"),
                ("ADIRS IR2 mode (S_OH_NAV_IR2_MODE)", "system.switches.S_OH_NAV_IR2_MODE"),
                ("ADIRS IR3 mode (S_OH_NAV_IR3_MODE)", "system.switches.S_OH_NAV_IR3_MODE"),
                ("Baro STD CPT (S_FCU_EFIS1_BARO_STD)", "system.switches.S_FCU_EFIS1_BARO_STD"),
                ("Baro STD F/O (S_FCU_EFIS2_BARO_STD)", "system.switches.S_FCU_EFIS2_BARO_STD"),
            };
            var dict = new Dictionary<string, string>();
            foreach (var (label, dataRef) in refs)
                dict[label] = ReadDataRefSafe(sdk, dataRef);
            return dict;
        }

        private Dictionary<string, string> BuildProsimGateAndIndicatorDatarefs()
        {
            var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
            var refs = new (string Label, string DataRef)[]
            {
                ("APU running (B_APU_RUNNING)", "system.gates.B_APU_RUNNING"),
                ("APU power-up (B_ELEC_POWERUP)", "system.gates.B_ELEC_POWERUP"),
                ("Hyd parking brake set (B_HYD_PARKING_BRAKE_SET)", "system.gates.B_HYD_PARKING_BRAKE_SET"),
                ("Battery switch 1 (B_ELEC_BATTERY_SWITCH_1)", "system.gates.B_ELEC_BATTERY_SWITCH_1"),
                ("External AC connect (B_ELEC_EXTERNAL_CONNECT)", "system.gates.B_ELEC_EXTERNAL_CONNECT"),
                ("DC ESS bus (B_ELEC_BUS_POWER_DC_ESS)", "system.gates.B_ELEC_BUS_POWER_DC_ESS"),
                ("Autobrake MAX (I_MIP_AUTOBRAKE_MAX_L)", "system.indicators.I_MIP_AUTOBRAKE_MAX_L"),
                ("Autobrake MED (I_MIP_AUTOBRAKE_MED_L)", "system.indicators.I_MIP_AUTOBRAKE_MED_L"),
                ("Autobrake LO (I_MIP_AUTOBRAKE_LO_L)", "system.indicators.I_MIP_AUTOBRAKE_LO_L"),
                ("APU bleed LED upper", "system.indicators.I_OH_PNEUMATIC_APU_BLEED_U"),
                ("APU bleed LED lower", "system.indicators.I_OH_PNEUMATIC_APU_BLEED_L"),
                ("EXT PWR LED ON (I_OH_ELEC_EXT_PWR_L)", "system.indicators.I_OH_ELEC_EXT_PWR_L"),
                ("EXT PWR LED AVAIL (I_OH_ELEC_EXT_PWR_U)", "system.indicators.I_OH_ELEC_EXT_PWR_U"),
            };
            var dict = new Dictionary<string, string>();
            foreach (var (label, dataRef) in refs)
                dict[label] = ReadDataRefSafe(sdk, dataRef);
            return dict;
        }

        private Dictionary<string, string> BuildProsimAircraftDatarefs()
        {
            var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
            var refs = new (string Label, string DataRef)[]
            {
                ("Throttle 1 lever", "aircraft.flightControls.throttle.1.lever"),
                ("Throttle 2 lever", "aircraft.flightControls.throttle.2.lever"),
                ("Flap handle", "aircraft.flap.positionHandle"),
                ("ADIRS 1 position available", "aircraft.adiru.1.position_available"),
                ("ADIRS 2 position available", "aircraft.adiru.2.position_available"),
                ("ADIRS 3 position available", "aircraft.adiru.3.position_available"),
                ("APU bleed valve", "aircraft.systems.pneumatic.valve.BLEED_VALVE"),
            };
            var dict = new Dictionary<string, string>();
            foreach (var (label, dataRef) in refs)
                dict[label] = ReadDataRefSafe(sdk, dataRef);
            return dict;
        }

        private static string ReadDataRefSafe(global::ProsimInterface.ProsimSdkInterface sdk, string dataRef)
        {
            if (sdk == null) return "N/A";
            try
            {
                var v = sdk.ReadDataRef(dataRef);
                if (v == null) return "N/A";
                return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "N/A";
            }
            catch { return "N/A"; }
        }

        private Dictionary<string, string> BuildConnection()
        {
            var sim = _app?.SimConnect;
            var simCtrl = _app?.SimService?.Controller;
            var prosim = _app?.ProsimService;
            var gsx = _app?.GsxService;
            var ai = gsx?.AircraftInterface;

            return new Dictionary<string, string>
            {
                ["SimConnected"] = Try(() => sim?.IsSimConnected),
                ["SessionRunning"] = Try(() => sim?.IsSessionRunning),
                ["SessionStopped"] = Try(() => sim?.IsSessionStopped),
                ["SimRunning"] = Try(() => simCtrl?.IsSimRunning),
                ["SimPaused"] = Try(() => sim?.IsPaused),
                ["CameraState"] = Try(() => sim?.CameraState),
                ["SimVersion"] = Try(() => sim?.SimVersionString),
                ["AircraftString"] = Try(() => sim?.AircraftString),
                ["IsProsimAircraft"] = Try(() => _app?.IsProsimAircraft),
                ["ProSim SDK Available"] = Try(() => Prosim2GSX.Instance?.IsSdkAvailable),
                ["ProSim SDK Initialized"] = Try(() => prosim?.IsInitialized),
                ["ProSim SDK Connected"] = Try(() => prosim?.IsConnected),
                ["AircraftInterface Loaded"] = Try(() => ai?.IsLoaded),
                ["GSX Running"] = Try(() => gsx?.CheckBinaries()),
                ["GsxController Active"] = Try(() => gsx?.IsActive),
            };
        }

        private Dictionary<string, string> BuildFlightPhase()
        {
            var gsx = _app?.GsxService;
            var auto = gsx?.AutomationController;
            var ai = gsx?.AircraftInterface;
            var fs = _app?.FlightStatus;

            return new Dictionary<string, string>
            {
                ["AutomationState"] = Try(() => auto?.State.ToString()),
                ["AutomationStarted"] = Try(() => auto?.IsStarted),
                ["IsOnGround"] = Try(() => auto?.IsOnGround),
                ["EnginesRunning"] = Try(() => ai?.EnginesRunning),
                ["GroundSpeed (kt)"] = Try(() => ai?.GroundSpeed),
                ["InMotion"] = Try(() => fs?.AppInMotion),
                ["IsBrakeSet"] = Try(() => ai?.IsBrakeSet),
                ["IsApuRunning"] = Try(() => ai?.IsApuRunning),
                ["IsApuBleedOn"] = Try(() => ai?.IsApuBleedOn),
                ["LightBeacon"] = Try(() => ai?.LightBeacon),
                ["LightNav"] = Try(() => ai?.LightNav),
                ["IsFlightPlanLoaded"] = Try(() => ai?.IsFlightPlanLoaded),
                ["FmsOrigin"] = Try(() => ai?.FmsOrigin),
                ["FmsDestination"] = Try(() => ai?.FmsDestination),
                ["FlightDuration"] = Try(() => ai?.FlightDuration.ToString()),
                ["Departure Services (done/run/total)"] =
                    Try(() => $"{auto?.ServiceCountCompleted ?? 0} / {auto?.ServiceCountRunning ?? 0} / {auto?.ServiceCountTotal ?? 0}"),
            };
        }

        // Walks the public static string members of GsxConstants for any value
        // that looks like an LVAR (starts with "L:"), then reads the cached
        // value from the GsxController's SimStore. Reflection here means
        // adding a new constant in GsxConstants.cs surfaces automatically in
        // the debug snapshot — no second list to maintain.
        private Dictionary<string, string> BuildGsxLvars()
        {
            var result = new Dictionary<string, string>();
            var simStore = _app?.GsxService?.SimStore;

            foreach (var (name, lvar) in EnumerateLvarConstants(typeof(GsxConstants)))
            {
                if (simStore == null)
                {
                    result[name] = "N/A";
                    continue;
                }

                try
                {
                    var sub = simStore[lvar];
                    if (sub == null)
                    {
                        result[name] = "N/A";
                        continue;
                    }
                    var n = sub.GetNumber();
                    result[name] = n.ToString("0.###", CultureInfo.InvariantCulture);
                }
                catch
                {
                    result[name] = "N/A";
                }
            }

            return result;
        }

        private Dictionary<string, string> BuildProsimFlightData()
        {
            var ai = _app?.GsxService?.AircraftInterface;

            return new Dictionary<string, string>
            {
                ["Registration"] = Try(() => ai?.Registration),
                ["Airline"] = Try(() => ai?.Airline),
                ["Title"] = Try(() => ai?.Title),
                ["FlightNumber"] = Try(() => ai?.FlightNumber),
                ["FuelCurrent (kg)"] = Try(() => ai?.FuelCurrent),
                ["FuelTarget (kg)"] = Try(() => ai?.FuelTarget),
                ["IsRefueling"] = Try(() => ai?.IsRefueling),
                ["UnitAircraft"] = Try(() => ai?.UnitAircraft.ToString()),
                ["EquipmentGpu"] = Try(() => ai?.EquipmentGpu),
                ["EquipmentPca"] = Try(() => ai?.EquipmentPca),
                ["EquipmentChocks"] = Try(() => ai?.EquipmentChocks),
                ["IsExternalPowerConnected"] = Try(() => ai?.IsExternalPowerConnected),
                ["HasOpenDoors"] = Try(() => ai?.HasOpenDoors),
                ["EfbBoardingState"] = Try(() => ai?.EfbBoardingState),
                ["EfbBoardingCompleted"] = Try(() => ai?.IsEfbBoardingCompleted),
                ["IsFinalReceived"] = Try(() => ai?.IsFinalReceived),
                ["SmartButtonRequest"] = Try(() => ai?.SmartButtonRequest),
                ["ZuluTimeSeconds"] = Try(() => ai?.ZuluTimeSeconds),
                ["SimbriefUser"] = Try(() => ai?.SimbriefUser),
            };
        }

        private Dictionary<string, string> BuildPushbackDoors()
        {
            var gsx = _app?.GsxService;
            var ctrl = gsx;
            var pushback = TryGetService<GSX.Services.GsxServicePushback>(GSX.Services.GsxServiceType.Pushback);
            var simStore = ctrl?.SimStore;

            return new Dictionary<string, string>
            {
                ["VehiclePushbackState"] = Try(() => pushback?.VehiclePushbackState),
                ["VehiclePushbackStateLabel"] = Try(() => pushback?.VehiclePushbackStateLabel),
                ["PushStatus"] = Try(() => pushback?.PushStatus),
                ["IsTugConnected"] = Try(() => pushback?.IsTugConnected),
                ["IsPinInserted"] = Try(() => pushback?.IsPinInserted),
                ["EngineStartConfirmed"] = Try(() => pushback?.EngineStartConfirmed),
                ["TugAttachedOnBoarding"] = Try(() => pushback?.TugAttachedOnBoarding),
                ["DoorToggle Cargo1"] = ReadLvar(simStore, GsxConstants.VarDoorToggleCargo1),
                ["DoorToggle Cargo2"] = ReadLvar(simStore, GsxConstants.VarDoorToggleCargo2),
                ["DoorToggle Service1"] = ReadLvar(simStore, GsxConstants.VarDoorToggleService1),
                ["DoorToggle Service2"] = ReadLvar(simStore, GsxConstants.VarDoorToggleService2),
            };
        }

        private Dictionary<string, string> BuildAudio()
        {
            var audio = _app?.AudioService;
            var cfg = _app?.Config;

            return new Dictionary<string, string>
            {
                ["AudioController Active"] = Try(() => audio?.IsActive),
                ["HasInitialized"] = Try(() => audio?.HasInitialized),
                ["IsPlanePowered"] = Try(() => audio?.IsPlanePowered),
                ["ResetVolumes"] = Try(() => audio?.ResetVolumes),
                ["ResetMappings"] = Try(() => audio?.ResetMappings),
                ["AcpSide"] = Try(() => cfg?.AudioAcpSide.ToString()),
                ["Mappings configured"] = Try(() => cfg?.AudioMappings?.Count),
                ["Blacklist entries"] = Try(() => cfg?.AudioDeviceBlacklist?.Count),
                ["RunAudioService"] = Try(() => cfg?.RunAudioService),
            };
        }

        private Dictionary<string, string> BuildWeb()
        {
            var host = _app?.WebHost;
            var cfg = _app?.Config;

            return new Dictionary<string, string>
            {
                ["WebServerEnabled"] = Try(() => cfg?.WebServerEnabled),
                ["IsRunning"] = Try(() => host?.IsRunning),
                ["Port"] = Try(() => cfg?.WebServerPort),
                ["BindAll"] = Try(() => cfg?.WebServerBindAll),
                ["TokenGeneration"] = Try(() => host?.TokenGeneration),
                ["AuthToken set"] = Try(() => !string.IsNullOrEmpty(cfg?.WebServerAuthToken)),
                ["RefreshMs"] = Try(() => cfg?.DebugRefreshMs),
            };
        }

        // ---------- helpers ----------

        // Lazy-evaluating wrapper: returns "N/A" on null OR exception, otherwise
        // the value's invariant-culture string form. Keeps the dictionary
        // builders short and immune to NREs deep in the service graph.
        private static string Try<T>(Func<T> f)
        {
            try
            {
                var v = f();
                if (v == null) return "N/A";
                return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "N/A";
            }
            catch (Exception ex)
            {
                Logger.Verbose($"DebugDataService.Try failed: {ex.Message}");
                return "N/A";
            }
        }

        private static string ReadLvar(CFIT.AppFramework.ResourceStores.SimStore simStore, string lvar)
        {
            if (simStore == null) return "N/A";
            try
            {
                var sub = simStore[lvar];
                if (sub == null) return "N/A";
                return sub.GetNumber().ToString("0.###", CultureInfo.InvariantCulture);
            }
            catch
            {
                return "N/A";
            }
        }

        private TService TryGetService<TService>(GSX.Services.GsxServiceType type)
            where TService : class
        {
            try
            {
                var services = _app?.GsxService?.GsxServices;
                if (services == null) return null;
                return services.TryGetValue(type, out var s) ? s as TService : null;
            }
            catch
            {
                return null;
            }
        }

        // Discovers all `L:FSDT_GSX_*` constants on the supplied type.
        // Handles both `public const string` and `public static string ... { get; }`
        // forms, which GsxConstants mixes today.
        private static IEnumerable<(string Name, string Value)> EnumerateLvarConstants(Type t)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

            foreach (var f in t.GetFields(flags))
            {
                if (f.FieldType != typeof(string)) continue;
                var v = f.GetValue(null) as string;
                if (string.IsNullOrEmpty(v) || !v.StartsWith("L:")) continue;
                yield return (f.Name, v);
            }

            foreach (var p in t.GetProperties(flags))
            {
                if (p.PropertyType != typeof(string) || !p.CanRead) continue;
                string v;
                try { v = p.GetValue(null) as string; }
                catch { continue; }
                if (string.IsNullOrEmpty(v) || !v.StartsWith("L:")) continue;
                yield return (p.Name, v);
            }
        }
    }
}
