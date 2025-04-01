﻿using Prosim2GSX.Behaviours;
using Prosim2GSX.Services.Audio;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Models
{
    public class ServiceModel
    {
        public string AcarsNetwork {  get; set; }
        public string AcarsNetworkUrl { get; set; }
        public string AcarsSecret { get; set; }
        public bool AutoBoarding { get; set; }
        public bool AutoConnect { get; set; }
        public bool JetwayOnly { get; set; }
        public bool AutoDeboarding { get; set; }
        public bool AutoRefuel { get; set; }
        public bool CallCatering { get; set; }
        public bool CancellationRequested { get; set; } = false;
        public bool ConnectPCA { get; set; }
        public bool DisableCrew { get; set; }
        public string FlightPlanType { get; set; }
        public bool GsxVolumeControl { get; set; }
        public double HydaulicsBlueAmount { get; set; }
        public double HydaulicsGreenAmount { get; set; }
        public double HydaulicsYellowAmount { get; set; }
        public bool IsProsimRunning { get; set; } = false;
        public bool IsSessionRunning { get; set; } = false;
        public bool IsSimRunning { get; set; } = false;
        public string LogFilePath { get; set; }
        public string LogLevel { get; set; }
        public float OperatorDelay { get; set; }
        public bool PcaOnlyJetways { get; set; }
        public string ProsimHostname { get; set; }
        public float RefuelRate { get; set; }
        public string RefuelUnit { get; set; }
        public float RepositionDelay { get; set; }
        public bool RepositionPlane { get; set; }
        public double SavedFuelAmount { get; set; }
        public bool ServiceExited { get; set; } = false;
        public bool SetOpenCateringDoor { get; set; }
        public bool SetOpenCargoDoors { get; set; }
        public bool SetSaveHydraulicFluids { get; set; }
        public bool SetSaveFuel { get; set; }
        public bool SetZeroFuel { get; set; }
        public string SimBriefID { get; set; }
        public string SimBriefURL { get; set; }
        public bool SynchBypass { get; set; }
        public bool TestArrival { get; set; }
        public bool UseAcars { get; set; }
        public bool UseActualPaxValue { get; set; }
        public bool WaitForConnect { get; set; }
        public bool Vhf1LatchMute { get; set; }
        public string Vhf1VolumeApp { get; set; }
        public bool Vhf1VolumeControl { get; set; }
        public bool Vhf2VolumeControl { get; set; }
        public string Vhf2VolumeApp { get; set; }
        public bool Vhf2LatchMute { get; set; }

        public bool Vhf3VolumeControl { get; set; }
        public string Vhf3VolumeApp { get; set; }
        public bool Vhf3LatchMute { get; set; }

        public bool CabVolumeControl { get; set; }
        public string CabVolumeApp { get; set; }
        public bool CabLatchMute { get; set; }

        public bool PaVolumeControl { get; set; }
        public string PaVolumeApp { get; set; }
        public bool PaLatchMute { get; set; }

        protected ConfigurationFile ConfigurationFile = new();

        public Dictionary<AudioChannel, AudioChannelConfig> AudioChannels { get; private set; } = new Dictionary<AudioChannel, AudioChannelConfig>();

        public ServiceModel()
        {
            LoadConfiguration();
        }

        public bool IsVhf1Controllable()
        {
            return Vhf1VolumeControl && !string.IsNullOrEmpty(Vhf1VolumeApp);
        }

        public bool IsVhf2Controllable()
        {
            return Vhf2VolumeControl && !string.IsNullOrEmpty(Vhf2VolumeApp);
        }

        public bool IsVhf3Controllable()
        {
            return Vhf3VolumeControl && !string.IsNullOrEmpty(Vhf3VolumeApp);
        }

        public bool IsCabControllable()
        {
            return CabVolumeControl && !string.IsNullOrEmpty(CabVolumeApp);
        }

        public bool IsPaControllable()
        {
            return PaVolumeControl && !string.IsNullOrEmpty(PaVolumeApp);
        }

        protected void LoadConfiguration()
        {
            ConfigurationFile.LoadConfiguration();

            AcarsNetwork = Convert.ToString(ConfigurationFile.GetSetting("acarsNetwork", "Hoppie"));
            AcarsNetworkUrl = Convert.ToString(ConfigurationFile.GetSetting("acarsNetworkUrl", "http://www.hoppie.nl/acars/system/connect.html"));
            AcarsSecret = Convert.ToString(ConfigurationFile.GetSetting("acarsSecret", ""));
            AutoBoarding = Convert.ToBoolean(ConfigurationFile.GetSetting("autoBoarding", "true"));
            AutoConnect = Convert.ToBoolean(ConfigurationFile.GetSetting("autoConnect", "true"));
            JetwayOnly = Convert.ToBoolean(ConfigurationFile.GetSetting("jetwayOnly", "false"));
            AutoDeboarding = Convert.ToBoolean(ConfigurationFile.GetSetting("autoDeboarding", "true"));
            AutoRefuel = Convert.ToBoolean(ConfigurationFile.GetSetting("autoRefuel", "true"));
            CabVolumeApp = Convert.ToString(ConfigurationFile.GetSetting("cabVolumeApp", ""));
            CabVolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("cabVolumeControl", "false"));
            CabLatchMute = Convert.ToBoolean(ConfigurationFile.GetSetting("cabLatchMute", "true"));
            CallCatering = Convert.ToBoolean(ConfigurationFile.GetSetting("callCatering", "true"));
            ConnectPCA = Convert.ToBoolean(ConfigurationFile.GetSetting("connectPCA", "true"));
            DisableCrew = Convert.ToBoolean(ConfigurationFile.GetSetting("disableCrew", "true"));
            FlightPlanType = Convert.ToString(ConfigurationFile.GetSetting("flightPlanType", "MCDU"));
            GsxVolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("gsxVolumeControl", "true"));
            HydaulicsBlueAmount = Convert.ToSingle(ConfigurationFile.GetSetting("hydraulicBlueAmount", "0"), new RealInvariantFormat(ConfigurationFile.GetSetting("hydraulicBlueAmount", "0")));
            HydaulicsGreenAmount = Convert.ToSingle(ConfigurationFile.GetSetting("hydraulicGreenAmount", "0"), new RealInvariantFormat(ConfigurationFile.GetSetting("hydraulicGreenAmount", "0")));
            HydaulicsYellowAmount = Convert.ToSingle(ConfigurationFile.GetSetting("hydraulicYellowAmount", "0"), new RealInvariantFormat(ConfigurationFile.GetSetting("hydraulicYellowAmount", "0")));
            LogFilePath = Convert.ToString(ConfigurationFile.GetSetting("logFilePath", "Prosim2GSX.log"));
            LogLevel = Convert.ToString(ConfigurationFile.GetSetting("logLevel", "Debug"));
            PcaOnlyJetways = Convert.ToBoolean(ConfigurationFile.GetSetting("pcaOnlyJetway", "true"));
            PaVolumeApp = Convert.ToString(ConfigurationFile.GetSetting("paVolumeApp", ""));
            PaVolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("paVolumeControl", "false"));
            PaLatchMute = Convert.ToBoolean(ConfigurationFile.GetSetting("paLatchMute", "true"));
            RefuelRate = Convert.ToSingle(ConfigurationFile.GetSetting("refuelRate", "28"), new RealInvariantFormat(ConfigurationFile.GetSetting("refuelRate", "28")));
            RefuelUnit = Convert.ToString(ConfigurationFile.GetSetting("refuelUnit", "KGS"));
            ProsimHostname = Convert.ToString(ConfigurationFile.GetSetting("prosimHostname", "127.0.0.1"));
            RepositionDelay = Convert.ToSingle(ConfigurationFile.GetSetting("repositionDelay", "3"), new RealInvariantFormat(ConfigurationFile.GetSetting("repositionDelay", "3")));
            RepositionPlane = Convert.ToBoolean(ConfigurationFile.GetSetting("repositionPlane", "true"));
            OperatorDelay = Convert.ToSingle(ConfigurationFile.GetSetting("operatorDelay", "10"), new RealInvariantFormat(ConfigurationFile.GetSetting("operatorDelay", "10")));
            SavedFuelAmount = Convert.ToSingle(ConfigurationFile.GetSetting("savedFuelAmount", "0"), new RealInvariantFormat(ConfigurationFile.GetSetting("savedFuelAmount", "0")));
            SetOpenCateringDoor = Convert.ToBoolean(ConfigurationFile.GetSetting("setOpenAftDoorCatering", "false"));
            SetOpenCargoDoors = Convert.ToBoolean(ConfigurationFile.GetSetting("setOpenCargoDoors", "true"));
            SetSaveHydraulicFluids = Convert.ToBoolean(ConfigurationFile.GetSetting("saveHydraulicFluids", "false"));
            SetSaveFuel = Convert.ToBoolean(ConfigurationFile.GetSetting("setSaveFuel", "false"));
            SetZeroFuel = Convert.ToBoolean(ConfigurationFile.GetSetting("setZeroFuel", "false"));
            SimBriefID = Convert.ToString(ConfigurationFile.GetSetting("pilotID", "0"));
            SimBriefURL = Convert.ToString(ConfigurationFile.GetSetting("simbriefURL", "https://www.simbrief.com/api/xml.fetcher.php?userid={0}"));
            SynchBypass = Convert.ToBoolean(ConfigurationFile.GetSetting("synchBypass", "true"));
            TestArrival = Convert.ToBoolean(ConfigurationFile.GetSetting("testArrival", "false"));
            UseAcars = Convert.ToBoolean(ConfigurationFile.GetSetting("useAcars", "false"));
            UseActualPaxValue = Convert.ToBoolean(ConfigurationFile.GetSetting("useActualValue", "true"));
            Vhf1VolumeApp = Convert.ToString(ConfigurationFile.GetSetting("vhf1VolumeApp", "vPilot"));
            Vhf1VolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf1VolumeControl", "false"));
            Vhf1LatchMute = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf1LatchMute", "true"));
            Vhf2VolumeApp = Convert.ToString(ConfigurationFile.GetSetting("vhf2VolumeApp", ""));
            Vhf2VolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf2VolumeControl", "false"));
            Vhf2LatchMute = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf2LatchMute", "true"));
            Vhf3VolumeApp = Convert.ToString(ConfigurationFile.GetSetting("vhf3VolumeApp", ""));
            Vhf3VolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf3VolumeControl", "false"));
            Vhf3LatchMute = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf3LatchMute", "true"));
            WaitForConnect = Convert.ToBoolean(ConfigurationFile.GetSetting("waitForConnect", "true"));

            InitializeAudioChannels();
        }

        private void InitializeAudioChannels()
        {
            // GSX
            AudioChannels[AudioChannel.GSX] = new AudioChannelConfig
            {
                ProcessName = "Couatl64_MSFS",
                VolumeDataRef = "system.analog.A_ASP_INT_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_INT_REC",
                Enabled = GsxVolumeControl
            };

            // VHF1
            AudioChannels[AudioChannel.VHF1] = new AudioChannelConfig
            {
                ProcessName = Vhf1VolumeApp,
                VolumeDataRef = "system.analog.A_ASP_VHF_1_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_VHF_1_REC",
                Enabled = IsVhf1Controllable(),
                LatchMute = Vhf1LatchMute
            };

            // VHF2
            AudioChannels[AudioChannel.VHF2] = new AudioChannelConfig
            {
                ProcessName = Vhf2VolumeApp,
                VolumeDataRef = "system.analog.A_ASP_VHF_2_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_VHF_2_REC",
                Enabled = IsVhf2Controllable(),
                LatchMute = Vhf2LatchMute
            };

            // VHF3
            AudioChannels[AudioChannel.VHF3] = new AudioChannelConfig
            {
                ProcessName = Vhf3VolumeApp,
                VolumeDataRef = "system.analog.A_ASP_VHF_3_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_VHF_3_REC",
                Enabled = IsVhf3Controllable(),
                LatchMute = Vhf3LatchMute
            };

            // CAB
            AudioChannels[AudioChannel.CAB] = new AudioChannelConfig
            {
                ProcessName = CabVolumeApp,
                VolumeDataRef = "system.analog.A_ASP_CAB_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_CAB_REC",
                Enabled = IsCabControllable(),
                LatchMute = CabLatchMute
            };

            // PA
            AudioChannels[AudioChannel.PA] = new AudioChannelConfig
            {
                ProcessName = PaVolumeApp,
                VolumeDataRef = "system.analog.A_ASP_PA_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_PA_REC",
                Enabled = IsPaControllable(),
                LatchMute = PaLatchMute
            };
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            return ConfigurationFile[key] ?? defaultValue;
        }

        public void SetSetting(string key, string value, bool noLoad = false)
        {
            ConfigurationFile[key] = value;
            if (!noLoad)
                LoadConfiguration();
        }

        public float GetFuelRateKGS()
        {
            if (RefuelUnit == "KGS")
                return RefuelRate;
            else
                return RefuelRate / ProsimController.weightConversion;
        }
    }
}
