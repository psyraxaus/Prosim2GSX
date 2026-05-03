using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using ProsimInterface;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Audio
{
    // Drives VoiceMeeter strip/bus volume + mute from ProSim ACP datarefs
    // when Config.UseVoiceMeeter is true. Independent of AudioSession; one
    // subscription per VoiceMeeterMapping on the configured ACP side.
    //
    // VBVMR_SetParameterFloat is sub-ms, so this writes synchronously on the
    // SDK callback thread — no per-mapping worker / coalescer needed. The
    // IsPlanePowered gate is checked before each write so knob movement on
    // a cold-and-dark aircraft is a no-op (parity with the CoreAudio path).
    public class VoiceMeeterChannelBinder
    {
        private readonly AudioController _controller;
        private readonly List<Binding> _bindings = new();
        private AcpSide _subscribedSide;

        public VoiceMeeterChannelBinder(AudioController controller)
        {
            _controller = controller;
        }

        public virtual void Bind()
        {
            Unbind();

            var audio = _controller.AudioInterface;
            if (audio == null) return;

            _subscribedSide = _controller.Config.AudioAcpSide;
            int side = (int)_subscribedSide;

            foreach (var mapping in _controller.Config.VoiceMeeterMappings)
            {
                var b = new Binding(mapping);
                string channelName = mapping.Channel.ToString();

                b.VolumeHandler = audio.SubscribeToVolume(channelName, side, raw => OnVolume(b, raw));
                b.MuteHandler = audio.SubscribeToMute(channelName, side, unmuted => OnMute(b, unmuted));

                // Seed current state so the strip reflects the live knob /
                // latch position immediately, not on the next change.
                try
                {
                    OnVolume(b, audio.ReadVolume(channelName, side));
                    OnMute(b, audio.ReadMute(channelName, side));
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"VoiceMeeterChannelBinder seed failed for {mapping}");
                }

                _bindings.Add(b);
            }

            Logger.Information($"VoiceMeeter: bound {_bindings.Count} channel mapping(s) on ACP{(side + 1)}");
        }

        public virtual void Unbind()
        {
            var audio = _controller.AudioInterface;
            if (audio == null) { _bindings.Clear(); return; }

            int side = (int)_subscribedSide;
            foreach (var b in _bindings)
            {
                string channelName = b.Mapping.Channel.ToString();
                try { if (b.VolumeHandler != null) audio.UnsubscribeVolume(channelName, side, b.VolumeHandler); } catch { }
                try { if (b.MuteHandler != null) audio.UnsubscribeMute(channelName, side, b.MuteHandler); } catch { }
            }
            _bindings.Clear();
        }

        // Restore every configured strip/bus to 0 dB unmuted before we hand
        // audio control back to CoreAudio. Without this, a knob set to -20dB
        // before the user switched to CoreAudio leaves the strip attenuated,
        // and audio routed via VoiceMeeter at the OS level keeps sounding
        // quiet even though Prosim2GSX is no longer touching VoiceMeeter.
        public virtual void ResetStripsToNeutral()
        {
            var vm = _controller.VoiceMeeter;
            if (vm == null || !vm.IsAvailable) return;

            foreach (var mapping in _controller.Config.VoiceMeeterMappings)
            {
                try
                {
                    vm.SetStripGainDb(mapping.StripIndex, mapping.IsBus, 0f);
                    if (mapping.UseLatch)
                        vm.SetStripMute(mapping.StripIndex, mapping.IsBus, false);
                }
                catch (Exception ex) { Logger.Verbose($"VoiceMeeter reset failed for {mapping}: {ex.GetType().Name}"); }
            }
            Logger.Information("VoiceMeeter strips restored to 0 dB unmuted (backend handed back to CoreAudio).");
        }

        private void OnVolume(Binding b, float raw)
        {
            if (!_controller.IsPlanePowered) return;
            var vm = _controller.VoiceMeeter;
            if (vm == null || !vm.IsAvailable) return;

            float v = raw / (float)ProsimAudioInterface.VolumeMax;
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            vm.SetStripVolume(b.Mapping.StripIndex, b.Mapping.IsBus, v);
        }

        private void OnMute(Binding b, bool unmuted)
        {
            if (!b.Mapping.UseLatch) return;
            if (!_controller.IsPlanePowered) return;
            var vm = _controller.VoiceMeeter;
            if (vm == null || !vm.IsAvailable) return;
            vm.SetStripMute(b.Mapping.StripIndex, b.Mapping.IsBus, !unmuted);
        }

        private sealed class Binding
        {
            public Binding(VoiceMeeterMapping mapping) { Mapping = mapping; }
            public VoiceMeeterMapping Mapping { get; }
            public Action<string, dynamic, dynamic> VolumeHandler { get; set; }
            public Action<string, dynamic, dynamic> MuteHandler { get; set; }
        }
    }
}
