using Prosim2GSX.AppConfig;
using Prosim2GSX.GSX;
using ProsimInterface;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.Web.Contracts
{
    // GSX Settings (Automation) tab content. Mirrors AircraftProfile + the
    // app-wide auto-deice settings on Config. Read+write — POST /api/gsxsettings
    // hands a populated DTO to ApplyTo which mutates the active profile and
    // persists.
    //
    // Threading: ApplyTo writes to AircraftProfile and Config and calls
    // SaveConfiguration. Phase 6 controllers MUST marshal ApplyTo onto the WPF
    // dispatcher because Config raises INPC events on the calling thread and
    // existing WPF bindings expect those on the UI thread.
    public class GsxSettingsDto
    {
        // Profile identity (read-only on the web — profile management is a
        // future feature, not in scope for the initial web UI).
        public string ProfileName { get; set; } = "";

        // Doors / gate
        public bool DoorStairHandling { get; set; }
        public bool DoorCargoHandling { get; set; }
        public bool DoorCateringHandling { get; set; }
        public bool DoorOpenBoardActive { get; set; }
        public bool DoorsCargoKeepOpenOnLoaded { get; set; }
        public bool DoorsCargoKeepOpenOnUnloaded { get; set; }
        public bool CloseDoorsOnFinal { get; set; }

        // Jetway / stairs
        public bool CallJetwayStairsOnPrep { get; set; }
        public bool CallJetwayStairsDuringDeparture { get; set; }
        public bool CallJetwayStairsOnArrival { get; set; }
        public int RemoveStairsAfterDepature { get; set; }
        public bool RemoveJetwayStairsOnFinal { get; set; }

        // Ground equipment
        public bool PlaceProsimStairsWalkaround { get; set; }
        public bool ClearGroundEquipOnBeacon { get; set; }
        public bool GradualGroundEquipRemoval { get; set; }
        public bool ConnectGpuWithApuRunning { get; set; }
        public int ConnectPca { get; set; }
        public bool PcaOverride { get; set; }
        public int ChockDelayMin { get; set; }
        public int ChockDelayMax { get; set; }

        // GSX services
        public bool CallReposition { get; set; }
        public bool CallDeboardOnArrival { get; set; }
        public bool RunDepartureDuringDeboarding { get; set; }
        public bool ChimeOnParked { get; set; }
        public bool ChimeOnDeboardComplete { get; set; }
        public List<ServiceConfigDto> DepartureServices { get; set; } = new();

        // Refuel
        public RefuelMethod RefuelMethod { get; set; } = RefuelMethod.FixedRate;
        public double RefuelRateKgSec { get; set; }
        public int RefuelTimeTargetSeconds { get; set; }
        public bool SkipFuelOnTankering { get; set; }
        public bool RefuelFinishOnHose { get; set; }

        // Pushback / tug
        public int AttachTugDuringBoarding { get; set; }
        public int CallPushbackWhenTugAttached { get; set; }
        public bool CallPushbackOnBeacon { get; set; }

        // Beacon-orchestrated departure sequence
        public bool SequenceOnBeacon { get; set; }
        public int SeqDoorsCloseDelayMin { get; set; }
        public int SeqDoorsCloseDelayMax { get; set; }
        public int SeqJetwayRetractDelayMin { get; set; }
        public int SeqJetwayRetractDelayMax { get; set; }
        public int SeqGpuDisconnectDelayMin { get; set; }
        public int SeqGpuDisconnectDelayMax { get; set; }

        // Operator / hub
        public bool OperatorAutoSelect { get; set; }
        public List<string> OperatorPreferences { get; set; } = new();
        public List<string> CompanyHubs { get; set; } = new();

        // Skip questions
        public bool SkipWalkAround { get; set; }
        public bool SkipCrewQuestion { get; set; }
        public bool SkipFollowMe { get; set; }
        public bool KeepDirectionMenuOpen { get; set; }
        public bool AnswerCabinCallGround { get; set; }
        public int DelayCabinCallGround { get; set; }
        public bool AnswerCabinCallAir { get; set; }
        public int DelayCabinCallAir { get; set; }

        // Aircraft / OFP
        public int FinalDelayMin { get; set; }
        public int FinalDelayMax { get; set; }
        public bool FuelSaveLoadFob { get; set; }
        public bool RandomizePax { get; set; }
        public double ChancePerSeat { get; set; }

        // Auto-deice (app-wide on Config, not per-profile)
        public bool AutoDeiceEnabled { get; set; }
        public AutoDeiceFluid AutoDeiceFluid { get; set; } = AutoDeiceFluid.TypeIV100;

        public static GsxSettingsDto From(AppService app)
        {
            var profile = app.Config?.CurrentProfile;
            var config = app.Config;
            if (profile == null || config == null)
                return new GsxSettingsDto();

            return new GsxSettingsDto
            {
                ProfileName = profile.Name,

                DoorStairHandling = profile.DoorStairHandling,
                DoorCargoHandling = profile.DoorCargoHandling,
                DoorCateringHandling = profile.DoorCateringHandling,
                DoorOpenBoardActive = profile.DoorOpenBoardActive,
                DoorsCargoKeepOpenOnLoaded = profile.DoorsCargoKeepOpenOnLoaded,
                DoorsCargoKeepOpenOnUnloaded = profile.DoorsCargoKeepOpenOnUnloaded,
                CloseDoorsOnFinal = profile.CloseDoorsOnFinal,

                CallJetwayStairsOnPrep = profile.CallJetwayStairsOnPrep,
                CallJetwayStairsDuringDeparture = profile.CallJetwayStairsDuringDeparture,
                CallJetwayStairsOnArrival = profile.CallJetwayStairsOnArrival,
                RemoveStairsAfterDepature = profile.RemoveStairsAfterDepature,
                RemoveJetwayStairsOnFinal = profile.RemoveJetwayStairsOnFinal,

                PlaceProsimStairsWalkaround = profile.PlaceProsimStairsWalkaround,
                ClearGroundEquipOnBeacon = profile.ClearGroundEquipOnBeacon,
                GradualGroundEquipRemoval = profile.GradualGroundEquipRemoval,
                ConnectGpuWithApuRunning = profile.ConnectGpuWithApuRunning,
                ConnectPca = profile.ConnectPca,
                PcaOverride = profile.PcaOverride,
                ChockDelayMin = profile.ChockDelayMin,
                ChockDelayMax = profile.ChockDelayMax,

                CallReposition = profile.CallReposition,
                CallDeboardOnArrival = profile.CallDeboardOnArrival,
                RunDepartureDuringDeboarding = profile.RunDepartureDuringDeboarding,
                ChimeOnParked = profile.ChimeOnParked,
                ChimeOnDeboardComplete = profile.ChimeOnDeboardComplete,
                DepartureServices = profile.DepartureServices.Values
                    .Select(ServiceConfigDto.From)
                    .ToList(),

                RefuelMethod = profile.RefuelMethod,
                RefuelRateKgSec = profile.RefuelRateKgSec,
                RefuelTimeTargetSeconds = profile.RefuelTimeTargetSeconds,
                SkipFuelOnTankering = profile.SkipFuelOnTankering,
                RefuelFinishOnHose = profile.RefuelFinishOnHose,

                AttachTugDuringBoarding = profile.AttachTugDuringBoarding,
                CallPushbackWhenTugAttached = profile.CallPushbackWhenTugAttached,
                CallPushbackOnBeacon = profile.CallPushbackOnBeacon,

                SequenceOnBeacon = profile.SequenceOnBeacon,
                SeqDoorsCloseDelayMin = profile.SeqDoorsCloseDelayMin,
                SeqDoorsCloseDelayMax = profile.SeqDoorsCloseDelayMax,
                SeqJetwayRetractDelayMin = profile.SeqJetwayRetractDelayMin,
                SeqJetwayRetractDelayMax = profile.SeqJetwayRetractDelayMax,
                SeqGpuDisconnectDelayMin = profile.SeqGpuDisconnectDelayMin,
                SeqGpuDisconnectDelayMax = profile.SeqGpuDisconnectDelayMax,

                OperatorAutoSelect = profile.OperatorAutoSelect,
                OperatorPreferences = profile.OperatorPreferences?.ToList() ?? new(),
                CompanyHubs = profile.CompanyHubs?.ToList() ?? new(),

                SkipWalkAround = profile.SkipWalkAround,
                SkipCrewQuestion = profile.SkipCrewQuestion,
                SkipFollowMe = profile.SkipFollowMe,
                KeepDirectionMenuOpen = profile.KeepDirectionMenuOpen,
                AnswerCabinCallGround = profile.AnswerCabinCallGround,
                DelayCabinCallGround = profile.DelayCabinCallGround,
                AnswerCabinCallAir = profile.AnswerCabinCallAir,
                DelayCabinCallAir = profile.DelayCabinCallAir,

                FinalDelayMin = profile.FinalDelayMin,
                FinalDelayMax = profile.FinalDelayMax,
                FuelSaveLoadFob = profile.FuelSaveLoadFob,
                RandomizePax = profile.RandomizePax,
                ChancePerSeat = profile.ChancePerSeat,

                AutoDeiceEnabled = config.AutoDeiceEnabled,
                AutoDeiceFluid = config.AutoDeiceFluid,
            };
        }

        public void ApplyTo(AppService app)
        {
            var profile = app.Config?.CurrentProfile;
            var config = app.Config;
            if (profile == null || config == null)
                return;

            // Profile name is read-only on the wire — ignore any inbound value.

            profile.DoorStairHandling = DoorStairHandling;
            profile.DoorCargoHandling = DoorCargoHandling;
            profile.DoorCateringHandling = DoorCateringHandling;
            profile.DoorOpenBoardActive = DoorOpenBoardActive;
            profile.DoorsCargoKeepOpenOnLoaded = DoorsCargoKeepOpenOnLoaded;
            profile.DoorsCargoKeepOpenOnUnloaded = DoorsCargoKeepOpenOnUnloaded;
            profile.CloseDoorsOnFinal = CloseDoorsOnFinal;

            profile.CallJetwayStairsOnPrep = CallJetwayStairsOnPrep;
            profile.CallJetwayStairsDuringDeparture = CallJetwayStairsDuringDeparture;
            profile.CallJetwayStairsOnArrival = CallJetwayStairsOnArrival;
            profile.RemoveStairsAfterDepature = RemoveStairsAfterDepature;
            profile.RemoveJetwayStairsOnFinal = RemoveJetwayStairsOnFinal;

            profile.PlaceProsimStairsWalkaround = PlaceProsimStairsWalkaround;
            profile.ClearGroundEquipOnBeacon = ClearGroundEquipOnBeacon;
            profile.GradualGroundEquipRemoval = GradualGroundEquipRemoval;
            profile.ConnectGpuWithApuRunning = ConnectGpuWithApuRunning;
            profile.ConnectPca = ConnectPca;
            profile.PcaOverride = PcaOverride;
            // Min/max pair: enforce min < max as the WPF setter does, otherwise
            // discard the inbound value (silent rather than 400 — matches WPF UX).
            if (ChockDelayMin < ChockDelayMax)
            {
                profile.ChockDelayMin = ChockDelayMin;
                profile.ChockDelayMax = ChockDelayMax;
            }

            profile.CallReposition = CallReposition;
            profile.CallDeboardOnArrival = CallDeboardOnArrival;
            profile.RunDepartureDuringDeboarding = RunDepartureDuringDeboarding;
            profile.ChimeOnParked = ChimeOnParked;
            profile.ChimeOnDeboardComplete = ChimeOnDeboardComplete;

            // DepartureServices: array order becomes the SortedDictionary key.
            profile.DepartureServices.Clear();
            for (int i = 0; i < DepartureServices.Count; i++)
                profile.DepartureServices[i] = DepartureServices[i].ToServiceConfig();

            profile.RefuelMethod = RefuelMethod;
            profile.RefuelRateKgSec = RefuelRateKgSec;
            profile.RefuelTimeTargetSeconds = RefuelTimeTargetSeconds;
            profile.SkipFuelOnTankering = SkipFuelOnTankering;
            profile.RefuelFinishOnHose = RefuelFinishOnHose;

            profile.AttachTugDuringBoarding = AttachTugDuringBoarding;
            profile.CallPushbackWhenTugAttached = CallPushbackWhenTugAttached;
            profile.CallPushbackOnBeacon = CallPushbackOnBeacon;

            profile.SequenceOnBeacon = SequenceOnBeacon;
            profile.SeqDoorsCloseDelayMin = SeqDoorsCloseDelayMin;
            profile.SeqDoorsCloseDelayMax = SeqDoorsCloseDelayMax;
            profile.SeqJetwayRetractDelayMin = SeqJetwayRetractDelayMin;
            profile.SeqJetwayRetractDelayMax = SeqJetwayRetractDelayMax;
            profile.SeqGpuDisconnectDelayMin = SeqGpuDisconnectDelayMin;
            profile.SeqGpuDisconnectDelayMax = SeqGpuDisconnectDelayMax;

            profile.OperatorAutoSelect = OperatorAutoSelect;
            var incomingOps = OperatorPreferences?.ToList() ?? new();
            CFIT.AppLogger.Logger.Information(
                $"GsxSettingsDto.ApplyTo: profile='{profile.Name}' OperatorPreferences inbound count={incomingOps.Count} (was {(profile.OperatorPreferences?.Count ?? 0)})");
            profile.OperatorPreferences = incomingOps;
            profile.CompanyHubs = CompanyHubs?.ToList() ?? new();

            profile.SkipWalkAround = SkipWalkAround;
            profile.SkipCrewQuestion = SkipCrewQuestion;
            profile.SkipFollowMe = SkipFollowMe;
            profile.KeepDirectionMenuOpen = KeepDirectionMenuOpen;
            profile.AnswerCabinCallGround = AnswerCabinCallGround;
            profile.DelayCabinCallGround = DelayCabinCallGround;
            profile.AnswerCabinCallAir = AnswerCabinCallAir;
            profile.DelayCabinCallAir = DelayCabinCallAir;

            if (FinalDelayMin < FinalDelayMax)
            {
                profile.FinalDelayMin = FinalDelayMin;
                profile.FinalDelayMax = FinalDelayMax;
            }
            profile.FuelSaveLoadFob = FuelSaveLoadFob;
            profile.RandomizePax = RandomizePax;
            profile.ChancePerSeat = ChancePerSeat;

            config.AutoDeiceEnabled = AutoDeiceEnabled;
            config.AutoDeiceFluid = AutoDeiceFluid;

            config.SaveConfiguration();
            CFIT.AppLogger.Logger.Information(
                $"GsxSettingsDto.ApplyTo: SaveConfiguration() complete for profile='{profile.Name}' (OperatorPreferences count={profile.OperatorPreferences?.Count ?? 0})");

            // Re-fire ProfileChanged so the WPF ModelAutomation re-binds all
            // its fields against the freshly-mutated profile. AircraftProfile
            // is plain POCO with no INPC, so this event is the only path the
            // existing WPF UI uses to detect profile changes — without it the
            // GSX Settings tab keeps showing pre-save values until the user
            // closes and reopens the tab. SetAircraftProfile re-resolves the
            // same name, which is a no-op assignment for the profile reference
            // and a synchronous ProfileChanged invocation.
            try { app.GsxService?.SetAircraftProfile(profile.Name); } catch { }
        }
    }
}
