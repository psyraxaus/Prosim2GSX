using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;

namespace Prosim2GSX.Web.Contracts
{
    // One row of the Audio tab's mapping grid. Mirrors AudioMapping verbatim;
    // the "All" sentinel (empty Device) is preserved as-is on the wire so the
    // React UI sees the same shape the WPF tab does.
    public class AudioMappingDto
    {
        public AudioChannel Channel { get; set; }
        public string Device { get; set; } = "";
        public string Binary { get; set; } = "";
        public bool UseLatch { get; set; } = true;
        public bool OnlyActive { get; set; } = true;

        public static AudioMappingDto From(AudioMapping src) => new()
        {
            Channel = src.Channel,
            Device = src.Device ?? "",
            Binary = src.Binary ?? "",
            UseLatch = src.UseLatch,
            OnlyActive = src.OnlyActive,
        };

        public AudioMapping ToAudioMapping() => new(
            Channel,
            Device ?? "",
            Binary ?? "",
            UseLatch,
            OnlyActive);
    }
}
