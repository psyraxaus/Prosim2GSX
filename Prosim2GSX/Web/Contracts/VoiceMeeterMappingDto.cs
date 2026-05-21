using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;

namespace Prosim2GSX.Web.Contracts
{
    // ACP channel → VoiceMeeter strip/bus binding. Independent of
    // AudioMappingDto — VoiceMeeter routing is per-mixer-target, not
    // per-process. Only consulted on the server when UseVoiceMeeter is true.
    public class VoiceMeeterMappingDto
    {
        public AudioChannel Channel { get; set; }
        public int StripIndex { get; set; }
        public bool IsBus { get; set; }
        public bool UseLatch { get; set; } = true;

        public static VoiceMeeterMappingDto From(VoiceMeeterMapping src) => new()
        {
            Channel = src.Channel,
            StripIndex = src.StripIndex,
            IsBus = src.IsBus,
            UseLatch = src.UseLatch,
        };

        public VoiceMeeterMapping ToVoiceMeeterMapping() => new(Channel, StripIndex, IsBus, UseLatch);
    }
}
