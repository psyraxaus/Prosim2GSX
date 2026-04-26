using CFIT.AppFramework.Messages;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;
using Prosim2GSX.GSX;
using Prosim2GSX.Prosim;
using Prosim2GSX.SayIntentions;
using Prosim2GSX.State;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX
{
    public enum AppResetRequest
    {
        None = 0,
        App = 1,
        AppGsx = 2,
    }

    public class AppService : AppService<Prosim2GSX, AppService, Config, Definition>
    {
        public virtual CancellationTokenSource RequestTokenSource { get; protected set; }
        public virtual CancellationToken RequestToken { get; protected set; }
        public virtual ProsimSdkService ProsimService { get; protected set; }
        public virtual GsxController GsxService { get; protected set; }
        public virtual AudioController AudioService { get; protected set; }
        public virtual ISayIntentionsService SayIntentionsService { get; protected set; } = new SayIntentionsService();
        public virtual AppResetRequest ResetRequested {  get; set; } = AppResetRequest.None;
        public virtual bool IsSessionInitializing { get; protected set; } = false;
        public virtual bool IsSessionInitialized { get; protected set; } = false;
        public virtual bool SessionStopRequested { get; protected set; } = false;
        public virtual bool IsProsimAircraft => SimConnect.AircraftString.Contains(Config.ProsimAircraftString, StringComparison.InvariantCultureIgnoreCase);

        // Long-lived observable state stores. Populated by services/workers and
        // observed by both the WPF Models and the future web/WebSocket layer.
        // Constructed eagerly so they outlive any individual tab/view-model.
        public virtual FlightStatusState FlightStatus { get; } = new();
        public virtual GsxState Gsx { get; } = new();
        public virtual AudioState Audio { get; } = new();
        // Settings is an alias for the existing Config singleton — Config already
        // implements INotifyPropertyChanged and persists itself, so it serves as
        // the AppSettingsState surface unchanged.
        public virtual Config Settings => Config;

        public AppService(Config config) : base(config)
        {
            RefreshToken();
        }

        protected virtual void RefreshToken()
        {
            RequestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Prosim2GSX.Instance.Token);
            RequestToken = RequestTokenSource.Token;
        }

        protected override void CreateServiceControllers()
        {
            if (Prosim2GSX.Instance.IsSdkAvailable)
            {
                try
                {
                    ProsimService = new ProsimSdkService(Config);
                    GsxService = new GsxController(Config);
                    AudioService = new AudioController(Config);
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to create service controllers — running in degraded mode");
                    Logger.LogException(ex);
                    ProsimService = null;
                    GsxService = null;
                    AudioService = null;
                    Prosim2GSX.Instance.IsSdkAvailable = false;
                }
            }
            else
            {
                Logger.Warning("ProSim SDK not available — services will not be created. Configure SDK path in Settings and restart.");
                ProsimService = null;
                GsxService = null;
                AudioService = null;
            }
        }

        protected override Task InitReceivers()
        {
            base.InitReceivers();
            ReceiverStore.Add<MsgSessionReady>().OnMessage += OnSessionReady;
            ReceiverStore.Add<MsgSessionEnded>().OnMessage += OnSessionEnded;
            return Task.CompletedTask;
        }

        protected virtual void OnSessionEnded(MsgSessionEnded obj)
        {
            SessionStopRequested = true;

            try
            {
                Logger.Debug($"Cancel Request Token");
                RequestTokenSource.Cancel();

                if (GsxService?.IsActive == true)
                {
                    Logger.Debug($"Stop GsxService");
                    GsxService.Stop();
                }

                if (AudioService?.IsActive == true)
                {
                    Logger.Debug($"Stop AudioService");
                    AudioService.Stop();
                }

                Config.SetDisplayUnit(Config.DisplayUnitDefault);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            IsSessionInitialized = false;
        }

        protected virtual void OnSessionReady(MsgSessionReady obj)
        {
            if (!IsProsimAircraft || IsSessionInitializing || IsSessionInitialized)
                return;

            if (!Prosim2GSX.Instance.IsSdkAvailable)
            {
                Logger.Warning("Session ready but ProSim SDK not available — skipping service initialization");
                return;
            }

            IsSessionInitializing = true;
            SessionStopRequested = false;

            try
            {
                Logger.Debug($"Refresh Token");
                RefreshToken();

                if (App.Config.RunGsxService && GsxService != null)
                {
                    Logger.Debug($"Start GsxService");
                    GsxService.Start();
                }

                if (App.Config.RunAudioService && AudioService != null)
                {
                    Logger.Debug($"Start AudioService");
                    AudioService.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            IsSessionInitialized = true;
            IsSessionInitializing = false;
        }

        public virtual async Task RestartGsx()
        {
            if (GsxService == null)
            {
                Logger.Warning("Cannot restart GSX — service not available (SDK not configured)");
                return;
            }

            Logger.Debug($"Kill Couatl Process");
            Sys.KillProcess(App.Config.BinaryGsx2020);
            Sys.KillProcess(App.Config.BinaryGsx2024);

            Logger.Debug($"Wait for Binary Start ({Config.DelayGsxBinaryStart}ms) ...");
            await Task.Delay(Config.DelayGsxBinaryStart, Token);

            if (SimService.Manager.GetSimVersion() == SimVersion.MSFS2020 && !Sys.GetProcessRunning(App.Config.BinaryGsx2020))
            {
                Logger.Debug($"Starting Process {App.Config.BinaryGsx2020}");
                string dir = Path.Join(GsxService.PathInstallation, "couatl64");
                Sys.StartProcess(Path.Join(dir, $"{App.Config.BinaryGsx2020}.exe"), dir);
            }

            if (SimService.Manager.GetSimVersion() == SimVersion.MSFS2024 && !Sys.GetProcessRunning(App.Config.BinaryGsx2024))
            {
                Logger.Debug($"Starting Process {App.Config.BinaryGsx2024}");
                string dir = Path.Join(GsxService.PathInstallation, "couatl64");
                Sys.StartProcess(Path.Join(dir, $"{App.Config.BinaryGsx2024}.exe"), dir);
            }

            await Task.Delay(Config.DelayGsxBinaryStart, Token);
        }

        protected override async Task MainLoop()
        {
            await Task.Delay(App.Config.TimerGsxCheck, Token);

            if (ResetRequested > AppResetRequest.None)
            {
                Logger.Debug($"Reset was requested: {ResetRequested}");
                OnSessionEnded(null);
                if (ResetRequested == AppResetRequest.App)
                    await Task.Delay(2500, Token);
                else
                    await RestartGsx();
                OnSessionReady(null);
                ResetRequested = AppResetRequest.None;
            }
        }

        protected override Task FreeResources()
        {
            base.FreeResources();
            ReceiverStore.Remove<MsgSessionReady>().OnMessage -= OnSessionReady;
            ReceiverStore.Remove<MsgSessionEnded>().OnMessage -= OnSessionEnded;
            return Task.CompletedTask;
        }
    }
}
