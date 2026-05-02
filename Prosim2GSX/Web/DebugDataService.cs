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

        public DebugDataService(AppService app)
        {
            _app = app;
        }

        // Outer key = group, inner key = variable name, value = current value
        // formatted as a string. Group order is the insertion order of the
        // outer Dictionary (preserved on .NET).
        public virtual Dictionary<string, Dictionary<string, string>> GetSnapshot()
        {
            var snapshot = new Dictionary<string, Dictionary<string, string>>
            {
                ["Connection"] = BuildConnection(),
                ["Flight Phase"] = BuildFlightPhase(),
                ["GSX LVARs"] = BuildGsxLvars(),
                ["ProSim Flight Data"] = BuildProsimFlightData(),
                ["Pushback / Doors"] = BuildPushbackDoors(),
                ["Audio"] = BuildAudio(),
                ["Web"] = BuildWeb(),
            };
            return snapshot;
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
