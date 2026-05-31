namespace Prosim2GSX.GSX.Services
{
    /// <summary>
    /// Strongly-typed model of GSX's <c>FSDT_GSX_JETWAY_OPERATION</c> /
    /// <c>FSDT_GSX_STAIRS_OPERATION</c> LVARs. The raw LVAR is an int that
    /// GSX uses as a positioning/animation state machine. Field testing
    /// across GSX Pro v4 (notably with Remote Control experimental enabled)
    /// has shown the LVAR can stick at <see cref="InMotion"/> values
    /// indefinitely after the equipment has visibly docked — see
    /// <c>GsxAutomationController.IsGateConnected</c> for the resulting
    /// stability-fallback. The enum names what we know empirically; raw
    /// values outside the documented set map to <see cref="Unknown"/>.
    /// </summary>
    public enum JetwayOperation
    {
        Unknown = 0,
        /// <summary>Equipment fully retracted / parked away from the aircraft (raw value 1).</summary>
        Retracted = 1,
        /// <summary>Equipment docked at the aircraft, idle (raw value 2).</summary>
        DockedIdle = 2,
        /// <summary>Brief transitional state observed in some flows (raw value 3) — treated as neither idle nor in-motion to preserve legacy gating.</summary>
        Transitional = 3,
        /// <summary>GSX reports the equipment is physically moving (raw value 4+). May persist erroneously under Remote Control.</summary>
        InMotion = 4,
    }

    /// <summary>
    /// Helpers for translating raw <c>FSDT_GSX_*_OPERATION</c> readings
    /// into <see cref="JetwayOperation"/> and back to high-level
    /// categories that the service layer cares about.
    /// </summary>
    public static class JetwayOperationExtensions
    {
        /// <summary>Parses the raw LVAR number into a <see cref="JetwayOperation"/>. Any raw value above 3 collapses to <see cref="JetwayOperation.InMotion"/>; 0 and negative are <see cref="JetwayOperation.Unknown"/>.</summary>
        public static JetwayOperation FromRaw(double raw)
        {
            var n = (int)raw;
            return n switch
            {
                1 => JetwayOperation.Retracted,
                2 => JetwayOperation.DockedIdle,
                3 => JetwayOperation.Transitional,
                > 3 => JetwayOperation.InMotion,
                _ => JetwayOperation.Unknown,
            };
        }

        /// <summary>True when the equipment is in a stable "not currently moving" state. Matches the legacy <c>operation &lt; 3</c> gate.</summary>
        public static bool IsIdle(this JetwayOperation op)
            => op == JetwayOperation.Retracted || op == JetwayOperation.DockedIdle;

        /// <summary>True when GSX reports physical motion. Matches the legacy <c>operation &gt; 3</c> gate. NOTE: may report true erroneously under GSX Pro v4 Remote Control — see the IsGateConnected stability fallback for the mitigation.</summary>
        public static bool IsInMotion(this JetwayOperation op)
            => op == JetwayOperation.InMotion;
    }

    /// <summary>
    /// Strongly-typed model of GSX's <c>FSDT_GSX_VEHICLE_PUSHBACK_STATE</c>
    /// LVAR. The raw LVAR carries GSX's pushback state machine with the
    /// documented values listed below. Renamed from "VehiclePushbackState"
    /// to <see cref="PushbackPhase"/> so the enum doesn't collide with
    /// <see cref="GsxServicePushback.VehiclePushbackState"/>'s int property,
    /// which existing consumers (state worker, debug snapshot) still read.
    /// </summary>
    public enum PushbackPhase
    {
        Unknown = -1,
        /// <summary>No pushback active (raw value 0).</summary>
        Idle = 0,
        /// <summary>Observed early state before pushback begins (raw value 1) — semantics unclear, used for "at gate / preparing" type states.</summary>
        AtGate = 1,
        /// <summary>Physical pushback in progress (raw value 8).</summary>
        PushingBack = 8,
        /// <summary>Tug waiting for engine shutdown (raw value 11).</summary>
        WaitingForEngineShutdown = 11,
        /// <summary>Push complete, awaiting "Confirm good engine start" (raw value 12).</summary>
        AwaitingEngineStart = 12,
        /// <summary>Tug disconnecting from aircraft (raw value 13).</summary>
        Disconnecting = 13,
        /// <summary>Tug clear, aircraft ready to start taxiing (raw value 14).</summary>
        ClearToStart = 14,
    }

    /// <summary>
    /// Helpers for translating raw <c>FSDT_GSX_VEHICLE_PUSHBACK_STATE</c>
    /// readings into <see cref="PushbackPhase"/> and to compute
    /// high-level predicates the menu and automation layers want.
    /// </summary>
    public static class PushbackPhaseExtensions
    {
        /// <summary>Parses the raw LVAR number into a <see cref="PushbackPhase"/>. Unrecognised raw values map to <see cref="PushbackPhase.Unknown"/>.</summary>
        public static PushbackPhase FromRaw(double raw)
        {
            var n = (int)raw;
            return n switch
            {
                0 => PushbackPhase.Idle,
                1 => PushbackPhase.AtGate,
                8 => PushbackPhase.PushingBack,
                11 => PushbackPhase.WaitingForEngineShutdown,
                12 => PushbackPhase.AwaitingEngineStart,
                13 => PushbackPhase.Disconnecting,
                14 => PushbackPhase.ClearToStart,
                _ => PushbackPhase.Unknown,
            };
        }

        /// <summary>True when GSX is past the direction-selection phase — any physical push, engine handshake, or tug-disconnect activity. Direction-menu auto-select is suppressed when this is true (GSX has been observed to re-open the direction menu after the push has already started).</summary>
        public static bool IsPushInProgress(this PushbackPhase phase)
            => phase == PushbackPhase.PushingBack
            || phase == PushbackPhase.WaitingForEngineShutdown
            || phase == PushbackPhase.AwaitingEngineStart
            || phase == PushbackPhase.Disconnecting
            || phase == PushbackPhase.ClearToStart;

        /// <summary>Human-readable label for diagnostic / debug surfaces. Replaces the freestanding <c>MapVehiclePushbackState</c> switch helper on <see cref="GsxServicePushback"/>.</summary>
        public static string ToDisplayLabel(this PushbackPhase phase) => phase switch
        {
            PushbackPhase.Idle => "Idle",
            PushbackPhase.AtGate => "At gate",
            PushbackPhase.PushingBack => "Pushing back",
            PushbackPhase.WaitingForEngineShutdown => "Waiting for engine shutdown",
            PushbackPhase.AwaitingEngineStart => "Awaiting engine start confirmation",
            PushbackPhase.Disconnecting => "Disconnecting",
            PushbackPhase.ClearToStart => "Clear to start",
            _ => "Unknown",
        };
    }
}
