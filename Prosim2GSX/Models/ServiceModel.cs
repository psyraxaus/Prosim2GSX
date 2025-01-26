﻿using Prosim2GSX.Behaviours;
using System;

namespace Prosim2GSX.Models
{
    public class ServiceModel
    {
        public bool AutoBoarding { get; set; }
        public bool AutoConnect { get; set; }
        public bool AutoDeboarding { get; set; }
        public bool AutoRefuel { get; set; }
        public bool CallCatering { get; set; }
        public bool CancellationRequested { get; set; } = false;
        public bool ConnectPCA { get; set; }
        public bool DisableCrew { get; set; }
        public string FlightPlanType { get; set; }
        public bool GsxVolumeControl { get; set; }
        public bool IsProsimRunning { get; set; } = false;
        public bool IsSessionRunning { get; set; } = false;
        public bool IsSimRunning { get; set; } = false;
        public float OperatorDelay { get; set; }
        public bool PcaOnlyJetways { get; set; }
        public string ProsimHostname { get; set; }
        public float RefuelRate { get; set; }
        public string RefuelUnit { get; set; }
        public float RepositionDelay { get; set; }
        public bool RepositionPlane { get; set; }
        public double SavedFuelAmount { get; set; }
        public bool ServiceExited { get; set; } = false;
        public bool SetOpenAftCateringDoor { get; set; }
        public bool SetSaveFuel { get; set; }
        public bool SetZeroFuel { get; set; }
        public string SimBriefID { get; set; }
        public string SimBriefURL { get; set; }
        public bool SynchBypass { get; set; }
        public bool TestArrival { get; set; }
        public bool UseActualPaxValue { get; set; }
        public bool Vhf1LatchMute { get; set; }
        public string Vhf1VolumeApp { get; set; }
        public bool Vhf1VolumeControl { get; set; }
        public bool WaitForConnect { get; set; }

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

            AutoBoarding = Convert.ToBoolean(ConfigurationFile.GetSetting("autoBoarding", "true"));
            AutoConnect = Convert.ToBoolean(ConfigurationFile.GetSetting("autoConnect", "true"));
            AutoDeboarding = Convert.ToBoolean(ConfigurationFile.GetSetting("autoDeboarding", "true"));
            AutoRefuel = Convert.ToBoolean(ConfigurationFile.GetSetting("autoRefuel", "true"));
            CallCatering = Convert.ToBoolean(ConfigurationFile.GetSetting("callCatering", "true"));
            ConnectPCA = Convert.ToBoolean(ConfigurationFile.GetSetting("connectPCA", "true"));
            DisableCrew = Convert.ToBoolean(ConfigurationFile.GetSetting("disableCrew", "true"));
            FlightPlanType = Convert.ToString(ConfigurationFile.GetSetting("flightPlanType", "MCDU"));
            GsxVolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("gsxVolumeControl", "true"));
            PcaOnlyJetways = Convert.ToBoolean(ConfigurationFile.GetSetting("pcaOnlyJetway", "true"));
            RefuelRate = Convert.ToSingle(ConfigurationFile.GetSetting("refuelRate", "28"), new RealInvariantFormat(ConfigurationFile.GetSetting("refuelRate", "28")));
            RefuelUnit = Convert.ToString(ConfigurationFile.GetSetting("refuelUnit", "KGS"));
            ProsimHostname = Convert.ToString(ConfigurationFile.GetSetting("prosimHostname", "127.0.0.1"));
            RepositionDelay = Convert.ToSingle(ConfigurationFile.GetSetting("repositionDelay", "3"), new RealInvariantFormat(ConfigurationFile.GetSetting("repositionDelay", "3")));
            RepositionPlane = Convert.ToBoolean(ConfigurationFile.GetSetting("repositionPlane", "true"));
            OperatorDelay = Convert.ToSingle(ConfigurationFile.GetSetting("operatorDelay", "10"), new RealInvariantFormat(ConfigurationFile.GetSetting("operatorDelay", "10")));
            SavedFuelAmount = Convert.ToSingle(ConfigurationFile.GetSetting("savedFuelAmount", "0"), new RealInvariantFormat(ConfigurationFile.GetSetting("savedFuelAmount", "0")));
            SetOpenAftCateringDoor = Convert.ToBoolean(ConfigurationFile.GetSetting("setOpenAftDoorCatering", "false"));
            SetSaveFuel = Convert.ToBoolean(ConfigurationFile.GetSetting("setSaveFuel", "false"));
            SetZeroFuel = Convert.ToBoolean(ConfigurationFile.GetSetting("setZeroFuel", "false"));
            SimBriefID = Convert.ToString(ConfigurationFile.GetSetting("pilotID", "0"));
            SimBriefURL = Convert.ToString(ConfigurationFile.GetSetting("simbriefURL", "https://www.simbrief.com/api/xml.fetcher.php?userid={0}"));
            SynchBypass = Convert.ToBoolean(ConfigurationFile.GetSetting("synchBypass", "true"));
            TestArrival = Convert.ToBoolean(ConfigurationFile.GetSetting("testArrival", "false"));
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

        public float GetFuelRateKGS()
        {
            if (RefuelUnit == "KGS")
                return RefuelRate;
            else
                return RefuelRate / ProsimController.weightConversion;
        }
    }
}