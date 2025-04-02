using System;

namespace Prosim2GSX.Services.Audio
{
    /// <summary>
    /// Interface for audio control services
    /// </summary>
    public interface IAudioService : IDisposable
    {
        /// <summary>
        /// Initializes the audio service and sets up audio sources
        /// </summary>
        void Initialize();

        /// <summary>
        /// Controls audio volume and mute state based on cockpit controls
        /// </summary>
        void ControlAudio();

        /// <summary>
        /// Resets audio settings to default values
        /// </summary>
        void ResetAudio();

        /// <summary>
        /// Adds a new audio source to be controlled
        /// </summary>
        /// <param name="processName">Name of the process to control</param>
        /// <param name="sourceName">Friendly name for the audio source</param>
        /// <param name="knobLvarName">LVAR name for the volume knob</param>
        /// <param name="muteLvarName">LVAR name for the mute control</param>
        void AddAudioSource(string processName, string sourceName, string knobLvarName, string muteLvarName);

        /// <summary>
        /// Removes an audio source
        /// </summary>
        /// <param name="sourceName">Friendly name of the audio source to remove</param>
        void RemoveAudioSource(string sourceName);

        void Dispose();
    }
}
