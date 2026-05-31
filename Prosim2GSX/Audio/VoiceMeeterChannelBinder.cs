using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using ProsimInterface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.Audio
{
    // Drives VoiceMeeter strip/bus volume + mute from ProSim ACP datarefs
    // when Config.UseVoiceMeeter is true. Supports driving 1-2 ACPs
    // simultaneously per Config.ActiveAcps; mappings come from
    // Config.VoiceMeeterMappingsByAcp keyed by ACP.
    //
    // VBVMR_SetParameterFloat is sub-ms, so writes happen synchronously on
    // the SDK callback thread — no per-mapping worker / coalescer needed.
    // Per-ACP power gating goes through AcpPowerGate, which subscribes to
    // the three relevant bus datarefs (AC ESS, DC ESS, DC 1) plus the
    // S_AUDIO_SWITCHING selector and re-evaluates each ACP on any change.
    // Knob movement on an unpowered ACP is a no-op; the other ACP keeps
    // operating independently.
    //
    // On startup the binder validates the config (VoiceMeeterMappingValidator);
    // any conflict (duplicate channel within an ACP, or duplicate strip across
    // ACPs) triggers a fallback to ACP1-only with whatever entries ACP1 has,
    // and AudioController.VoiceMeeterFallbackReason is set so the UI can
    // surface the reason.
    public class VoiceMeeterChannelBinder
    {
        private readonly AudioController _controller;
        private readonly List<AcpBinding> _acpBindings = new();
        private AcpPowerGate _powerGate;

        public VoiceMeeterChannelBinder(AudioController controller)
        {
            _controller = controller;
        }

        public virtual void Bind()
        {
            Unbind();

            var audio = _controller.AudioInterface;
            if (audio == null) return;

            var sdk = _controller.ProsimService?.AircraftInterface?.SdkInterface;
            if (sdk == null)
            {
                Logger.Warning("VoiceMeeter: SDK interface not available — skipping bind");
                return;
            }

            // Validate first. On conflict: fall back to ACP1-only with ACP1's
            // mappings intact (ACP2/ACP3 entries dropped this session). The
            // user's config file is NOT modified — they fix it via UI / JSON
            // and the next rebind picks up the corrected state.
            var validation = VoiceMeeterMappingValidator.Validate(_controller.Config);
            var effectiveActiveAcps = new List<AcpSide>(_controller.Config.ActiveAcps ?? new List<AcpSide> { AcpSide.CPT });
            if (!validation.IsValid)
            {
                Logger.Warning($"VoiceMeeter mapping conflict — falling back to ACP1 only: {validation.Message}");
                _controller.VoiceMeeterFallbackReason = validation.Message;
                effectiveActiveAcps = new List<AcpSide> { AcpSide.CPT };
            }
            else
            {
                _controller.VoiceMeeterFallbackReason = null;
            }

            // Power gate spins up first so the initial per-ACP state log lands
            // before the per-mapping subscribe logs (cleaner read order).
            _powerGate = new AcpPowerGate(audio, sdk, OnAcpPowerTransition);
            _powerGate.Subscribe();

            int totalMappings = 0;
            foreach (var acp in effectiveActiveAcps)
            {
                if (!_controller.Config.VoiceMeeterMappingsByAcp.TryGetValue(acp, out var mappings) || mappings == null || mappings.Count == 0)
                {
                    Logger.Information($"VoiceMeeter: ACP{(int)acp + 1} ({acp}) active but has no mappings configured");
                    continue;
                }

                var acpBinding = new AcpBinding(acp);
                int side = (int)acp;
                foreach (var mapping in mappings)
                {
                    var b = new Binding(acpBinding, mapping);
                    string channelName = mapping.Channel.ToString();

                    b.VolumeHandler = audio.SubscribeToVolume(channelName, side, raw => OnVolume(b, raw));
                    b.MuteHandler = audio.SubscribeToMute(channelName, side, unmuted => OnMute(b, unmuted));

                    // Seed current state so the strip reflects the live knob /
                    // latch position immediately, not on the next change.
                    // OnVolume/OnMute internally honour the power gate, so a
                    // seed on an unpowered ACP is a no-op (matches the spec:
                    // "leave VoiceMeeter state unchanged when gate closed").
                    try
                    {
                        OnVolume(b, audio.ReadVolume(channelName, side));
                        OnMute(b, audio.ReadMute(channelName, side));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex, $"VoiceMeeterChannelBinder seed failed for ACP{side + 1}.{mapping}");
                    }

                    acpBinding.Bindings.Add(b);
                }

                _acpBindings.Add(acpBinding);
                totalMappings += acpBinding.Bindings.Count;

                var mapSummary = string.Join(", ", acpBinding.Bindings.Select(x => $"{x.Mapping.Channel}→{(x.Mapping.IsBus ? "Bus" : "Strip")} {x.Mapping.StripIndex + 1}"));
                bool initialPowered = _powerGate.IsAcpPowered(acp);
                Logger.Information($"VoiceMeeter: ACP{side + 1} ({acp}) bound [{(initialPowered ? "powered" : "unpowered")}] with {acpBinding.Bindings.Count} mapping(s): {mapSummary}");
            }

            Logger.Information($"VoiceMeeter: {totalMappings} total mapping(s) across {_acpBindings.Count} active ACP(s)");
        }

        public virtual void Unbind()
        {
            var audio = _controller.AudioInterface;

            if (audio != null)
            {
                foreach (var acpBinding in _acpBindings)
                {
                    int side = (int)acpBinding.Acp;
                    foreach (var b in acpBinding.Bindings)
                    {
                        string channelName = b.Mapping.Channel.ToString();
                        try { if (b.VolumeHandler != null) audio.UnsubscribeVolume(channelName, side, b.VolumeHandler); } catch { }
                        try { if (b.MuteHandler != null) audio.UnsubscribeMute(channelName, side, b.MuteHandler); } catch { }
                    }
                }
            }
            _acpBindings.Clear();

            try { _powerGate?.Unsubscribe(); } catch { }
            _powerGate = null;
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
            if (_acpBindings.Count == 0) return;

            foreach (var acpBinding in _acpBindings)
            {
                foreach (var b in acpBinding.Bindings)
                {
                    try
                    {
                        vm.SetStripGainDb(b.Mapping.StripIndex, b.Mapping.IsBus, 0f);
                        if (b.Mapping.UseLatch)
                            vm.SetStripMute(b.Mapping.StripIndex, b.Mapping.IsBus, false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Verbose($"VoiceMeeter reset failed for ACP{(int)acpBinding.Acp + 1}.{b.Mapping}: {ex.GetType().Name}");
                    }
                }
            }
            Logger.Information($"VoiceMeeter strips restored to 0 dB unmuted across {_acpBindings.Count} ACP(s) (backend handed back to CoreAudio).");
        }

        private void OnVolume(Binding b, float raw)
        {
            var vm = _controller.VoiceMeeter;
            if (vm == null || !vm.IsAvailable) return;
            if (_powerGate == null || !_powerGate.IsAcpPowered(b.Owner.Acp)) return;

            float v = raw / (float)ProsimAudioInterface.VolumeMax;
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            vm.SetStripVolume(b.Mapping.StripIndex, b.Mapping.IsBus, v);
        }

        private void OnMute(Binding b, bool unmuted)
        {
            if (!b.Mapping.UseLatch) return;
            var vm = _controller.VoiceMeeter;
            if (vm == null || !vm.IsAvailable) return;
            if (_powerGate == null || !_powerGate.IsAcpPowered(b.Owner.Acp)) return;

            vm.SetStripMute(b.Mapping.StripIndex, b.Mapping.IsBus, !unmuted);
        }

        private void OnAcpPowerTransition(AcpSide acp, bool powered, string reason)
        {
            // Only log transitions for ACPs we're actually driving — otherwise
            // every bind cycle would emit three "ACP3 unpowered" lines for
            // people running ACP1+ACP2.
            if (!_acpBindings.Any(b => b.Acp == acp)) return;
            Logger.Information($"VoiceMeeter: ACP{(int)acp + 1} ({acp}) {(powered ? "powered" : "unpowered")} — {reason}");
        }

        private sealed class AcpBinding
        {
            public AcpSide Acp { get; }
            public List<Binding> Bindings { get; } = new();
            public AcpBinding(AcpSide acp) { Acp = acp; }
        }

        private sealed class Binding
        {
            public AcpBinding Owner { get; }
            public VoiceMeeterMapping Mapping { get; }
            public Action<string, dynamic, dynamic> VolumeHandler { get; set; }
            public Action<string, dynamic, dynamic> MuteHandler { get; set; }
            public Binding(AcpBinding owner, VoiceMeeterMapping mapping)
            {
                Owner = owner;
                Mapping = mapping;
            }
        }
    }
}
