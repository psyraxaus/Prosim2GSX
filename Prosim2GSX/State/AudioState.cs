using CommunityToolkit.Mvvm.ComponentModel;

namespace Prosim2GSX.State
{
    // Long-lived observable mirror for runtime Audio state that does not live on
    // Config (Config already owns ACP side, mappings, blacklist, startup volumes
    // and unmute). Today the only Model-local field is the audio-backend toggle.
    public partial class AudioState : ObservableObject
    {
        // True when CoreAudio backend is selected, false when VoiceMeeter is selected.
        [ObservableProperty] private bool _IsCoreAudioSelected = true;
    }
}
