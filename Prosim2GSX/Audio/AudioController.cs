using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.SimConnectLib;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Prosim;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Audio
{
    public enum AudioChannel
    {
        VHF1,
        VHF2,
        VHF3,
        HF1,
        HF2,
        INT,
        CAB,
        PA
    }

    public enum AcpSide
    {
        CPT = 0,
        FO = 1
    }

    public class AudioController : ServiceController<Prosim2GSX, AppService, Config, Definition>
    {
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        public virtual SimConnectManager SimConnect => Prosim2GSX.Instance.AppService.SimConnect;
        public virtual ProsimSdkService ProsimService => AppService.Instance.ProsimService;
        public virtual ProsimAudioInterface AudioInterface => ProsimService?.AudioInterface;

        public virtual bool IsActive { get; protected set; } = false;
        public virtual bool IsPlanePowered { get; protected set; } = false;
        public virtual bool HasInitialized { get; protected set; } = false;
        public virtual DeviceManager DeviceManager { get; }
        public virtual SessionManager SessionManager { get; }
        public virtual AudioSessionRegistry SessionRegistry { get; }
        public virtual VoiceMeeterService VoiceMeeter { get; }
        public virtual VoiceMeeterChannelBinder VoiceMeeterBinder { get; }
        // Tracks the last-known backend / mappings revision so DoRun can
        // detect a UI-driven change and re-bind without restarting the loop.
        public virtual bool ResetVoiceMeeterBindings { get; set; } = false;
        protected virtual DateTime NextProcessCheck { get; set; } = DateTime.MinValue;
        public virtual bool ResetVolumes { get; set; } = false;
        public virtual bool ResetMappings { get; set; } = false;

        protected virtual Action<string, dynamic, dynamic> PowerHandler { get; set; }

        public AudioController(Config config) : base(config)
        {
            DeviceManager = new(this);
            SessionManager = new(this);
            SessionRegistry = new(this);
            VoiceMeeter = new();
            VoiceMeeterBinder = new(this);
        }

        protected override Task InitReceivers()
        {
            base.InitReceivers();
            return Task.CompletedTask;
        }

        protected override Task FreeResources()
        {
            base.FreeResources();
            UnsubscribePower();
            DeviceManager.Clear();
            return Task.CompletedTask;
        }

        protected virtual void SubscribePower()
        {
            var audio = AudioInterface;
            if (audio == null)
            {
                Logger.Warning("AudioInterface not available — cannot subscribe to power gate");
                return;
            }

            try
            {
                var sdk = ProsimService?.AircraftInterface?.SdkInterface;
                if (sdk != null)
                    IsPlanePowered = sdk.GetBool(ProsimConstants.RefElecBusPowerDcEss);
            }
            catch { /* SDK may not be ready yet — first callback will seed it */ }

            PowerHandler = audio.SubscribeToPower(ProsimConstants.RefElecBusPowerDcEss, value =>
            {
                IsPlanePowered = value;
                Logger.Debug($"Audio power gate -> {value}");
            });
        }

        protected virtual void UnsubscribePower()
        {
            var audio = AudioInterface;
            if (audio != null && PowerHandler != null)
            {
                try { audio.UnsubscribePower(ProsimConstants.RefElecBusPowerDcEss, PowerHandler); } catch { }
            }
            PowerHandler = null;
        }

        protected override async Task DoRun()
        {
            // Wait for the ProSim SDK to publish its audio interface — it's
            // created in ProsimSdkService.SetAircraftInterface, which fires
            // after the GsxController spins up.
            while (AudioInterface == null && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                await Task.Delay(Config.AudioServiceRunInterval, RequestToken);

            SubscribePower();

            while (!IsPlanePowered && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                await Task.Delay(Config.AudioServiceRunInterval, RequestToken);

            Logger.Debug($"Aircraft powered. AudioService active");
            try
            {
                bool rescanNeeded = false;
                bool lastBackendVm = Config.UseVoiceMeeter;

                // Initial backend setup. Mappings registration / binder bind
                // are mutually exclusive — we only ever run one route at a time.
                if (lastBackendVm)
                {
                    VoiceMeeter.Login(Config.VoiceMeeterDllPath);
                    VoiceMeeterBinder.Bind();
                }
                else
                {
                    SessionManager.RegisterMappings();
                }

                IsActive = true;
                while (SimConnect.IsSessionRunning && IsExecutionAllowed && !Token.IsCancellationRequested)
                {
                    // Live backend transitions. Detect a flip on Config and
                    // swap routes cleanly without restarting the loop.
                    if (Config.UseVoiceMeeter != lastBackendVm)
                    {
                        if (Config.UseVoiceMeeter)
                        {
                            // CoreAudio → VoiceMeeter
                            SessionManager.UnregisterMappings();
                            VoiceMeeter.Login(Config.VoiceMeeterDllPath);
                            VoiceMeeterBinder.Bind();
                        }
                        else
                        {
                            // VoiceMeeter → CoreAudio. Reset configured strips
                            // to 0 dB unmuted BEFORE suspending so the user's
                            // audio chain isn't left attenuated by stale VM
                            // state. We Suspend (not Logout) — the DLL stays
                            // loaded for the lifetime of the audio service so
                            // a subsequent CoreAudio → VM transition doesn't
                            // round-trip VBVMR_Login (flaky on some VM versions).
                            VoiceMeeterBinder.Unbind();
                            VoiceMeeterBinder.ResetStripsToNeutral();
                            VoiceMeeter.SuspendWrites();
                            SessionManager.RegisterMappings();
                        }
                        lastBackendVm = Config.UseVoiceMeeter;
                    }

                    if (Config.UseVoiceMeeter)
                    {
                        // VoiceMeeter mode — light loop. Login is idempotent
                        // (handles the user setting the DLL path post-startup);
                        // ResetVoiceMeeterBindings handles ACP-side or
                        // mappings-list edits from the UI.
                        if (!VoiceMeeter.IsAvailable && !string.IsNullOrWhiteSpace(Config.VoiceMeeterDllPath))
                            VoiceMeeter.Login(Config.VoiceMeeterDllPath);

                        if (ResetVoiceMeeterBindings)
                        {
                            VoiceMeeterBinder.Bind();
                            ResetVoiceMeeterBindings = false;
                        }

                        HasInitialized = true;
                        await Task.Delay(Config.AudioServiceRunInterval, RequestToken);
                        continue;
                    }

                    // CoreAudio mode — original loop body.
                    rescanNeeded = SessionManager.HasInactiveSessions || SessionManager.HasEmptySearches || ResetMappings;
                    if (rescanNeeded)
                        Logger.Debug($"Rescan Needed - InactiveSessions {SessionManager.HasInactiveSessions} | EmptySearches {SessionManager.HasEmptySearches} | ResetMappings {ResetMappings}");

                    if (ResetMappings)
                    {
                        SessionManager.UnregisterMappings();
                        SessionManager.RegisterMappings();
                        ResetMappings = false;
                    }

                    if (rescanNeeded || NextProcessCheck <= DateTime.Now)
                    {
                        if (SessionManager.CheckProcesses(rescanNeeded))
                        {
                            if (!rescanNeeded)
                                Logger.Debug($"Rescan Needed - CheckProcess had Changes");
                            rescanNeeded = true;
                            await Task.Delay(Config.AudioProcessStartupDelay, Token);
                        }
                        NextProcessCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.AudioProcessCheckInterval);
                    }

                    if (DeviceManager.Scan(SessionManager.HasEmptySearches))
                        rescanNeeded = true;
                    if (rescanNeeded)
                        Logger.Debug($"Rescan Needed - DeviceEnum");

                    HasInitialized = true;

                    SessionManager.CheckSessions(rescanNeeded);
                    if (ResetVolumes)
                        SessionManager.SynchControls();

                    // Reuses Devices populated by Scan() above — the registry
                    // walks the same Sessions collections, so this adds one
                    // process-name lookup per unique PID and no extra COM.
                    SessionRegistry.Refresh();

                    ResetVolumes = false;
                    rescanNeeded = false;
                    await Task.Delay(Config.AudioServiceRunInterval, RequestToken);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            IsActive = false;
            try { VoiceMeeterBinder?.Unbind(); } catch { }
            try { VoiceMeeterBinder?.ResetStripsToNeutral(); } catch { }
            SessionManager.UnregisterMappings();
            UnsubscribePower();

            Logger.Debug($"AudioService ended");
        }

        public override Task Stop()
        {
            base.Stop();

            try { SessionManager.RestoreVolumes(); } catch { }
            UnsubscribePower();
            IsActive = false;
            HasInitialized = false;
            DeviceManager.Clear();
            SessionManager.Clear();
            SessionRegistry.Clear();
            try { VoiceMeeterBinder?.Unbind(); } catch { }
            try { VoiceMeeterBinder?.ResetStripsToNeutral(); } catch { }
            try { VoiceMeeter?.Logout(); } catch { }
            return Task.CompletedTask;
        }
    }
}
