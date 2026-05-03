using CoreAudio;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;
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

        public AcpSide AudioAcpSide { get; set; } = AcpSide.CPT;
        public DataFlow AudioDeviceFlow { get; set; } = DataFlow.Render;
        public DeviceState AudioDeviceState { get; set; } = DeviceState.Active;

        // Mappings preserved in user-edit order (matches WPF grid order).
        public List<AudioMappingDto> Mappings { get; set; } = new();

        // Devices the user has chosen to exclude from enumeration.
        public List<string> Blacklist { get; set; } = new();

        public static AudioDto From(AppService app)
        {
            var config = app.Config;
            var audio = app.Audio;
            if (config == null)
                return new AudioDto();

            return new AudioDto
            {
                IsCoreAudioSelected = audio?.IsCoreAudioSelected ?? true,
                AudioAcpSide = config.AudioAcpSide,
                AudioDeviceFlow = config.AudioDeviceFlow,
                AudioDeviceState = config.AudioDeviceState,
                Mappings = config.AudioMappings?.Select(AudioMappingDto.From).ToList() ?? new(),
                Blacklist = config.AudioDeviceBlacklist?.ToList() ?? new(),
            };
        }

        public void ApplyTo(AppService app)
        {
            var config = app.Config;
            var audio = app.Audio;
            var ctrl = app.AudioService;
            if (config == null) return;

            if (audio != null) audio.IsCoreAudioSelected = IsCoreAudioSelected;

            // Use the same setter side-effects ModelAudio relies on so the
            // controller picks up the change without a tab open.
            bool mappingsChanged =
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

            config.SaveConfiguration();

            // Match the WPF tab's setter side-effects: mappings/device-filter
            // changes prompt the controller to re-enumerate; ACP-side changes
            // prompt a fresh volume reset on the next tick.
            if (ctrl != null)
            {
                if (mappingsChanged)
                    ctrl.ResetMappings = true;
                ctrl.ResetVolumes = true;
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
