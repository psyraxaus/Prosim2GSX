using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using Prosim2GSX.AppConfig;
using Prosim2GSX.GSX;
using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Prosim2GSX.Aircraft
{
    public class AircraftInterface
    {
        protected virtual GsxController Controller { get; }
        protected virtual SimConnectManager SimConnect => Prosim2GSX.Instance.AppService.SimConnect;
        public virtual Config Config => Controller.Config;
        public virtual AircraftProfile Profile => Controller.AircraftProfile;
        protected virtual SimStore SimStore => Controller.SimStore;
        protected virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices => Controller.GsxServices;
        public virtual ProsimAircraftInterface ProsimInterface { get; }
        public virtual bool IsInitialized { get; protected set; } = false;
        
        protected virtual ISimResourceSubscription SubAirline { get; set; }
        protected virtual ISimResourceSubscription SubTitle { get; set; }
        protected virtual ISimResourceSubscription SubLivery { get; set; }
        protected virtual ISimResourceSubscription SubZuluTime { get; set; }

        public virtual string Airline => SubAirline?.GetString();
        public virtual string Title => !string.IsNullOrWhiteSpace(SubLivery?.GetString()) ? SubLivery.GetString() : SubTitle?.GetString() ?? "";
        public virtual string Registration => ProsimInterface.Registration;
        public virtual bool IsFlightPlanLoaded => ProsimInterface.IsFlightPlanLoaded;
        public virtual bool IsLoaded => ProsimInterface.IsLoaded;
        public virtual bool IsRefueling => ProsimInterface.IsRefueling;
        public virtual DisplayUnit UnitAircraft => ProsimInterface.UnitAircraft;
        public virtual string SimbriefUser => ProsimInterface.SimbriefUser;
        public virtual TimeSpan FlightDuration => ProsimInterface.FlightDuration;
        public virtual bool IsEfbBoardingCompleted => !string.IsNullOrWhiteSpace(EfbBoardingState) && (EfbBoardingState?.Equals("ended", System.StringComparison.InvariantCultureIgnoreCase) == true || EfbBoardingState?.Equals("completed", System.StringComparison.InvariantCultureIgnoreCase) == true);
        public virtual string EfbBoardingState => ProsimInterface?.EfbBoardingState;
        public virtual double FuelCurrent => ProsimInterface.FuelCurrent;
        public virtual double FuelTarget => ProsimInterface.FuelTarget;
        public virtual bool SmartButtonRequest => ProsimInterface.SmartButtonRequest;
        public virtual int GroundSpeed => (int)ProsimInterface.GetGroundSpeed();
        public virtual bool IsOnGround => ProsimInterface.GetOnGround();
        public virtual bool EquipmentGpu => ProsimInterface.GetGpuState();
        public virtual bool EquipmentPca => ProsimInterface.GetPcaState();
        public virtual bool EquipmentChocks => ProsimInterface.GetChocksState();
        public virtual bool EnginesRunning => ProsimInterface.GetEnginesRunning();
        public virtual bool IsFinalReceived => ProsimInterface.GetFinalReceived();
        public virtual bool IsExternalPowerConnected => ProsimInterface.GetExternalPowerConnected();
        public virtual bool IsApuRunning => ProsimInterface.IsApuRunning;
        public virtual bool IsApuBleedOn => ProsimInterface.IsApuBleedOn;
        public virtual bool HasOpenDoors => ProsimInterface.GetOpenDoors();
        public virtual bool IsBrakeSet => ProsimInterface.GetBrake();
        public virtual bool LightNav => ProsimInterface.GetLightNav();
        public virtual bool LightBeacon => ProsimInterface.GetLightBeacon();
        public virtual string FlightNumber => ProsimInterface.FlightNumber ?? "";
        public virtual int ZuluTimeSeconds => (int)(SubZuluTime?.GetNumber() ?? 0);

        public AircraftInterface(GsxController controller)
        {
            Controller = controller;

            // Create the SDK interface with the path from config
            var sdkInterface = new ProsimSdkInterface(Controller.Config.ProSimSdkPath);

            // Pass both controller and SDK interface to ProsimAircraftInterface
            ProsimInterface = new ProsimAircraftInterface(Controller, sdkInterface);
        }

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                SubAirline = SimStore.AddVariable("ATC AIRLINE", SimUnitType.String);
                SubTitle = SimStore.AddVariable("TITLE", SimUnitType.String);
                if (Sys.GetProcessRunning(Config.BinaryMsfs2024))
                    SubLivery = SimStore.AddVariable("LIVERY NAME", SimUnitType.String);
                SubZuluTime = SimStore.AddVariable("ZULU TIME", SimUnitType.Seconds);

                SimStore.AddVariable(ProsimConstants.VarOhSigns, SimUnitType.Number);
                SimStore.AddVariable(ProsimConstants.VarOhPneumaticPack1, SimUnitType.Number);
                SimStore.AddVariable(ProsimConstants.VarOhPneumaticPack2, SimUnitType.Number);

                Controller.WalkaroundWasSkipped += OnWalkaroundWasSkipped;
                Controller.AutomationController.OnStateChange += OnAutomationState;
                Controller.GsxServices[GsxServiceType.Stairs].OnStateChanged += OnStairChange;
                (Controller.GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel).OnStateChanged += OnRefuelStateChanged;
                (Controller.GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel).OnHoseConnection += OnHoseChanged;
                (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnActive += OnBoardingActive;
                (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnCompleted += OnBoardingCompleted;
                (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnPaxChange += OnPaxChangeBoarding;
                (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnCargoChange += OnCargoChangeBoarding;
                (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnActive += OnDeboardingActive;
                (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnCompleted += OnDeboardingCompleted;
                (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnPaxChange += OnPaxChangeDeboarding;
                (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnCargoChange += OnCargoChangeDeboarding;

                Controller.MsgCouatlStarted.OnMessage += OnCouatlStarted;

                ProsimInterface.Init();

                IsInitialized = true;
            }
        }

        public virtual void FreeResources()
        {
            ProsimInterface.FreeResources();

            Controller.WalkaroundWasSkipped -= OnWalkaroundWasSkipped;
            Controller.AutomationController.OnStateChange -= OnAutomationState;
            Controller.GsxServices[GsxServiceType.Stairs].OnStateChanged -= OnStairChange;
            (Controller.GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel).OnStateChanged -= OnRefuelStateChanged;
            (Controller.GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel).OnHoseConnection -= OnHoseChanged;
            (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnActive -= OnBoardingActive;
            (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnCompleted -= OnBoardingCompleted;
            (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnPaxChange -= OnPaxChangeBoarding;
            (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnCargoChange -= OnCargoChangeBoarding;
            (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnActive -= OnDeboardingActive;
            (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnCompleted -= OnDeboardingCompleted;
            (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnPaxChange -= OnPaxChangeDeboarding;
            (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnCargoChange -= OnCargoChangeDeboarding;

            Controller.MsgCouatlStarted.OnMessage -= OnCouatlStarted;

            SimStore.Remove(ProsimConstants.VarOhSigns);
            SimStore.Remove(ProsimConstants.VarOhPneumaticPack1);
            SimStore.Remove(ProsimConstants.VarOhPneumaticPack2);

            SimStore.Remove("ATC AIRLINE");
            SimStore.Remove("TITLE");
            if (Sys.GetProcessRunning(Config.BinaryMsfs2024))
                SimStore.Remove("LIVERY NAME");
            SimStore.Remove("ZULU TIME");
        }

        public virtual void Run()
        {
            ProsimInterface.Run();
        }

        public virtual void Stop()
        {
            Reset();
            ProsimInterface.Stop();
        }

        public virtual async Task ResetSmartButton()
        {
            await ProsimInterface.ResetSmartButton();
        }

        public virtual void Reset()
        {
            ProsimInterface.SmartButtonRequest = false;
        }

        public virtual void ResetFlight()
        {
            ProsimInterface.ResetFlight();
        }

        public virtual async Task UnloadOfp(bool unloadOfp = true)
        {
            await ProsimInterface.UnloadOfp(unloadOfp);
        }

        protected virtual async Task OnWalkaroundWasSkipped()
        {
            await ProsimInterface.OnStatePreparation();
        }

        protected virtual void OnAutomationState(AutomationState state)
        {
            if (state == AutomationState.TaxiOut)
                ProsimInterface.OnStateTaxiOut();
            else if (state == AutomationState.TaxiIn)
                ProsimInterface.OnStateTaxiIn();
            else if (state == AutomationState.Arrival)
                ProsimInterface.OnStateArrival();
        }

        protected virtual async Task OnStairChange(GsxService service)
        {
            await ProsimInterface.OnStairChange((int)GsxServices[GsxServiceType.Stairs].State, (int)GsxServices[GsxServiceType.Jetway].State);
        }

        protected virtual async Task OnRefuelStateChanged(GsxService service)
        {
            if (!AppService.Instance.IsProsimAircraft)
                return;
            var serviceRefuel = service as GsxServiceRefuel;

            if (serviceRefuel.State == GsxServiceState.Active)
                await ProsimInterface.OnRefuelActive();
            else if (serviceRefuel.State == GsxServiceState.Completed)
            {
                if (ProsimInterface.IsRefueling)
                {
                    if (Profile.RefuelFinishOnHose)
                    {
                        Logger.Information($"GSX Refuel reported completed while Refueling - aborting Refuel Process");
                        await ProsimInterface.RefuelAbort();
                    }
                    else
                        Logger.Information($"GSX Refuel reported completed while Refueling - continuing Refuel Process");
                }
                else if (Controller.AutomationState < AutomationState.Departure)
                    await ProsimInterface.RefuelComplete();
            }
        }

        public virtual async Task OnHoseChanged(bool hoseConnected)
        {
            if (hoseConnected && !ProsimInterface.IsRefueling)
            {
                Logger.Information($"Fuel Hose connected - start Refuel Process");
                await ProsimInterface.SetRefuelPower(set: true);
                await ProsimInterface.RefuelStart();
            }
            
            if (!hoseConnected && ProsimInterface.IsRefueling)
            {
                if (Profile.RefuelFinishOnHose)
                {
                    Logger.Information($"GSX Fuelhose reported disconnected while Refueling - aborting Refuel Process");
                    await ProsimInterface.RefuelAbort();
                }
                else
                    Logger.Information($"GSX Fuelhose reported disconnected while Refueling - continuing Refuel Process");
            }
        }

        protected virtual void OnCouatlStarted(MsgGsxCouatlStarted msg)
        {
            try
            {
                if (!AppService.Instance.IsProsimAircraft || Controller.AutomationController.IsStarted)
                    return;

                var serviceBoarding = Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding;
                if (serviceBoarding.WasActive)
                    serviceBoarding.ForceComplete();

                var serviceRefuel = Controller.GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel;
                if (serviceRefuel.WasActive)
                    serviceRefuel.ForceComplete();
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        protected virtual async Task OnBoardingActive(GsxService service)
        {
            await ProsimInterface.BoardingStart();
        }

        public virtual async Task OnBoardingCompleted(GsxService service)
        {
            await ProsimInterface.BoardingStop();
        }

        protected virtual async void OnPaxChangeBoarding(GsxServiceBoarding service)
        {
            await ProsimInterface.OnPaxChangeBoarding(service.PaxTotal);
        }

        protected virtual async void OnCargoChangeBoarding(GsxServiceBoarding service)
        {
            await ProsimInterface.OnCargoChangeBoarding(service.CargoPercent);
        }

        protected virtual Task OnDeboardingActive(GsxService service)
        {
            ProsimInterface.OnDeboardingActive();
            return Task.CompletedTask;
        }

        public virtual async Task OnDeboardingCompleted(GsxService service)
        {
            await ProsimInterface.OnDeboardingCompleted();
        }

        protected virtual async void OnPaxChangeDeboarding(GsxServiceDeboarding serviceDeboarding)
        {
            await ProsimInterface.OnPaxChangeDeboarding(serviceDeboarding.PaxTotal);
        }

        protected virtual async void OnCargoChangeDeboarding(GsxServiceDeboarding service)
        {
            await ProsimInterface.OnCargoChangeDeboarding(service.CargoPercent);
        }

        public virtual async Task SetPca(bool set)
        {
            await ProsimInterface.SetPca(set);
        }

        public virtual async Task SetChocks(bool set, bool force = false)
        {
            await ProsimInterface.SetChocks(set, force);
        }

        public virtual async Task SetGroundPower(bool set, bool force = false)
        {
            await ProsimInterface.SetGroundPower(set, force);
        }

        public virtual async Task CloseAllDoors()
        {
            await ProsimInterface.CloseAllDoors();
        }

        public virtual int GetPaxBoarding()
        {
            return ProsimInterface.GetPaxBoarding();
        }

        public virtual int GetPaxDeboarding()
        {
            return ProsimInterface.GetPaxDeboarding();
        }

        public virtual async Task DingCabin()
        {
            await ProsimInterface.DingCabin();
        }

        public virtual async Task FlashMechCall()
        {
            await ProsimInterface.TriggerMechCall();
        }
    }
}
