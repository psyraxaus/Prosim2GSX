using CFIT.AppLogger;
using ProsimInterface;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Audio
{
    // Per-ACP electrical power evaluator for the VoiceMeeter binder.
    //
    // Bus assignments per A320 schematic:
    //   ACP1 (CPT): AC ESS primary, DC ESS backup. Offline when AUDIO SWITCHING
    //               is set to CAPT (0) — captain swapped to ACP3.
    //   ACP2 (FO):  AC ESS primary, DC ESS backup. Offline when AUDIO SWITCHING
    //               is set to F/O (2) — F/O swapped to ACP3.
    //   ACP3 (OBS): DC BUS 1. No audio-switching modifier (it's the swap target,
    //               not the source).
    //
    // Subscribes to the three bus gates + the audio-switching selector at
    // Subscribe(); the cached values feed IsAcpPowered(). On any change the
    // gate re-evaluates every ACP and notifies the binder of transitions
    // (one log line per ACP per transition with a human-readable reason).
    internal class AcpPowerGate
    {
        private readonly ProsimAudioInterface _audio;
        private readonly ProsimSdkInterface _sdk;
        private readonly Action<AcpSide, bool, string> _onTransition;

        private bool _acEss;
        private bool _dcEss;
        private bool _dc1;
        // 0:CAPT, 1:NORM, 2:F/O. Default 1 (NORM) so an absent / unreadable
        // dataref doesn't gate out ACP1 or ACP2 spuriously.
        private int _audioSwitching = 1;

        private Action<string, dynamic, dynamic> _acEssHandler;
        private Action<string, dynamic, dynamic> _dcEssHandler;
        private Action<string, dynamic, dynamic> _dc1Handler;
        private Action<string, dynamic, dynamic> _switchingHandler;

        // Per-ACP last-logged powered state. null = never observed; the first
        // ReEvaluate after Subscribe always emits a transition log so the
        // initial state is visible.
        private readonly Dictionary<AcpSide, bool?> _lastLogged = new()
        {
            [AcpSide.CPT] = null,
            [AcpSide.FO] = null,
            [AcpSide.OBS] = null,
        };

        public AcpPowerGate(ProsimAudioInterface audio, ProsimSdkInterface sdk, Action<AcpSide, bool, string> onTransition)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
            _onTransition = onTransition ?? throw new ArgumentNullException(nameof(onTransition));
        }

        public bool IsAcpPowered(AcpSide acp) => acp switch
        {
            AcpSide.CPT => (_acEss || _dcEss) && _audioSwitching != 0,
            AcpSide.FO  => (_acEss || _dcEss) && _audioSwitching != 2,
            AcpSide.OBS => _dc1,
            _ => false,
        };

        public void Subscribe()
        {
            // Seed cached values so the first IsAcpPowered call after Subscribe
            // reflects current bus state, not the field defaults. If a read
            // throws (SDK not ready yet), the seed stays at the default and
            // the next dataref-change callback corrects it.
            try { _acEss = _sdk.GetBool(ProsimConstants.RefElecBusPowerAcEss); } catch { }
            try { _dcEss = _sdk.GetBool(ProsimConstants.RefElecBusPowerDcEss); } catch { }
            try { _dc1 = _sdk.GetBool(ProsimConstants.RefElecBusPowerDc1); } catch { }
            try { _audioSwitching = _sdk.GetInt(ProsimConstants.RefAudioSwitching, 1); } catch { }

            _acEssHandler = _audio.SubscribeToPower(ProsimConstants.RefElecBusPowerAcEss, v => { _acEss = v; ReEvaluate(); });
            _dcEssHandler = _audio.SubscribeToPower(ProsimConstants.RefElecBusPowerDcEss, v => { _dcEss = v; ReEvaluate(); });
            _dc1Handler   = _audio.SubscribeToPower(ProsimConstants.RefElecBusPowerDc1,   v => { _dc1 = v;   ReEvaluate(); });

            // AUDIO SWITCHING is an int — no SubscribeToInt in the SDK, so go
            // direct via SdkInterface.Subscribe and re-read the value inside
            // the handler. Same pattern SubscribeToPower uses internally.
            _switchingHandler = (dr, oldVal, newVal) =>
            {
                try { _audioSwitching = _sdk.GetInt(ProsimConstants.RefAudioSwitching, 1); }
                catch (Exception ex) { Logger.LogException(ex, "AcpPowerGate audio-switching read"); return; }
                ReEvaluate();
            };
            _sdk.Subscribe(ProsimConstants.RefAudioSwitching, _switchingHandler);

            // Seed _lastLogged so subsequent dataref changes only emit
            // transition logs for actual state flips. The binder is
            // responsible for logging the initial state per active ACP
            // (it knows which ACPs we care about; the gate doesn't).
            foreach (AcpSide acp in new[] { AcpSide.CPT, AcpSide.FO, AcpSide.OBS })
                _lastLogged[acp] = IsAcpPowered(acp);
        }

        public void Unsubscribe()
        {
            try { if (_acEssHandler != null) _audio.UnsubscribePower(ProsimConstants.RefElecBusPowerAcEss, _acEssHandler); } catch { }
            try { if (_dcEssHandler != null) _audio.UnsubscribePower(ProsimConstants.RefElecBusPowerDcEss, _dcEssHandler); } catch { }
            try { if (_dc1Handler != null)   _audio.UnsubscribePower(ProsimConstants.RefElecBusPowerDc1,   _dc1Handler); } catch { }
            try { if (_switchingHandler != null) _sdk.Unsubscribe(ProsimConstants.RefAudioSwitching, _switchingHandler); } catch { }
            _acEssHandler = null;
            _dcEssHandler = null;
            _dc1Handler = null;
            _switchingHandler = null;
        }

        private void ReEvaluate()
        {
            foreach (AcpSide acp in new[] { AcpSide.CPT, AcpSide.FO, AcpSide.OBS })
            {
                bool now = IsAcpPowered(acp);
                if (_lastLogged[acp] == now) continue;
                _lastLogged[acp] = now;
                _onTransition(acp, now, BuildReason(acp, now));
            }
        }

        private string BuildReason(AcpSide acp, bool powered)
        {
            if (acp == AcpSide.OBS)
                return powered ? "DC 1 powered" : "DC 1 unpowered";

            bool busOk = _acEss || _dcEss;
            bool switchingOk = acp == AcpSide.CPT ? _audioSwitching != 0 : _audioSwitching != 2;

            if (powered)
                return $"AC ESS={_acEss}, DC ESS={_dcEss}, AUDIO SWITCHING={SwitchingName(_audioSwitching)}";

            if (!busOk && !switchingOk)
                return $"AC ESS+DC ESS lost AND AUDIO SWITCHING={SwitchingName(_audioSwitching)}";
            if (!busOk)
                return "AC ESS+DC ESS lost";
            return $"AUDIO SWITCHING={SwitchingName(_audioSwitching)}";
        }

        private static string SwitchingName(int v) => v switch
        {
            0 => "CAPT",
            1 => "NORM",
            2 => "F/O",
            _ => $"?{v}",
        };
    }
}
