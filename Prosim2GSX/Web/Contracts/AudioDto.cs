using CoreAudio;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.Web.Contracts
{
    // Audio Settings tab content — full mirror of the WPF tab. Includes the
    // backend selector (CoreAudio vs VoiceMeeter), app→device mappings, and
    // the device blacklist. ACP knob/latch state is read from ProSim datarefs
    // at runtime, so per-channel startup volumes are not exposed here.
    //
    // Threading: ApplyTo writes Config + AudioState. Phase 6 controllers must
    // marshal onto the WPF dispatcher.
    public class AudioDto
    {
        public bool IsCoreAudioSelected { get; set; } = true;

        // Persisted backend toggle (positive sense). IsCoreAudioSelected is
        // kept on the wire for backwards compatibility; the two are inverses.
        public bool UseVoiceMeeter { get; set; } = false;
        public string VoiceMeeterDllPath { get; set; } = "";

        public AcpSide AudioAcpSide { get; set; } = AcpSide.CPT;
        public DataFlow AudioDeviceFlow { get; set; } = DataFlow.Render;
        public DeviceState AudioDeviceState { get; set; } = DeviceState.Active;

        // Mappings preserved in user-edit order (matches WPF grid order).
        public List<AudioMappingDto> Mappings { get; set; } = new();

        // Multi-ACP VoiceMeeter routing. ActiveAcps selects 1-3 concurrent
        // ACPs; VoiceMeeterMappingsByAcp holds each ACP's channel-to-strip
        // bindings. Keys serialize as enum names ("CPT"/"FO"/"OBS") via the
        // global JsonStringEnumConverter. CoreAudio mappings (above) stay
        // dormant while UseVoiceMeeter is true.
        public List<AcpSide> ActiveAcps { get; set; } = new() { AcpSide.CPT };
        public Dictionary<AcpSide, List<VoiceMeeterMappingDto>> VoiceMeeterMappingsByAcp { get; set; } = new();

        // Last fallback the binder applied — null when validation passed.
        // The React panel surfaces this as a yellow banner in the
        // VoiceMeeter Mappings section. Read-only field on the wire; the
        // server ignores it on POST.
        public string VoiceMeeterFallbackReason { get; set; }

        // Devices the user has chosen to exclude from enumeration.
        public List<string> Blacklist { get; set; } = new();

        public static AudioDto From(AppService app)
        {
            var config = app.Config;
            var audio = app.Audio;
            var ctrl = app.AudioService;
            if (config == null)
                return new AudioDto();

            var byAcp = new Dictionary<AcpSide, List<VoiceMeeterMappingDto>>();
            if (config.VoiceMeeterMappingsByAcp != null)
            {
                foreach (var kv in config.VoiceMeeterMappingsByAcp)
                {
                    byAcp[kv.Key] = kv.Value?.Select(VoiceMeeterMappingDto.From).ToList() ?? new();
                }
            }

            return new AudioDto
            {
                IsCoreAudioSelected = !config.UseVoiceMeeter,
                UseVoiceMeeter = config.UseVoiceMeeter,
                VoiceMeeterDllPath = config.VoiceMeeterDllPath ?? "",
                AudioAcpSide = config.AudioAcpSide,
                AudioDeviceFlow = config.AudioDeviceFlow,
                AudioDeviceState = config.AudioDeviceState,
                Mappings = config.AudioMappings?.Select(AudioMappingDto.From).ToList() ?? new(),
                ActiveAcps = config.ActiveAcps?.ToList() ?? new() { AcpSide.CPT },
                VoiceMeeterMappingsByAcp = byAcp,
                VoiceMeeterFallbackReason = ctrl?.VoiceMeeterFallbackReason,
                Blacklist = config.AudioDeviceBlacklist?.ToList() ?? new(),
            };
        }

        public void ApplyTo(AppService app)
        {
            var config = app.Config;
            var audio = app.Audio;
            var ctrl = app.AudioService;
            if (config == null) return;

            // UseVoiceMeeter is the source of truth; IsCoreAudioSelected on
            // the wire stays in sync but the inverse flag wins if both are set.
            config.UseVoiceMeeter = UseVoiceMeeter;
            config.VoiceMeeterDllPath = VoiceMeeterDllPath ?? "";
            if (audio != null) audio.IsCoreAudioSelected = !UseVoiceMeeter;

            // Use the same setter side-effects ModelAudio relies on so the
            // controller picks up the change without a tab open.
            bool coreAudioMappingsChanged =
                config.AudioAcpSide != AudioAcpSide
                || config.AudioDeviceFlow != AudioDeviceFlow
                || config.AudioDeviceState != AudioDeviceState
                || !MappingListsEqual(config.AudioMappings, Mappings)
                || !BlacklistEqual(config.AudioDeviceBlacklist, Blacklist);

            config.AudioAcpSide = AudioAcpSide;
            config.AudioDeviceFlow = AudioDeviceFlow;
            config.AudioDeviceState = AudioDeviceState;

            // Replace the lists wholesale — preserves caller-supplied order.
            config.AudioMappings = Mappings?.Select(m => m.ToAudioMapping()).ToList() ?? new();
            config.AudioDeviceBlacklist = Blacklist?.ToList() ?? new();

            // Multi-ACP VM state. Clamp ActiveAcps to {CPT,FO,OBS} unique
            // entries; reject empty (default to [CPT]). Same invariants as
            // Config.NormalizeActiveAcps — repeated here so the server stays
            // authoritative even on malformed POSTs.
            var clean = new List<AcpSide>();
            foreach (var side in ActiveAcps ?? Enumerable.Empty<AcpSide>())
            {
                if (!Enum.IsDefined(typeof(AcpSide), side)) continue;
                if (clean.Contains(side)) continue;
                clean.Add(side);
            }
            if (clean.Count == 0) clean.Add(AcpSide.CPT);
            config.ActiveAcps = clean;

            var nextByAcp = new Dictionary<AcpSide, List<VoiceMeeterMapping>>();
            if (VoiceMeeterMappingsByAcp != null)
            {
                foreach (var kv in VoiceMeeterMappingsByAcp)
                {
                    if (!Enum.IsDefined(typeof(AcpSide), kv.Key)) continue;
                    nextByAcp[kv.Key] = kv.Value?.Select(m => m.ToVoiceMeeterMapping()).ToList() ?? new();
                }
            }
            config.VoiceMeeterMappingsByAcp = nextByAcp;

            config.SaveConfiguration();

            // Match the WPF tab's setter side-effects: mappings/device-filter
            // changes prompt the controller to re-enumerate; ACP-side changes
            // prompt a fresh volume reset on the next tick. VoiceMeeter
            // mapping edits flag a binder rebind on the audio service tick.
            if (ctrl != null)
            {
                if (coreAudioMappingsChanged)
                    ctrl.ResetMappings = true;
                ctrl.ResetVolumes = true;
                ctrl.ResetVoiceMeeterBindings = true;
            }
        }

        private static bool MappingListsEqual(List<AudioMapping> a, List<AudioMappingDto> b)
        {
            if (a == null || b == null) return a == null && b == null;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                var x = a[i]; var y = b[i];
                if (x.Channel != y.Channel || x.Device != y.Device || x.Binary != y.Binary
                    || x.UseLatch != y.UseLatch || x.OnlyActive != y.OnlyActive)
                    return false;
            }
            return true;
        }

        private static bool BlacklistEqual(List<string> a, List<string> b)
        {
            if (a == null || b == null) return a == null && b == null;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }
    }
}
