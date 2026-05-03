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
        protected virtual DateTime NextProcessCheck { get; set; } = DateTime.MinValue;
        public virtual bool ResetVolumes { get; set; } = false;
        public virtual bool ResetMappings { get; set; } = false;

        protected virtual Action<string, dynamic, dynamic> PowerHandler { get; set; }

        public AudioController(Config config) : base(config)
        {
            DeviceManager = new(this);
            SessionManager = new(this);
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
                SessionManager.RegisterMappings();
                bool rescanNeeded = false;
                IsActive = true;
                while (SimConnect.IsSessionRunning && IsExecutionAllowed && !Token.IsCancellationRequested)
                {
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
            return Task.CompletedTask;
        }
    }
}
