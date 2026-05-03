using CommunityToolkit.Mvvm.ComponentModel;

namespace Prosim2GSX.State
{
    // Long-lived observable mirror for runtime Audio state. Today it surfaces
    // the audio-backend toggle (CoreAudio vs VoiceMeeter), which is the
    // inverse of Config.UseVoiceMeeter. The WPF AUDIO API radio buttons bind
    // here; ModelAudio writes through to Config so persistence and live state
    // stay in lock-step.
    public partial class AudioState : ObservableObject
    {
        [ObservableProperty] private bool _IsCoreAudioSelected = true;
    }
}
