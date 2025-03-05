﻿using Prosim2GSX.Behaviours;
using System;

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

        protected ConfigurationFile ConfigurationFile = new();

        public ServiceModel()
        {
            LoadConfiguration();
        }

        public bool IsVhf1Controllable()
        {
            return Vhf1VolumeControl && !string.IsNullOrEmpty(Vhf1VolumeApp);
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
            WaitForConnect = Convert.ToBoolean(ConfigurationFile.GetSetting("waitForConnect", "true"));

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

        // GetFuelRateKGS method moved to ProsimFuelService
    }
}
